using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;
using TruckFlow.Infrastructure.Entities;
using TruckFlowApi.Infra.Database.Configurations;

namespace TruckFlowApi.Infra.Database
{
    public class AppDbContext : IdentityDbContext<Usuario>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new UsuarioConfiguracao());
            modelBuilder.ApplyConfiguration(new AgendamentoConfiguracao());
            modelBuilder.ApplyConfiguration(new CargaConfiguracao());
            modelBuilder.ApplyConfiguration(new VeiculoConfiguracao());
            modelBuilder.ApplyConfiguration(new AdministradorConfiguracao());
            modelBuilder.ApplyConfiguration(new NotificacaoConfiguracao());
            modelBuilder.ApplyConfiguration(new ProdutoConfiguracao());
            modelBuilder.ApplyConfiguration(new NotaFiscalConfiguracao());
            modelBuilder.ApplyConfiguration(new UnidadeEntregaConfiguracao());
        }

        public DbSet<Usuario> Usuario { get; set; }
        public DbSet<Veiculo> Veiculo { get; set; }
        public DbSet<Produto> Produto { get; set; }
        public DbSet<Fornecedor> Fornecedor { get; set; }
        public DbSet<Carga> Carga { get; set; }
        public DbSet<UnidadeEntrega> UnidadeEntrega { get; set; }
        public DbSet<Agendamento> Agendamento { get; set; }
        public DbSet<Administrador> Administrador { get; set; }
        public DbSet<NotaFiscal> NotaFiscal { get; set; }
        public DbSet<Notificacao> Notificacao { get; set; }
        public DbSet<LocalDescarga> LocalDescarga { get; set; }
    }
}
    

