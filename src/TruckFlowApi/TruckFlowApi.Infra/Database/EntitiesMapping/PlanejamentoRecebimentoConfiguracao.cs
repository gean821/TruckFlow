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
    internal class PlanejamentoRecebimentoConfiguracao : IEntityTypeConfiguration<PlanejamentoRecebimento>
    {
        public void Configure(EntityTypeBuilder<PlanejamentoRecebimento> builder)
        {
            builder.ToTable(nameof(PlanejamentoRecebimento));

            builder.HasKey(x => x.Id);

            builder.Property(x => x.DataInicio)
                .IsRequired();

            builder.Property(x => x.StatusRecebimento);

            builder.Property(x => x.CreatedAt)
             .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .IsRequired(false);

            builder.Property(x => x.DeletedAt)
                .IsRequired(false);

            builder.HasOne(x => x.Fornecedor)
                .WithMany(x => x.Recebimentos)
                .HasForeignKey(x => x.FornecedorId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
