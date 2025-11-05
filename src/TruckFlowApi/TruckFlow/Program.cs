using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TruckFlow.Application.Auth;
using TruckFlow.Application.Entities;
using TruckFlow.Extensions.Auth;
using TruckFlow.Extensions.Cors;
using TruckFlow.Extensions.Fornecedor;
using TruckFlow.Extensions.LocalDescarga;
using TruckFlow.Extensions.Produto;
using TruckFlowApi.Infra.Database;


namespace TruckFlow
{
    public class Program
    {
        public static void Main(string[] args)
        { 
            var builder = WebApplication.CreateBuilder(args);
            
            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddTransient<AuthService>();
            builder.Services.AddCorsDependency();
            builder.Services.AddProduto();
            builder.Services.AddLocalDescarga();
            builder.Services.AddFornecedor();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            
            builder.Services.AddSwaggerGen();
            builder.Services.AddDbContext<AppDbContext>(optionsBuilder =>
            {
                optionsBuilder.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            builder.AddAuthenticationJwt();
            builder.Services.AddAuthentication();
            builder.Services.AddAuthorization();

            builder.Services.AddIdentity<Usuario, IdentityRole<Guid>>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();
                

            var app = builder.Build();
            app.UseCors("AllowFrontend");
            
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
