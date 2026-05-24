using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Database.EntitiesMapping
{
    public class DispositivoUsuarioConfiguracao : IEntityTypeConfiguration<DispositivoUsuario>
    {
        public void Configure(EntityTypeBuilder<DispositivoUsuario> builder)
        {
            builder.ToTable(nameof(DispositivoUsuario));

            builder.HasKey(x => x.Id);

            builder.Property<uint>("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            builder.Property(x => x.UsuarioId)
                .IsRequired();

            builder.Property(x => x.ExpoPushToken)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Plataforma)
                .IsRequired();

            builder.Property(x => x.AppVersion)
                .IsRequired(false)
                .HasMaxLength(50);

            builder.Property(x => x.UltimoUsoEm)
                .IsRequired();

            builder.Property(x => x.Ativo)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.HasOne(x => x.Usuario)
                .WithMany()
                .HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.ExpoPushToken)
                .IsUnique()
                .HasDatabaseName("IX_DispositivoUsuario_ExpoPushToken");

            builder.HasIndex(x => x.UsuarioId)
                .HasDatabaseName("IX_DispositivoUsuario_UsuarioAtivo")
                .HasFilter("\"Ativo\" = true");
        }
    }
}