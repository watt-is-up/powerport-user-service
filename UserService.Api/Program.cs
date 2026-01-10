using UserService.Application.Providers.RegisterProvider;
using UserService.Infrastructure;

namespace UserService.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Options
            builder.Services.Configure<ProvisioningOptions>(builder.Configuration.GetSection("Provisioning"));

            // Clean architecture wiring
            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddScoped<IProviderProvisioningService, ProviderProvisioningService>();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // MVP: no auth wired yet (you can add JWT auth later)
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
