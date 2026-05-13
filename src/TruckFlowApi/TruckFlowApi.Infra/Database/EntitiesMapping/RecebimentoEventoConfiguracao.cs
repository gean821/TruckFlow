using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Database.EntitiesMapping
{
    public sealed class RecebimentoEventoConfiguracao : IEntityTypeConfiguration<RecebimentoEvento>
    {
        public void Configure(EntityTypeBuilder<RecebimentoEvento> builder)
        {
            builder.ToTable(nameof(RecebimentoEvento));

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Quantidade)
                .IsRequired()
                .HasPrecision(18, 3);

            builder.Property(x => x.DataRecebimento)
                .IsRequired();

            builder.Property(x => x.Tipo)
                .HasConversion<int>()
                .IsRequired()
                .HasSentinel((TruckFlow.Domain.Enums.TipoMovimentoRecebimento)0)
                .HasDefaultValue(TruckFlow.Domain.Enums.TipoMovimentoRecebimento.Reserva);

            builder.Property(x => x.Observacao)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.HasOne(x => x.ItemPlanejamento)
                .WithMany(x => x.RecebimentoEventos)
                .HasForeignKey(x => x.ItemPlanejamentoId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.HasOne(x => x.Agendamento)
                .WithMany()
                .HasForeignKey(x => x.AgendamentoId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.Produto)
                .WithMany()
                .HasForeignKey(x => x.ProdutoId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.HasOne(x => x.Fornecedor)
                .WithMany()
                .HasForeignKey(x => x.FornecedorId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.HasOne(x => x.Empresa)
                .WithMany()
                .HasForeignKey(x => x.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.AgendamentoId, x.Tipo })
                .IsUnique()
                .HasFilter("\"AgendamentoId\" IS NOT NULL");

            builder.HasIndex(x => new { x.EmpresaId, x.ItemPlanejamentoId })
                .HasFilter("\"ItemPlanejamentoId\" IS NULL");

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .IsRequired(false);

            builder.Property(x => x.DeletedAt)
                .IsRequired(false);
        }
    }
}
