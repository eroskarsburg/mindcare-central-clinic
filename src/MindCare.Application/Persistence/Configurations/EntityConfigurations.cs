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
        b.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(15);
        b.Property(x => x.Email).HasColumnName("email").HasMaxLength(120);
        b.Property(x => x.NotifyByWhatsApp).HasColumnName("notify_whatsapp").HasDefaultValue(true).HasSentinel(true);
        b.Property(x => x.NotifyByEmail).HasColumnName("notify_email").HasDefaultValue(true).HasSentinel(true);
        b.HasIndex(x => x.Phone);
        b.Property(x => x.BirthDate).HasColumnName("birth_date").HasColumnType("date");
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20)
            .HasDefaultValue(ClientStatus.Ativo).HasSentinel((ClientStatus)(-1));
        b.Property(x => x.ResponsibleProfessionalId).HasColumnName("id_responsible_professional");
        b.Property(x => x.EmergencyContactName).HasColumnName("emergency_contact_name").HasMaxLength(100);
        b.Property(x => x.EmergencyContactPhone).HasColumnName("emergency_contact_phone").HasMaxLength(15);
        b.Property(x => x.Address).HasColumnName("address").HasMaxLength(250);
        b.Property(x => x.HealthInsurance).HasColumnName("health_insurance").HasMaxLength(80);
        b.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(1000);
        b.HasOne(x => x.ResponsibleProfessional)
            .WithMany()
            .HasForeignKey(x => x.ResponsibleProfessionalId)
            .OnDelete(DeleteBehavior.SetNull);
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
        b.Property(x => x.RegistrationNumber).HasColumnName("registration_number").HasMaxLength(40);
        b.Property(x => x.CommissionPercent).HasColumnName("commission_percent").HasPrecision(5, 2);
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
        b.Property(x => x.ReminderQueuedAt).HasColumnName("reminder_queued_at");
        b.Property(x => x.ConfirmationTokenHash).HasColumnName("confirmation_token_hash").HasMaxLength(64);
        b.Property(x => x.PatientResponseVia).HasColumnName("patient_response_via").HasMaxLength(20);
        b.Property(x => x.PatientRespondedAt).HasColumnName("patient_responded_at");
        b.HasIndex(x => x.ConfirmationTokenHash);
        b.Property(x => x.ServiceTypeId).HasColumnName("id_service_type");
        b.Property(x => x.PackageId).HasColumnName("id_package");
        b.HasOne(x => x.ServiceType).WithMany().HasForeignKey(x => x.ServiceTypeId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.Package).WithMany(p => p.Appointments).HasForeignKey(x => x.PackageId).OnDelete(DeleteBehavior.SetNull);
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
        b.HasMany(x => x.Entries).WithOne(e => e.Payment).HasForeignKey(e => e.PaymentId).OnDelete(DeleteBehavior.Cascade);
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

