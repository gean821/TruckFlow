using Microsoft.AspNetCore.Identity;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Extensions.Auth
{
    public static class RoleSeeder
    {
        public static async Task SeedRolesAsync(this IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var logger = services.GetRequiredService<ILoggerFactory>()
                .CreateLogger("RoleSeeder");

            foreach (var roleName in Roles.All)
            {
                if (await roleManager.RoleExistsAsync(roleName))
                {
                    continue;
                }

                var result = await roleManager.CreateAsync(new IdentityRole<Guid>
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant(),
                });

                if (result.Succeeded)
                {
                    logger.LogInformation("Role '{Role}' criada no seed.", roleName);
                }
                else
                {
                    var erros = string.Join("; ", result.Errors.Select(e => e.Description));
                    logger.LogError("Falha ao criar role '{Role}': {Erros}", roleName, erros);
                }
            }
        }
    }
}
