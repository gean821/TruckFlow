using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Database.EntitiesMapping
{
    public sealed class AuditLogConfiguracao : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable(nameof(AuditLog));

            builder.HasKey(x => x.Id);

            builder.Property(x => x.EntityName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.EntityId)
                .IsRequired()
                .HasMaxLength(64);

            builder.Property(x => x.Action)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(x => x.ChangesJson)
                .HasColumnType("jsonb");

            builder.Property(x => x.Timestamp)
                .IsRequired();

            builder.Property(x => x.IpAddress)
                .HasMaxLength(64);

            builder.Property(x => x.UserAgent)
                .HasMaxLength(512);

            builder.HasOne(x => x.Empresa)
                .WithMany()
                .HasForeignKey(x => x.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.EmpresaId, x.Timestamp });
            builder.HasIndex(x => new { x.EmpresaId, x.EntityName, x.EntityId });
            builder.HasIndex(x => new { x.EmpresaId, x.UserId });
        }
    }
}
