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

            builder.Property(x => x.DiasSemana)
                .HasColumnType("varchar(20)")
                .IsRequired();

            builder.HasOne(x => x.UnidadeEntrega)
                .WithMany(x => x.Grades)
                .HasForeignKey(x => x.UnidadeEntregaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.LocalDescarga)
               .WithMany(x => x.Grades)
               .HasForeignKey(x => x.LocalDescargaId)
               .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(g => g.Agendamentos)
                .WithOne(a => a.Grade)
                .HasForeignKey(a => a.GradeId)
                .OnDelete(DeleteBehavior.Restrict); 

            builder.HasOne(x => x.Produto)
               .WithMany(x => x.Grades)
               .HasForeignKey(x => x.ProdutoId)
               .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Empresa)
                .WithMany()
                .HasForeignKey(x => x.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
