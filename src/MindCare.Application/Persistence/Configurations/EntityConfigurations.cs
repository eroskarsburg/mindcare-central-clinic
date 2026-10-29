using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MindCare.Application.Domain;

namespace MindCare.Application.Persistence.Configurations;

// Os nomes de tabelas e colunas seguem o esquema original do projeto (snake_case).

internal sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> b)
    {
        b.ToTable("clients");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id_client");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        b.Property(x => x.Cpf).HasColumnName("cpf").HasMaxLength(11).IsRequired();
        b.Property(x => x.Gender).HasColumnName("gender").HasMaxLength(20).IsRequired();
        b.Property(x => x.Age).HasColumnName("age");
        b.Property(x => x.ConsentAt).HasColumnName("consent_at");
        b.Property(x => x.AnonymizedAt).HasColumnName("anonymized_at");
        b.Ignore(x => x.IsAnonymized);
        b.HasIndex(x => x.Cpf).IsUnique();
    }
}

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id_user");
        b.Property(x => x.Username).HasColumnName("username").HasMaxLength(50).IsRequired();
        b.Property(x => x.PasswordHash).HasColumnName("password").HasMaxLength(255).IsRequired();
        b.Property(x => x.AccessLevel).HasColumnName("id_access_level");
        b.Property(x => x.LastActivity).HasColumnName("last_activity");
        b.Property(x => x.MustChangePassword).HasColumnName("must_change_password");
        b.Property(x => x.FailedLoginCount).HasColumnName("failed_login_count");
        b.Property(x => x.LockoutUntil).HasColumnName("lockout_until");
        b.HasIndex(x => x.Username).IsUnique();
    }
}

internal sealed class ProfessionalConfiguration : IEntityTypeConfiguration<Professional>
{
    public void Configure(EntityTypeBuilder<Professional> b)
    {
        b.ToTable("professionals");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id_professional");
        b.Property(x => x.UserId).HasColumnName("id_user_prof");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        b.Property(x => x.Cpf).HasColumnName("cpf").HasMaxLength(11).IsRequired();
        b.Property(x => x.Gender).HasColumnName("gender").HasMaxLength(20).IsRequired();
        b.Property(x => x.Speciality).HasColumnName("speciality").HasMaxLength(60).IsRequired();
        b.HasIndex(x => x.Cpf).IsUnique();
        b.HasIndex(x => x.UserId).IsUnique();

        b.HasOne(x => x.User)
            .WithOne(x => x.Professional)
            .HasForeignKey<Professional>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> b)
    {
        b.ToTable("appointments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id_appointment");
        b.Property(x => x.ClientId).HasColumnName("id_client");
        b.Property(x => x.Modality).HasColumnName("modality").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.ScheduledAt).HasColumnName("scheduled_date");
        b.Property(x => x.Observation).HasColumnName("observation").HasMaxLength(500);
        b.Property(x => x.ProfessionalId).HasColumnName("id_professional");
        b.Property(x => x.DurationMinutes).HasColumnName("duration_minutes").HasDefaultValue(Appointment.DefaultDurationMinutes);
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20)
            .HasDefaultValue(AppointmentStatus.Agendada).HasSentinel((AppointmentStatus)(-1));
        b.Property(x => x.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(200);
        b.Property(x => x.SeriesId).HasColumnName("series_id");
        b.Property(x => x.MeetingUrl).HasColumnName("meeting_url").HasMaxLength(200);
        b.Ignore(x => x.EndsAt);
        b.Ignore(x => x.OccupiesSlot);
        b.HasIndex(x => x.ScheduledAt);
        b.HasIndex(x => new { x.ProfessionalId, x.ScheduledAt });
        b.HasIndex(x => x.SeriesId);

        b.HasOne(x => x.Professional)
            .WithMany()
            .HasForeignKey(x => x.ProfessionalId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasOne(x => x.Client)
            .WithMany(x => x.Appointments)
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Payment)
            .WithOne(x => x.Appointment)
            .HasForeignKey<Payment>(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("payments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id_payments");
        b.Property(x => x.AppointmentId).HasColumnName("id_appointment");
        b.Property(x => x.Price).HasColumnName("price").HasPrecision(10, 2);
        b.Property(x => x.PaidPrice).HasColumnName("paid_price").HasPrecision(10, 2);
        b.Property(x => x.PaidDate).HasColumnName("paid_date").HasColumnType("date");
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        b.Ignore(x => x.Outstanding);
        b.HasIndex(x => x.AppointmentId).IsUnique();
        b.HasIndex(x => x.Status);
    }
}

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id_audit");
        b.Property(x => x.Timestamp).HasColumnName("timestamp");
        b.Property(x => x.UserId).HasColumnName("id_user");
        b.Property(x => x.Username).HasColumnName("username").HasMaxLength(50);
        b.Property(x => x.Action).HasColumnName("action").HasMaxLength(40).IsRequired();
        b.Property(x => x.Entity).HasColumnName("entity").HasMaxLength(40).IsRequired();
        b.Property(x => x.EntityId).HasColumnName("entity_id").HasMaxLength(40);
        b.Property(x => x.Details).HasColumnName("details").HasMaxLength(500);
        b.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        b.HasIndex(x => x.Timestamp);
        b.HasIndex(x => new { x.Entity, x.EntityId });
    }
}

internal sealed class ProfessionalAvailabilityConfiguration : IEntityTypeConfiguration<ProfessionalAvailability>
{
    private static readonly ValueConverter<TimeOnly, TimeSpan> TimeOnlyConverter =
        new(t => t.ToTimeSpan(), ts => TimeOnly.FromTimeSpan(ts));

    public void Configure(EntityTypeBuilder<ProfessionalAvailability> b)
    {
        b.ToTable("professional_availability");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id_availability");
        b.Property(x => x.ProfessionalId).HasColumnName("id_professional");
        b.Property(x => x.DayOfWeek).HasColumnName("day_of_week");
        // O provider MySQL lê colunas TIME como TimeSpan: converte explicitamente para TimeOnly.
        b.Property(x => x.StartTime).HasColumnName("start_time").HasConversion(TimeOnlyConverter);
        b.Property(x => x.EndTime).HasColumnName("end_time").HasConversion(TimeOnlyConverter);
        b.HasIndex(x => new { x.ProfessionalId, x.DayOfWeek });

        b.HasOne<Professional>()
            .WithMany(p => p.Availability)
            .HasForeignKey(x => x.ProfessionalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ScheduleBlockConfiguration : IEntityTypeConfiguration<ScheduleBlock>
{
    public void Configure(EntityTypeBuilder<ScheduleBlock> b)
    {
        b.ToTable("schedule_blocks");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id_block");
        b.Property(x => x.ProfessionalId).HasColumnName("id_professional");
        b.Property(x => x.StartsAt).HasColumnName("starts_at");
        b.Property(x => x.EndsAt).HasColumnName("ends_at");
        b.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(120).IsRequired();
        b.HasIndex(x => new { x.StartsAt, x.EndsAt });

        b.HasOne(x => x.Professional)
            .WithMany()
            .HasForeignKey(x => x.ProfessionalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
