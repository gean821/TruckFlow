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
    internal class ItemPlanejamentoConfiguracao : IEntityTypeConfiguration<ItemPlanejamento>
    {
        public void Configure(EntityTypeBuilder<ItemPlanejamento> builder)
        {
            builder.ToTable(nameof(ItemPlanejamento));

            builder.HasKey(x => x.Id);

            builder.Property(x => x.QuantidadeTotalPlanejada)
                .HasPrecision(18, 2);
            
            builder.Property(x => x.CadenciaDiariaPlanejada)
                .HasPrecision(18, 2);
            
            builder.Property(x => x.QuantidadeTotalRecebida)
                .HasPrecision(18, 2);


            builder.HasOne(x => x.PlanejamentoRecebimento)
                .WithMany(x => x.ItemPlanejamentos)
                .HasForeignKey(x => x.PlanejamentoRecebimentoId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasOne(x => x.Produto)
                .WithMany(x => x.ItemPlanejamentos)
                .HasForeignKey(x => x.ProdutoId);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .IsRequired(false);

            builder.Property(x => x.DeletedAt)
                .IsRequired(false);
        }
    }
}
