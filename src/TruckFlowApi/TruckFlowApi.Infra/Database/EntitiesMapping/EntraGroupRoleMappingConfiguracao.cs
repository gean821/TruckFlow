using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Database.EntitiesMapping
{
    public sealed class EntraGroupRoleMappingConfiguracao : IEntityTypeConfiguration<EntraGroupRoleMapping>
    {
        public void Configure(EntityTypeBuilder<EntraGroupRoleMapping> builder)
        {
            builder.ToTable(nameof(EntraGroupRoleMapping));

            builder.HasKey(x => x.Id);

            builder.Property(x => x.EntraGroupId)
                .IsRequired()
                .HasMaxLength(64);

            builder.Property(x => x.EntraGroupNome)
                .IsRequired()
                .HasMaxLength(128);

            builder.Property(x => x.RoleName)
                .IsRequired()
                .HasMaxLength(32);

            builder.Property(x => x.Ativo)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.HasOne(x => x.Empresa)
                .WithMany()
                .HasForeignKey(x => x.EmpresaId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.EntraGroupId).IsUnique();
            builder.HasIndex(x => x.EmpresaId);
        }
    }
}
