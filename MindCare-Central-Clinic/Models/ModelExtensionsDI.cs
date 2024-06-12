using MindCare.Application.Services.IServices;
using MindCare.Application.Services;

namespace MindCare_Central_Clinic.Models
{
    public static class ModelExtensionsDI
    {
        public static IServiceCollection AddModelExtensions(this IServiceCollection services)
        {
            services.AddScoped<ClientViewModel>();
            return services;
        }
    }
}
