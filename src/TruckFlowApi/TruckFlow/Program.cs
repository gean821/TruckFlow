using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TruckFlow.Domain.Entities;
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
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            
            builder.Services.AddSwaggerGen();
            builder.Services.AddDbContext<AppDbContext>(optionsBuilder =>
            {
                optionsBuilder.UseSqlServer(builder.Configuration["DefaultConnection"]);
            });

            builder.Services.AddAuthentication();
            builder.Services.AddAuthorization();

            builder.Services
                .AddIdentityApiEndpoints<Usuario>()
                .AddEntityFrameworkStores<AppDbContext>();

            var app = builder.Build();
            app.MapIdentityApi<Usuario>();

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
