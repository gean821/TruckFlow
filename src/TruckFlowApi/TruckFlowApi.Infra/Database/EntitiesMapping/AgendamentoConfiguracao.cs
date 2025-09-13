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
    public class AgendamentoConfiguracao : IEntityTypeConfiguration<Agendamento>
    {
        public void Configure(EntityTypeBuilder<Agendamento> builder)
        {
            builder.ToTable(nameof(Agendamento));

            builder.HasKey(x => x.Id);

            builder.Property(x => x.StatusAgendamento)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .IsRequired(false);

            builder.Property(x => x.DeletedAt)
                .IsRequired(false);


            builder.HasOne<Usuario>(x => x.Usuario)
                .WithMany(x => x.Agendamentos)
                .HasForeignKey(x => x.UsuarioId)
                .IsRequired(false);

            builder.HasOne<Fornecedor>(x => x.Fornecedor)
                .WithOne(x => x.Agendamento)
                .HasForeignKey<Agendamento>(x => x.FornecedorId)
                .IsRequired();

            builder.Property(x => x.TipoCarga)
                .IsRequired();

            builder.Property(x => x.VolumeCarga)
                .IsRequired();

            builder.HasOne<UnidadeEntrega>(x => x.UnidadeEntrega)
                .WithOne(x => x.Agendamento)
                .IsRequired();

            builder.HasOne(x => x.NotaFiscal)
                .WithOne(x => x.Agendamento)
                .HasForeignKey<Agendamento>(x => x.NotaFiscalId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
