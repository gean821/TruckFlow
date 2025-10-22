using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Entities;

namespace TruckFlowApi.Infra.Database.Configurations
{
    public class NotificacaoConfiguracao : IEntityTypeConfiguration<Notificacao>
    {
        public void Configure(EntityTypeBuilder<Notificacao> builder)
        {
            builder.ToTable(nameof(Notificacao));
            
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Descricao)
                .IsRequired();

            builder.HasOne<Agendamento>(x => x.Agendamento)
                .WithMany(x => x.Notificacoes)
                .HasForeignKey(x => x.AgendamentoId)
                .IsRequired(false);
        }
    }
}
