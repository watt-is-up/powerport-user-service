using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Options;
using Npgsql;
using UserService.Application.Configuration;
using UserService.Application.Providers.RegisterProvider;

namespace UserService.Infrastructure;

public sealed class TenantInfraProvisioner : ITenantInfraProvisioner
{
    private readonly TenantProvisioningOptions _opt;
    private readonly SecretClient _kv;

    public TenantInfraProvisioner(IOptions<TenantProvisioningOptions> opt)
    {
        _opt = opt.Value;
        _kv = new SecretClient(new Uri(_opt.KeyVaultUri), new DefaultAzureCredential());
    }

    public async Task<TenantInfraResult> EnsureTenantDatabasesAsync(
        string uniqueName,
        string environment,
        CancellationToken ct)
    {
        var env = (environment ?? "").Trim().ToLowerInvariant();
        var tenant = (uniqueName ?? "").Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(env)) throw new ArgumentException("environment is required", nameof(environment));
        if (string.IsNullOrWhiteSpace(tenant)) throw new ArgumentException("uniqueName is required", nameof(uniqueName));

        // Azure PG often needs user@servername
        var adminUser = _opt.AdminUser;

        var adminConn = new NpgsqlConnectionStringBuilder
        {
            Host = _opt.PostgresHost,
            Port = _opt.PostgresPort,
            Database = "postgres",
            Username = adminUser,
            Password = _opt.AdminPassword,
            SslMode = SslMode.Require,
            // TrustServerCertificate = true, // enable for DEV only if you hit cert issues
        }.ToString();

        await using var conn = new NpgsqlConnection(adminConn);
        await conn.OpenAsync(ct);

        var dbNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var secretNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var svc in _opt.TenantServices)
        {
            var dbName = DbName(env, svc, tenant);
            var secretName = KvSecret(env, svc, tenant);

            dbNames[svc] = dbName;
            secretNames[svc] = secretName;

            // Create DB if missing
            if (!await DatabaseExistsAsync(conn, dbName, ct))
            {
                await using var cmd = new NpgsqlCommand($@"CREATE DATABASE ""{dbName}"";", conn);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            // Save conn string in KeyVault (per-service DB)
            var dbConn = new NpgsqlConnectionStringBuilder
            {
                Host = _opt.PostgresHost,
                Port = _opt.PostgresPort,
                Database = dbName,
                Username = adminUser,
                Password = _opt.AdminPassword,
                SslMode = SslMode.Require,
                // TrustServerCertificate = true, // DEV only if needed
            }.ToString();

            await RecoverIfDeletedAsync(secretName, ct);
            await _kv.SetSecretAsync(secretName, dbConn, ct);
        }

        return new TenantInfraResult(dbNames, secretNames);
    }

    private static string DbName(string env, string service, string uniqueName)
        => $"db_svc_{service}__{uniqueName}__{env}".ToLowerInvariant();

    private static string KvSecret(string env, string service, string uniqueName)
        => $"kv-conn-svc-{service}-{uniqueName}-{env}".ToLowerInvariant();

    private static async Task<bool> DatabaseExistsAsync(NpgsqlConnection conn, string dbName, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @n", conn);
        cmd.Parameters.AddWithValue("n", dbName);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is not null;
    }

    private async Task RecoverIfDeletedAsync(string secretName, CancellationToken ct)
    {
        try
        {
            var deleted = await _kv.GetDeletedSecretAsync(secretName, ct);
            if (deleted?.Value != null)
                await _kv.StartRecoverDeletedSecretAsync(secretName, ct);
        }
        catch
        {
            // not deleted -> ignore
        }
    }
}
