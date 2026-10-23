using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;

namespace TruckFlow.Controllers
{
    /// <summary>
    /// Utilitários pra testar localmente fluxos que dependem de infra externa (Entra ID).
    /// Bloqueado fora de Development de propósito — nunca existe em staging/produção.
    /// </summary>
    [ApiController]
    [Route("v1/dev")]
    public class DevToolsController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly IJitProvisioningService _jit;

        public DevToolsController(IWebHostEnvironment env, IJitProvisioningService jit)
        {
            _env = env;
            _jit = jit;
        }

        /// <summary>
        /// Aciona o JitProvisioningService de verdade, sem passar pelo Entra ID/OIDC —
        /// simula exatamente o que o callback faria com as claims recebidas.
        ///
        /// Pré-requisito: já ter uma Empresa com Configuracoes = {"authMethods":["EntraId"]}
        /// e uma linha em EntraGroupRoleMapping apontando um EntraGroupId (pode ser qualquer
        /// string de teste) pra essa Empresa + um Role de Roles.AdminLevel.
        ///
        /// Depois de chamar, olhe a tela de usuários — o usuário criado aparece com o chip
        /// "SSO" e sem os botões de editar/ativar-inativar.
        /// </summary>
        [HttpPost("seed-entra-login")]
        public async Task<IActionResult> SeedEntraLogin(
            [FromBody] SeedEntraLoginRequest request,
            CancellationToken token = default)
        {
            if (!_env.IsDevelopment())
                return NotFound();

            var resultado = await _jit.SincronizarAsync(
                request.EntraObjectId,
                request.Email,
                request.NomeExibicao,
                request.EntraGroupIds,
                token);

            if (!resultado.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    motivo = resultado.FailureReason?.ToString(),
                    dica = resultado.FailureReason?.ToString() switch
                    {
                        "SemGrupoMapeado" => "Nenhum EntraGroupRoleMapping ativo bate com os EntraGroupIds enviados.",
                        "GruposAmbiguos" => "Os grupos enviados resolvem para mais de uma Empresa.",
                        "EmpresaNaoPermiteEntraId" => "A Empresa resolvida não tem \"EntraId\" em Configuracoes.authMethods.",
                        _ => null
                    }
                });
            }

            return Ok(new
            {
                success = true,
                usuarioId = resultado.Usuario!.Id,
                empresaId = resultado.Usuario.EmpresaId,
                userName = resultado.Usuario.UserName,
                origem = resultado.Usuario.Origem.ToString(),
            });
        }
    }

    public class SeedEntraLoginRequest
    {
        public required string EntraObjectId { get; set; }
        public string? Email { get; set; }
        public string? NomeExibicao { get; set; }
        public required List<string> EntraGroupIds { get; set; }
    }
}
