﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Entities;

namespace TruckFlowApi.Infra.Database.EntitiesMapping
{
    public sealed class NotaFiscalItemConfiguracao : IEntityTypeConfiguration<NotaFiscalItem>
    {
        public void Configure(EntityTypeBuilder<NotaFiscalItem> builder)
        {
            builder.ToTable(nameof(NotaFiscalItem));
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Codigo)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.Descricao)
                .HasMaxLength(300)
                .IsRequired();

            builder.Property(x => x.ValorUnitario)
                .HasPrecision(18, 2);

            builder.Property(x => x.ValorTotal)
                .HasPrecision(18, 2);

            builder.HasOne(x => x.NotaFiscal)
                .WithMany(x => x.Itens)
                .HasForeignKey(x => x.NotaFiscalId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

