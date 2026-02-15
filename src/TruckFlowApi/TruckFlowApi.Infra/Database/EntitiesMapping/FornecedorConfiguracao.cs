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
    public class FornecedorConfiguracao : IEntityTypeConfiguration<Fornecedor>
    {
        public void Configure(EntityTypeBuilder<Fornecedor> builder)
        {
            builder.ToTable(nameof(Fornecedor));
            
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Nome)
                .IsRequired().
                HasMaxLength(250);

            builder.Property(x => x.Cnpj)
                .IsRequired()
                .HasMaxLength(18);

            builder.HasOne(x => x.Empresa)
                .WithMany(x => x.Fornecedores)
                .HasForeignKey(x => x.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.ProdutoFornecedores)
                    .WithOne(x => x.Fornecedor)
                    .HasForeignKey(x => x.FornecedorId)
                    .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
