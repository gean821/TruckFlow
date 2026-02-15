using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Database.Configurations;
using TruckFlowApi.Infra.Database.EntitiesMapping;

namespace TruckFlowApi.Infra.Database
{
    public class AppDbContext : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>
    {
        private readonly IEmpresaContext _empresaContext;
        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            IEmpresaContext empresaContext
            ) : base(options)
        {
            _empresaContext = empresaContext;
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            ApplyGlobalFilters(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            modelBuilder.ApplyConfiguration(new UsuarioConfiguracao());
            modelBuilder.ApplyConfiguration(new AgendamentoConfiguracao());
            modelBuilder.ApplyConfiguration(new CargaConfiguracao());
            modelBuilder.ApplyConfiguration(new MotoristaConfiguracao());
            modelBuilder.ApplyConfiguration(new VeiculoConfiguracao());
            modelBuilder.ApplyConfiguration(new AdministradorConfiguracao());
            modelBuilder.ApplyConfiguration(new NotificacaoConfiguracao());
            modelBuilder.ApplyConfiguration(new ProdutoConfiguracao());
            modelBuilder.ApplyConfiguration(new NotaFiscalConfiguracao());
            modelBuilder.ApplyConfiguration(new UnidadeEntregaConfiguracao());
            modelBuilder.ApplyConfiguration(new NotaFiscalItemConfiguracao());
            modelBuilder.ApplyConfiguration(new PlanejamentoRecebimentoConfiguracao());
            modelBuilder.ApplyConfiguration(new ItemPlanejamentoConfiguracao());
            modelBuilder.ApplyConfiguration(new GradeConfiguracao());
            modelBuilder.ApplyConfiguration(new FornecedorConfiguracao());
            modelBuilder.ApplyConfiguration(new LocalDescargaConfiguracao());
            modelBuilder.ApplyConfiguration(new ProdutoFornecedorConfiguracao());
        }

        public DbSet<Usuario> Usuario { get; set; }
        public DbSet<Veiculo> Veiculo { get; set; }
        public DbSet<Motorista> Motorista { get; set; }
        public DbSet<Produto> Produto { get; set; }
        public DbSet<Fornecedor> Fornecedor { get; set; }
        public DbSet<Carga> Carga { get; set; }
        public DbSet<UnidadeEntrega> UnidadeEntrega { get; set; }
        public DbSet<Grade> Grade { get; set; }
        public DbSet<Agendamento> Agendamento { get; set; }
        public DbSet<Administrador> Administrador { get; set; }
        public DbSet<NotaFiscal> NotaFiscal { get; set; }
        public DbSet<Notificacao> Notificacao { get; set; }
        public DbSet<LocalDescarga> LocalDescarga { get; set; }
        public DbSet<NotaFiscalItem> NotaFiscalItems { get; set; }
        public DbSet<PlanejamentoRecebimento> PlanejamentosRecebimento { get; set; }
        public DbSet<ItemPlanejamento> ItensPlanejamento { get; set; }
        public DbSet<RecebimentoEvento> RecebimentoEvento { get; set; }

        private void ApplyGlobalFilters(ModelBuilder modelBuilder)
        {
            var empresaScopedEntities = modelBuilder.Model
                .GetEntityTypes()
                .Where(t => typeof(IEmpresaScoped).IsAssignableFrom(t.ClrType));

            foreach (var entityType in empresaScopedEntities)
            {
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(SetGlobalFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(entityType.ClrType);

                method.Invoke(this, new object[] { modelBuilder });
            }
        }

        private void SetGlobalFilter<TEntity>(ModelBuilder modelBuilder)
            where TEntity : class, IEmpresaScoped
        {
            modelBuilder.Entity<TEntity>()
                .HasQueryFilter(e =>
                _empresaContext.EmpresaId == Guid.Empty
                || e.EmpresaId == _empresaContext.EmpresaId);
        }
    }
}