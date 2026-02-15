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
    public class ProdutoFornecedorConfiguracao
     : IEntityTypeConfiguration<ProdutoFornecedor>
    {
        public void Configure(EntityTypeBuilder<ProdutoFornecedor> builder)
        {
            builder.ToTable("ProdutoFornecedor");

            builder.HasKey(x => new { x.ProdutoId, x.FornecedorId });

            builder.HasOne(x => x.Produto)
    .WithMany(x => x.ProdutoFornecedores)
    .HasForeignKey(x => x.ProdutoId)
    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Fornecedor)
                .WithMany(x => x.ProdutoFornecedores)
                .HasForeignKey(x => x.FornecedorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Empresa)
                .WithMany()
                .HasForeignKey(x => x.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.CreatedAt)
                .IsRequired();
        }
    }
}
