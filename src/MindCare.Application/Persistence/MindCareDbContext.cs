using Microsoft.EntityFrameworkCore;
using MindCare.Application.Domain;

namespace MindCare.Application.Persistence;

public sealed class MindCareDbContext(DbContextOptions<MindCareDbContext> options) : DbContext(options)
{
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Professional> Professionals => Set<Professional>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ProfessionalAvailability> Availability => Set<ProfessionalAvailability>();
    public DbSet<ScheduleBlock> ScheduleBlocks => Set<ScheduleBlock>();
    public DbSet<OutboundMessage> OutboundMessages => Set<OutboundMessage>();
    public DbSet<StaffNotification> StaffNotifications => Set<StaffNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MindCareDbContext).Assembly);
}
