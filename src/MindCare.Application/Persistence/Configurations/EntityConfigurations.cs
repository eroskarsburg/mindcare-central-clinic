using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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
        b.HasIndex(x => x.ScheduledAt);

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
