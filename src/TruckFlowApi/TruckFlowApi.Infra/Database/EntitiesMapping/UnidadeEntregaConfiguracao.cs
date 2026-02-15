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
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Localizacao)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(x => x.Logradouro).HasMaxLength(200);
            builder.Property(x => x.Numero).HasMaxLength(20);
            builder.Property(x => x.Complemento).HasMaxLength(100);
            builder.Property(x => x.Bairro).HasMaxLength(150);
            builder.Property(x => x.Cidade).HasMaxLength(150);
            builder.Property(x => x.Estado).HasMaxLength(50);
            builder.Property(x => x.Cep).HasMaxLength(20);

            builder.Property(x => x.Latitude);
            builder.Property(x => x.Longitude);

            builder.HasOne(x => x.Empresa)
                .WithMany(x => x.Unidades)
                .HasForeignKey(x => x.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.LocaisDeDescarga)
                .WithOne(x => x.UnidadeEntrega)
                .HasForeignKey(x => x.UnidadeEntregaId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
