namespace MindCare_Central_Clinic.Models
{
    public static class ModelExtensionsDI
    {
        public static IServiceCollection AddModelExtensions(this IServiceCollection services)
        {
            services.AddScoped<ClientViewModel>();
            services.AddScoped<AppointmentViewModel>();
            services.AddScoped<PaymentViewModel>();
            services.AddScoped<ProfessionalViewModel>();
            services.AddScoped<HomeViewModel>();
            return services;
        }
    }
}
