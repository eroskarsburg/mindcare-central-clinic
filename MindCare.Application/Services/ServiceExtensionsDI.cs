using Microsoft.Extensions.DependencyInjection;
using MindCare.Application.Services.IServices;

namespace MindCare.Application.Services
{
    public static class ServiceExtensionsDI
    {
        public static IServiceCollection AddServiceExtensions(this IServiceCollection services)
        {
            services.AddScoped<IClientService, ClientService>();
            services.AddScoped<IAppointmentService, AppointmentService>();
            services.AddScoped<IPaymentService, PaymentService>();
            return services;
        }
    }
}
