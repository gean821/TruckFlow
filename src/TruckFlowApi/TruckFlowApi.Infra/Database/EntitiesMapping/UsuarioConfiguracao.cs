using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Entities;

namespace TruckFlowApi.Infra.Database.EntitiesMapping
{
    public class UsuarioConfiguracao : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable(nameof(Usuario));

            builder.HasKey(x => x.Id);
         
            builder.Property(x => x.UserName)
                .IsRequired().HasMaxLength(200);

            builder.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(x => x.CreatedAt)
                    .IsRequired();
            
            builder.Property(x => x.UpdatedAt)
                    .IsRequired(false);

            builder.Property(x => x.DeletedAt)
                    .IsRequired(false);

            builder.HasOne(u => u.Motorista)
                    .WithOne(m => m.Usuario)
                    .HasForeignKey<Motorista>(m => m.UsuarioId)
                    .IsRequired(false);

            builder.HasOne(u => u.Administrador)
                    .WithOne(a => a.Usuario)
                    .HasForeignKey<Administrador>(a => a.UsuarioId)
                    .IsRequired(false);

            builder.HasMany(u => u.Agendamentos)
                    .WithOne(a => a.Usuario)
                    .HasForeignKey(a => a.UsuarioId)
                    .IsRequired(false);
        }
    }
}
