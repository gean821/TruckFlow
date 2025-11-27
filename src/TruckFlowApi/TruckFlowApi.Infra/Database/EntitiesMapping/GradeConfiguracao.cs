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
    public sealed class GradeConfiguracao : IEntityTypeConfiguration<Grade>
    {
        public void Configure(EntityTypeBuilder<Grade> builder)
        {
            builder.ToTable(nameof(Grade));

            builder.HasKey(x => x.Id);

            builder.Property(x => x.DataInicio)
                .HasColumnType("date")
                .IsRequired();

            builder.Property(x => x.DataFim)
                .HasColumnType("date")
                .IsRequired();

            builder.Property(x => x.HoraInicial)
                .HasColumnType("time")
                .IsRequired();

            builder.Property(x => x.HoraFinal)
                .HasColumnType("time")
                .IsRequired();

            builder.Property(x => x.IntervaloMinutos)
                .HasColumnType("int")
                .IsRequired();


            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.DeletedAt)
                .IsRequired(false);

            builder.Property(x => x.UpdatedAt)
            .IsRequired(false);

            builder.HasOne(x => x.Produto)
               .WithMany(x => x.Grades)
               .HasForeignKey(x => x.ProdutoId)
               .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Produto)
                .WithMany(x => x.Grades)
                .HasForeignKey(x => x.ProdutoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
