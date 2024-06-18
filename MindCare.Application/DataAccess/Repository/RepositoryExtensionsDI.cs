using Microsoft.Extensions.DependencyInjection;
using MindCare.Application.DataAccess.Repository.IRepository;

namespace MindCare.Application.DataAccess.Repository
{
    public static class RepositoryExtensionsDI
    {
        public static IServiceCollection AddRepositoryExtensions(this IServiceCollection services)
        {
            services.AddScoped<IClientRepository, ClientRepository>();
            services.AddScoped<IAppointmentRepository, AppointmentRepository>();
            return services;
        }
    }
}
