using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;
using TruckFlow.Infrastructure.Entities;

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

            builder.Property(x => x.DeletedAt).IsRequired(false);

        }
    }
}
