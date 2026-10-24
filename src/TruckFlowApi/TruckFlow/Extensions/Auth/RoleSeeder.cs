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

            await services.SyncDesenvolvedoresAsync();
        }

        public static async Task SyncDesenvolvedoresAsync(this IServiceProvider services)
        {
            var userManager = services.GetRequiredService<UserManager<Usuario>>();
            var configuration = services.GetRequiredService<IConfiguration>();
            var logger = services.GetRequiredService<ILoggerFactory>()
                .CreateLogger("RoleSeeder");

            var emailsConfigurados = configuration
                .GetSection("Plataforma:DesenvolvedorEmails")
                .Get<string[]>() ?? [];

            var emails = emailsConfigurados
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim().ToUpperInvariant())
                .ToHashSet();

            foreach (var atual in await userManager.GetUsersInRoleAsync(Roles.Desenvolvedor))
            {
                if (atual.NormalizedEmail is not null && emails.Contains(atual.NormalizedEmail))
                {
                    continue;
                }

                await userManager.RemoveFromRoleAsync(atual, Roles.Desenvolvedor);
                logger.LogWarning("Role Desenvolvedor removida do usuário {UsuarioId} (fora de Plataforma:DesenvolvedorEmails).", atual.Id);
            }

            foreach (var email in emails)
            {
                var usuario = await userManager.FindByEmailAsync(email);

                if (usuario is null)
                {
                    logger.LogWarning("Plataforma:DesenvolvedorEmails contém e-mail sem usuário cadastrado: {Email}", email);
                    continue;
                }

                if (await userManager.IsInRoleAsync(usuario, Roles.Desenvolvedor))
                {
                    continue;
                }

                await userManager.AddToRoleAsync(usuario, Roles.Desenvolvedor);
                logger.LogInformation("Role Desenvolvedor atribuída ao usuário {UsuarioId}.", usuario.Id);
            }
        }
    }
}
