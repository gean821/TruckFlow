using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Database.EntitiesMapping
{
    public class NotificacaoConfiguracao : IEntityTypeConfiguration<Notificacao>
    {
        public void Configure(EntityTypeBuilder<Notificacao> builder)
        {
            builder.ToTable(nameof(Notificacao));

            builder.HasKey(x => x.Id);

            builder.Property<uint>("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            builder.Property(x => x.EmpresaId)
                .IsRequired();

            builder.Property(x => x.DestinatarioUsuarioId)
                .IsRequired();

            builder.Property(x => x.Tipo)
                .IsRequired();

            builder.Property(x => x.Prioridade)
                .IsRequired();

            builder.Property(x => x.Titulo)
                .IsRequired()
                .HasMaxLength(120);

            builder.Property(x => x.Corpo)
                .IsRequired();

            builder.Property(x => x.Payload)
                .IsRequired()
                .HasColumnType("jsonb");

            builder.Property(x => x.LidaEm)
                .IsRequired(false);

            builder.Property(x => x.CorrelationId)
                .IsRequired(false);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.HasOne(x => x.Destinatario)
                .WithMany()
                .HasForeignKey(x => x.DestinatarioUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Entregas)
                .WithOne(x => x.Notificacao)
                .HasForeignKey(x => x.NotificacaoId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.EmpresaId, x.DestinatarioUsuarioId, x.CreatedAt })
                .HasDatabaseName("IX_Notificacao_FeedDestinatario")
                .IsDescending(false, false, true);

            builder.HasIndex(x => new { x.EmpresaId, x.DestinatarioUsuarioId })
                .HasDatabaseName("IX_Notificacao_Naolidas")
                .HasFilter("\"LidaEm\" IS NULL");
        }
    }
}