internal sealed class OutboundMessageConfiguration : IEntityTypeConfiguration<OutboundMessage>
{
    public void Configure(EntityTypeBuilder<OutboundMessage> b)
    {
        b.ToTable("outbound_messages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id_message");
        b.Property(x => x.ClientId).HasColumnName("id_client");
        b.Property(x => x.AppointmentId).HasColumnName("id_appointment");
        b.Property(x => x.Channel).HasColumnName("channel").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Kind).HasColumnName("kind").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Recipient).HasColumnName("recipient").HasMaxLength(120).IsRequired();
        b.Property(x => x.ConfirmationToken).HasColumnName("confirmation_token").HasMaxLength(64);
        b.Property(x => x.Note).HasColumnName("note").HasMaxLength(200);
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Attempts).HasColumnName("attempts");
        b.Property(x => x.LastError).HasColumnName("last_error").HasMaxLength(500);
        b.Property(x => x.ExternalId).HasColumnName("external_id").HasMaxLength(200);
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.NextAttemptAt).HasColumnName("next_attempt_at");
        b.Property(x => x.SentAt).HasColumnName("sent_at");
        b.HasIndex(x => new { x.Status, x.NextAttemptAt });
        b.HasIndex(x => x.ExternalId);

        b.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Appointment).WithMany().HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class StaffNotificationConfiguration : IEntityTypeConfiguration<StaffNotification>
{
    public void Configure(EntityTypeBuilder<StaffNotification> b)
    {
        b.ToTable("staff_notifications");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id_notification");
        b.Property(x => x.UserId).HasColumnName("id_user");
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(120).IsRequired();
        b.Property(x => x.Message).HasColumnName("message").HasMaxLength(500).IsRequired();
        b.Property(x => x.Link).HasColumnName("link").HasMaxLength(200);
        b.Property(x => x.Icon).HasColumnName("icon").HasMaxLength(40);
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.ReadAt).HasColumnName("read_at");
        b.HasIndex(x => new { x.UserId, x.ReadAt });

        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ClinicalNoteConfiguration : IEntityTypeConfiguration<ClinicalNote>
{
    public void Configure(EntityTypeBuilder<ClinicalNote> b)
    {
        b.ToTable("clinical_notes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id_note");
        b.Property(x => x.ClientId).HasColumnName("id_client");
        b.Property(x => x.ProfessionalId).HasColumnName("id_professional");
        b.Property(x => x.AppointmentId).HasColumnName("id_appointment");
        b.Property(x => x.EncryptedContent).HasColumnName("encrypted_content").HasMaxLength(60_000).IsRequired();
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        b.HasIndex(x => x.ClientId);

        b.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Professional).WithMany().HasForeignKey(x => x.ProfessionalId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Appointment).WithMany().HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class ClientDocumentConfiguration : IEntityTypeConfiguration<ClientDocument>
{
    public void Configure(EntityTypeBuilder<ClientDocument> b)
    {
        b.ToTable("client_documents");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id_document");
        b.Property(x => x.ClientId).HasColumnName("id_client");
        b.Property(x => x.Category).HasColumnName("category").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(160).IsRequired();
        b.Property(x => x.StoragePath).HasColumnName("storage_path").HasMaxLength(200).IsRequired();
        b.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(120).IsRequired();
        b.Property(x => x.SizeBytes).HasColumnName("size_bytes");
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(200);
        b.Property(x => x.UploadedByUserId).HasColumnName("id_user_uploaded");
        b.Property(x => x.UploadedByProfessionalId).HasColumnName("id_professional_uploaded");
        b.Property(x => x.UploadedAt).HasColumnName("uploaded_at");
        b.HasIndex(x => x.ClientId);

        b.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ServiceTypeConfiguration : IEntityTypeConfiguration<ServiceType>
{
    public void Configure(EntityTypeBuilder<ServiceType> b)
    {
        b.ToTable("service_types");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id_service_type");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(80).IsRequired();
        b.Property(x => x.DurationMinutes).HasColumnName("duration_minutes");
        b.Property(x => x.Price).HasColumnName("price").HasPrecision(10, 2);
        b.Property(x => x.Active).HasColumnName("active").HasDefaultValue(true).HasSentinel(true);
        b.HasIndex(x => x.Name).IsUnique();
    }
}

internal sealed class PaymentEntryConfiguration : IEntityTypeConfiguration<PaymentEntry>
{
    public void Configure(EntityTypeBuilder<PaymentEntry> b)
    {
        b.ToTable("payment_entries");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id_entry");
        b.Property(x => x.PaymentId).HasColumnName("id_payments");
        b.Property(x => x.PackageId).HasColumnName("id_package");
        b.Property(x => x.Amount).HasColumnName("amount").HasPrecision(10, 2);
        b.Property(x => x.PaidAt).HasColumnName("paid_at").HasColumnType("date");
        b.Property(x => x.Method).HasColumnName("method").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Note).HasColumnName("note").HasMaxLength(200);
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.HasIndex(x => x.PaidAt);
    }
}

internal sealed class SessionPackageConfiguration : IEntityTypeConfiguration<SessionPackage>
{
    public void Configure(EntityTypeBuilder<SessionPackage> b)
    {
        b.ToTable("session_packages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id_package");
        b.Property(x => x.ClientId).HasColumnName("id_client");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(80).IsRequired();
        b.Property(x => x.TotalSessions).HasColumnName("total_sessions");
        b.Property(x => x.Price).HasColumnName("price").HasPrecision(10, 2);
        b.Property(x => x.PurchasedAt).HasColumnName("purchased_at").HasColumnType("date");
        b.Property(x => x.ExpiresAt).HasColumnName("expires_at").HasColumnType("date");
        b.Ignore(x => x.PaidAmount);
        b.Ignore(x => x.UsedSessions);
        b.Ignore(x => x.RemainingSessions);
        b.HasIndex(x => x.ClientId);

        b.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Entries).WithOne(e => e.Package).HasForeignKey(e => e.PackageId).OnDelete(DeleteBehavior.Cascade);
    }
}
