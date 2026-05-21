using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Database.EntitiesMapping
{
    public class OutboxEventConfiguracao : IEntityTypeConfiguration<OutboxEvent>
    {
        public void Configure(EntityTypeBuilder<OutboxEvent> builder)
        {
            builder.ToTable(nameof(OutboxEvent));

            builder.HasKey(x => x.Id);

            builder.Property<uint>("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            builder.Property(x => x.EventType)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Payload)
                .IsRequired()
                .HasColumnType("jsonb");

            builder.Property(x => x.IdempotencyKey)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.EmpresaId)
                .IsRequired();

            builder.Property(x => x.OcorridoEm)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.ProcessedAt)
                .IsRequired(false);

            builder.Property(x => x.Tentativas)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.ProximaTentativaEm)
                .IsRequired(false);

            builder.Property(x => x.UltimoErro)
                .IsRequired(false);

            builder.Property(x => x.CorrelationId)
                .IsRequired(false);

            builder.HasIndex(x => x.IdempotencyKey)
                .IsUnique()
                .HasDatabaseName("IX_OutboxEvent_IdempotencyKey");

            builder.HasIndex(x => new { x.ProcessedAt, x.ProximaTentativaEm })
                .HasDatabaseName("IX_OutboxEvent_Claim_Pending")
                .HasFilter("\"ProcessedAt\" IS NULL");
        }
    }
}
