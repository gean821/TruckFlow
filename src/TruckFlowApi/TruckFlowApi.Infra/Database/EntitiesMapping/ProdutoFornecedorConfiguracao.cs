using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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

            builder.Property(x => x.CodigoFornecedor)
                .HasMaxLength(50);

            builder.Property(x => x.EanFornecedor)
                .HasMaxLength(20);

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

            builder.HasIndex(x => new { x.FornecedorId, x.CodigoFornecedor });
        }
    }
}
