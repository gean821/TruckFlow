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
    public class LocalDescargaConfiguracao : IEntityTypeConfiguration<LocalDescarga>
    {
        public void Configure(EntityTypeBuilder<LocalDescarga> builder)
        {
            builder.ToTable(nameof(LocalDescarga));

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Nome)
                .IsRequired()
                .HasMaxLength(300);

            builder.HasOne(x => x.Empresa)
                .WithMany()
                .HasForeignKey(x => x.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.EmpresaId);

            builder.Property(x => x.UnidadeEntregaId)
                .IsRequired();

            builder.HasMany(x => x.Produtos)
                .WithOne(p => p.LocalDescarga)
                .HasForeignKey(p => p.LocalDescargaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Grades)
                .WithOne(g => g.LocalDescarga)
                .HasForeignKey(g => g.LocalDescargaId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

