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
    public class AdministradorConfiguracao : IEntityTypeConfiguration<Administrador>
    {
        public void Configure(EntityTypeBuilder<Administrador> builder)
        {
            builder.ToTable(nameof(Administrador));
            
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Nome)
                .IsRequired()
                .HasMaxLength(250);

            builder.Property(x => x.FuncaoAdm)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .IsRequired(false);

            builder.Property(x => x.DeletedAt)
                .IsRequired(false);

            builder.HasOne(x => x.Usuario)
                .WithOne(x => x.Administrador)
                .HasForeignKey<Administrador>(x => x.UsuarioId)
                .IsRequired(false);
        }
    }
}
