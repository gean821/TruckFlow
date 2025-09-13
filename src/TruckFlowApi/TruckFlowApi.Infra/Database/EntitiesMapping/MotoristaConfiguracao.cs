using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;
using TruckFlow.Infrastructure.Entities;

namespace TruckFlowApi.Infra.Database.Configurations
{
    public class MotoristaConfiguracao : IEntityTypeConfiguration<Motorista>
    {
        public void Configure(EntityTypeBuilder<Motorista> builder)
        {
            builder.ToTable(nameof(Motorista));
            
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Nome)
                .IsRequired()
                .HasMaxLength(250);

            builder.Property(x => x.Telefone)
                .IsRequired().
                HasMaxLength(11);

            builder.Property(x => x.CreatedAt)
                .IsRequired();
            
            builder.Property(x => x.UpdatedAt)
                .IsRequired(false);

            builder.Property(x => x.DeletedAt)
                .IsRequired(false);

            builder.HasOne<Veiculo>(x => x.Veiculo)
                .WithOne(x => x.Motorista)
                .HasForeignKey<Veiculo>(x => x.MotoristaId);

            builder.HasMany<Agendamento>(x => x.Agendamentos)
                .WithOne(x => x.Usuario.Motorista)
                .HasForeignKey(x => x.UsuarioId)
                .IsRequired(false);

            builder.HasOne<Usuario>(x => x.Usuario)
                .WithOne(x => x.Motorista)
                .HasForeignKey<Motorista>(x => x.UsuarioId)
                .IsRequired();
        }
    }
}
