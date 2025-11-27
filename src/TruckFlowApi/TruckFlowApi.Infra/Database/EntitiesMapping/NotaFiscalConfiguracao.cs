using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Entities;
using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Database.EntitiesMapping
{
    public class NotaFiscalConfiguracao : IEntityTypeConfiguration<NotaFiscal>
    {
        public void Configure(EntityTypeBuilder<NotaFiscal> builder)
        {
            builder.ToTable(nameof(NotaFiscal));
            
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Numero)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .IsRequired(false);

            builder.Property(x => x.DeletedAt)
                .IsRequired(false);

            builder.Property(x => x.TipoCarga)
                .IsRequired();

            builder.Property(x => x.ChaveAcesso)
               .HasMaxLength(44)
               .IsRequired();

            builder.HasIndex(x => x.ChaveAcesso)
                .IsUnique();

            builder.Property(x => x.EmitenteNome)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.EmitenteCnpj)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.DestinatarioNome)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.DestinatarioCpfCnpj)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.ValorTotal)
                .HasPrecision(18, 2);

            builder.Property(x => x.PesoBruto)
                .HasPrecision(18, 3);

            builder.Property(x => x.PesoLiquido)
                .HasPrecision(18, 3);

            builder.Property(x => x.RawXml)
                .HasColumnType("text");

            builder.HasOne<Fornecedor>(x => x.Fornecedor)
                .WithOne(x => x.NotaFiscal)
                .HasForeignKey<NotaFiscal>(x => x.FornecedorId)
                .IsRequired();

            builder.HasOne<Agendamento>(x => x.Agendamento)
                .WithOne(x => x.NotaFiscal)
                .HasForeignKey<Agendamento>(x => x.NotaFiscalId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.HasMany(x => x.Itens)
                .WithOne(x => x.NotaFiscal)
                .HasForeignKey(x => x.NotaFiscalId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
