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
    public class VeiculoConfiguracao : IEntityTypeConfiguration<Veiculo>
    {
        public void Configure(EntityTypeBuilder<Veiculo> builder)
        {

            builder.ToTable(nameof(Veiculo));

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Nome)
                .IsRequired();

            builder.Property(x => x.Placa)
                .IsRequired()
                .HasMaxLength(7);

            builder.Property(x => x.TipoVeiculo)
                .IsRequired();

            builder.HasOne<Motorista>(x => x.Motorista)
                .WithOne(x => x.Veiculo)
                .HasForeignKey<Veiculo>(x => x.MotoristaId)
                .IsRequired();
        }
    }
}
