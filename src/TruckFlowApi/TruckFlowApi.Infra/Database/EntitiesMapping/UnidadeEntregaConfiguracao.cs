using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Database.Configurations
{
    public class UnidadeEntregaConfiguracao : IEntityTypeConfiguration<UnidadeEntrega>
    {
        public void Configure(EntityTypeBuilder<UnidadeEntrega> builder)
        {
            builder.ToTable(nameof(UnidadeEntrega));

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Nome)
                .IsRequired();

            builder.Property(x => x.Localizacao)
                .IsRequired();
        }
    }
}
