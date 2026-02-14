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

            // 🔑 PK
            builder.HasKey(x => x.Id);

            // 📦 Quantidade recebida
            builder.Property(x => x.Quantidade)
                .IsRequired()
                .HasPrecision(18, 3);

            builder.Property(x => x.DataRecebimento)
                .IsRequired();


            builder.Property(x => x.Observacao)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.HasOne(x => x.ItemPlanejamento)
                .WithMany(x => x.RecebimentoEventos)
                .HasForeignKey(x => x.ItemPlanejamentoId)
                .OnDelete(DeleteBehavior.Restrict);

            
            builder.HasOne(x => x.Agendamento)
                .WithMany()
                .HasForeignKey(x => x.AgendamentoId)
                .OnDelete(DeleteBehavior.SetNull);

            
            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .IsRequired(false);

            builder.Property(x => x.DeletedAt)
                .IsRequired(false);
        }
    }
}

