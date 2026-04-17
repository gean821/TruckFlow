using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TruckFlow.Application;
using TruckFlow.Configuration;
using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Entities;
using TruckFlow.Extensions.Agendamento;
using TruckFlow.Extensions.Auth;
using TruckFlow.Extensions.Cors;
using TruckFlow.Extensions.Dashboard;
using TruckFlow.Extensions.Empresa;
using TruckFlow.Extensions.Fornecedor;
using TruckFlow.Extensions.Grade;
using TruckFlow.Extensions.Guards;
using TruckFlow.Extensions.ItemPlanejamento;
using TruckFlow.Extensions.LocalDescarga;
using TruckFlow.Extensions.NotaFiscal;
using TruckFlow.Extensions.Produto;
using TruckFlow.Extensions.Recebimento;
using TruckFlow.Extensions.RecebimentoEvento;
using TruckFlow.Extensions.Saas;
using TruckFlow.Extensions.UnidadeEntrega;
using TruckFlow.Extensions.UserContext;
using TruckFlow.Filters;
using TruckFlow.Middlewares;
using TruckFlowApi.Infra.Database;


namespace TruckFlow
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.ListenAnyIP(8080);
            });

            builder.AddSerilogLogging();

            // Add services to the container.

            builder.Services.AddCorsDependency();
            builder.Services.AddProduto();
            builder.Services.AddLocalDescarga();
            builder.Services.AddFornecedor();
            builder.Services.AddPlanejamentoRecebimento();
            builder.Services.AddItemPlanejameto();
            builder.Services.AddUnidadeEntrega();
            builder.Services.AddGrade();
            builder.Services.AddNotaFiscal();
            builder.Services.AddAgendamento();
            builder.Services.AddUserAuth();
            builder.Services.AddDashboard();
            builder.Services.AddRecebimentoEvento();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<IEmpresaContext, EmpresaContext>();
            builder.Services.AddEmpresa();
            builder.Services.AddUserContext();
            builder.Services.AddSaas();
            builder.Services.AddSecurity();

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(x =>
            {
                x.OperationFilter<FileUploadOperationFilter>();
            });

            builder.Services.AddDbContext<AppDbContext>(optionsBuilder =>
            {
                optionsBuilder.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            builder.Services.AddIdentity<Usuario, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
            })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            builder.AddAuthenticationJwt();
            builder.Services.AddAuthorization();
            builder.Services.AddTransient<RequestLoggingMiddleware>();
            builder.Services.AddScoped<ActionLoggingFilter>();

            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<ActionLoggingFilter>();
            })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                });

            var app = builder.Build();
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
            }

            app.UseRouting();
            app.UseCors("AllowFrontend");
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseMiddleware<RequestLoggingMiddleware>();

            // Configure the HTTP request pipeline.

            app.UseSwagger();
            app.UseSwaggerUI();

            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            if (app.Environment.IsDevelopment())
            {
                app.Run();
            }
            else
            {
                var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
                app.Run($"http://0.0.0.0:{port}");
            }
        }
    }
}
