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
    public class ProdutoConfiguracao : IEntityTypeConfiguration<Produto>
    {
        public void Configure(EntityTypeBuilder<Produto> builder)
        {
            builder.ToTable(nameof(Produto));

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Nome)
                .IsRequired();

            builder.HasOne(x => x.LocalDescarga)
                .WithMany(x => x.Produtos)
                .HasForeignKey(x => x.LocalDescargaId)
                .IsRequired();

            builder.HasMany(x => x.Fornecedores)
                .WithMany(x => x.Produtos)
                .UsingEntity<ProdutoFornecedor>(x => x.HasOne(pf => pf.Fornecedor)
                .WithMany()
                .HasForeignKey(pf => pf.FornecedorId),
                x => x.HasOne(x => x.Produto)
                .WithMany()
                .HasForeignKey(x => x.ProdutoId),
                x =>
                {
                    x.HasKey(pf => new { pf.ProdutoId, pf.FornecedorId });
                    x.ToTable("ProdutoFornecedor");
                });
        }
    }
}
