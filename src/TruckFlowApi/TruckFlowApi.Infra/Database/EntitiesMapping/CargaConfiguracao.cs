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
    public class CargaConfiguracao : IEntityTypeConfiguration<Carga>
    {
        public void Configure(EntityTypeBuilder<Carga> builder)
        {
            builder.ToTable(nameof(Carga));
            
            builder.HasKey(x => x.Id);
            builder.Property(x => x.TipoCarga)
                .IsRequired();
        }
    }
}
