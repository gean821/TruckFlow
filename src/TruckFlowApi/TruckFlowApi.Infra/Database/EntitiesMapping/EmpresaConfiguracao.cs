using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Database.EntitiesMapping
{
    public sealed class EmpresaConfiguracao : IEntityTypeConfiguration<Empresa>
    {
        public void Configure(EntityTypeBuilder<Empresa> builder)
        {
            builder.ToTable(nameof(Empresa));

            builder.HasKey(x => x.Id);

            builder.Property(x => x.RazaoSocial)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.NomeFantasia)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Cnpj)
                .IsRequired()
                .HasMaxLength(18);

            builder.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Logradouro)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Numero)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.Bairro)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.Cidade)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.Estado)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.Cep)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.InscricaoEstadual)
                .HasMaxLength(50);

            builder.Property(x => x.InscricaoMunicipal)
                .HasMaxLength(50);

            builder.Property(x => x.Telefone)
                .HasMaxLength(20);

            builder.Property(x => x.Complemento)
                .HasMaxLength(100);

            builder.Property(x => x.Latitude);
            builder.Property(x => x.Longitude);

            builder.Property(x => x.Ativa)
                .IsRequired();

            builder.HasIndex(x => x.Cnpj)
                .IsUnique();

            builder.HasIndex(x => x.Email);


            builder.HasMany(x => x.Unidades)
                .WithOne(x => x.Empresa)
                .HasForeignKey(x => x.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Fornecedores)
                .WithOne(x => x.Empresa)
                .HasForeignKey(x => x.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Produtos)
                .WithOne(x => x.Empresa)
                .HasForeignKey(x => x.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Usuarios)
                .WithOne(x => x.Empresa)
                .HasForeignKey(x => x.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
