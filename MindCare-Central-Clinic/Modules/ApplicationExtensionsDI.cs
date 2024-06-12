using MindCare.Application.DataAccess.Repository;
using MindCare.Application.Services;
using MindCare_Central_Clinic.Models;

namespace MindCare_Central_Clinic.Modules
{
    public static class ApplicationExtensionsDI
    {
        public static IServiceCollection AddApplicationExtensions(this IServiceCollection services)
        {
            services.AddServiceExtensionsDI();
            services.AddRepositoryExtensionsDI();
            services.AddModelExtensionsDI();
            //services.AddScoped<IEmailSender, EmailSender>();
            return services;
        }

        public static IServiceCollection AddServiceExtensionsDI(this IServiceCollection services) => services.AddServiceExtensions();

        public static IServiceCollection AddRepositoryExtensionsDI(this IServiceCollection services) => services.AddRepositoryExtensions();

        public static IServiceCollection AddModelExtensionsDI(this IServiceCollection services) => services.AddModelExtensions();
    }
}
