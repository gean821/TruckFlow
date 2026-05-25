using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;

namespace TruckFlowApi.Infra.Database.EntitiesMapping
{
    public class NotificacaoEntregaConfiguracao : IEntityTypeConfiguration<NotificacaoEntrega>
    {
        public void Configure(EntityTypeBuilder<NotificacaoEntrega> builder)
        {
            builder.ToTable(nameof(NotificacaoEntrega));

            builder.HasKey(x => x.Id);

            builder.Property<uint>("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            builder.Property(x => x.EmpresaId)
                .IsRequired();

            builder.Property(x => x.NotificacaoId)
                .IsRequired();

            builder.Property(x => x.Canal)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired()
                .HasDefaultValue(StatusEntregaNotificacao.Pendente);

            builder.Property(x => x.TentativasEfetuadas)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.ProximaTentativaEm)
                .IsRequired(false);

            builder.Property(x => x.UltimaTentativaEm)
                .IsRequired(false);

            builder.Property(x => x.ProviderMessageId)
                .IsRequired(false)
                .HasMaxLength(200);

            builder.Property(x => x.Erro)
                .IsRequired(false);

            builder.Property(x => x.ReceiptCheckedAt)
                .IsRequired(false);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.HasIndex(x => new { x.Status, x.ProximaTentativaEm })
                .HasDatabaseName("IX_NotificacaoEntrega_Dispatcher_Pendente")
                .HasFilter("\"Status\" = 0");

            builder.HasIndex(x => x.NotificacaoId)
                .HasDatabaseName("IX_NotificacaoEntrega_Notificacao");

            builder.HasIndex(x => x.UltimaTentativaEm)
                .HasDatabaseName("IX_NotificacaoEntrega_Receipt_Pending")
                .HasFilter("\"Status\" = 1 AND \"Canal\" = 2 AND \"ReceiptCheckedAt\" IS NULL");
        }
    }
}
