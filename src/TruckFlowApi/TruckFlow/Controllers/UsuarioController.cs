using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TruckFlow.Domain.Entities;

[ApiController]


public class UsuarioController : ControllerBase
{
    [HttpPost("dev/criar-usuario-teste")]
    public async Task<IActionResult> CriarUsuarioTeste(
        [FromServices] UserManager<Usuario> userManager)
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        if (await userManager.FindByIdAsync(userId.ToString()) != null)
            return Ok("Usuário já existe");
    
        var user = new Usuario
        {
            Id = userId,
            UserName = "motorista.teste",
            Email = "motorista@teste.com",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, "Teste@123");

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok("Usuário criado");
    }
}

