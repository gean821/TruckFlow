using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Database.EntitiesMapping
{
    public sealed class NotaFiscalItemConfiguracao : IEntityTypeConfiguration<NotaFiscalItem>
    {
        public void Configure(EntityTypeBuilder<NotaFiscalItem> builder)
        {
            builder.ToTable(nameof(NotaFiscalItem));
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Codigo)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.Ean)
                .HasMaxLength(20);

            builder.Property(x => x.Descricao)
                .HasMaxLength(300)
                .IsRequired();

            builder.Property(x => x.ValorUnitario)
                .HasPrecision(18, 2);

            builder.Property(x => x.ValorTotal)
                .HasPrecision(18, 2);

            builder.Property(x => x.Quantidade)
                .HasPrecision(18, 3);

            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(30);

            builder.Property(x => x.OrigemMatch)
                .HasConversion<string>()
                .HasMaxLength(30);

            builder.HasOne(x => x.NotaFiscal)
                .WithMany(x => x.Itens)
                .HasForeignKey(x => x.NotaFiscalId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Produto)
                .WithMany()
                .HasForeignKey(x => x.ProdutoId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Property(x => x.EmpresaId)
                   .IsRequired();

            builder.HasOne(x => x.Empresa)
                .WithMany()
                .HasForeignKey(x => x.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.EmpresaId);
            builder.HasIndex(x => x.ProdutoId);
            builder.HasIndex(x => new { x.EmpresaId, x.Status });
        }
    }
}
