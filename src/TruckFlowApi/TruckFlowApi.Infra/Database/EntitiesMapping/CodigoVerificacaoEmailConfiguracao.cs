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
    public sealed class CodigoVerificacaoEmailConfiguracao : IEntityTypeConfiguration<CodigoVerificacaoEmail>
    {
        public void Configure(EntityTypeBuilder<CodigoVerificacaoEmail> builder)
        {
            builder.ToTable("CodigoVerificacaoEmail");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.CodigoHash)
                .IsRequired()
                .HasMaxLength(64);

            builder.Property(x => x.Finalidade)
                .IsRequired();

            builder.Property(x => x.Tentativas)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.ExpiraEm)
                .IsRequired();

            builder.Property(x => x.CriadoEm)
                .IsRequired();

            builder.HasOne(x => x.Usuario)
                .WithMany()
                .HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.UsuarioId);
            builder.HasIndex(x => new { x.UsuarioId, x.Finalidade, x.UsadoEm });
        }
    }
}
