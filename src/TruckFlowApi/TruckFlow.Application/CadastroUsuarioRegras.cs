using Microsoft.AspNetCore.Identity;
using TruckFlow.Application.Exceptions;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Application
{
    public static class CadastroUsuarioRegras
    {
        public static async Task GarantirUsuarioEEmailLivresAsync(
            UserManager<Usuario> userManager,
            string userName,
            string email
        )
        {
            if (await userManager.FindByNameAsync(userName) is not null)
            {
                throw new BusinessException("Este usuário já está em uso. Escolha outro.");
            }

            if (await userManager.FindByEmailAsync(email) is not null)
            {
                throw new BusinessException("Este e-mail já está cadastrado.");
            }
        }

        public static void GarantirSenhaForte(string senha)
        {
            var faltando = new List<string>();

            if (senha.Length < 10)
                faltando.Add("pelo menos 10 caracteres");
            if (!senha.Any(char.IsUpper))
                faltando.Add("uma letra maiúscula");
            if (!senha.Any(char.IsLower))
                faltando.Add("uma letra minúscula");
            if (!senha.Any(char.IsDigit))
                faltando.Add("um número");
            if (senha.All(char.IsLetterOrDigit))
                faltando.Add("um caractere especial (ex.: @, #, !)");

            if (faltando.Count > 0)
            {
                throw new BusinessException($"A senha precisa ter: {string.Join(", ", faltando)}.");
            }
        }
    }
}
