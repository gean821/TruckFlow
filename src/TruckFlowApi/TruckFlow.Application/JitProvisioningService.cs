using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Auth;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    /// <summary>
    /// Provisionamento just-in-time de usuários staff (Admin/Gerente/Monitor/Portaria) que
    /// autenticam via Entra ID (SSO federado da Aurora ou de qualquer outro cliente que venha
    /// a usar Entra ID). Nunca toca em Motorista — esse papel nunca vem do Entra ID.
    ///
    /// Nunca escreve no Entra ID/AD do cliente — só lê o token para decidir acesso dentro do
    /// próprio TruckFlow. Ver Docs/entra-id-integracao-backlog.md (ENTRA-04, ENTRA-05, ENTRA-06).
    /// </summary>
    public class JitProvisioningService : IJitProvisioningService
    {
        private readonly AppDbContext _db;
        private readonly UserManager<Usuario> _userManager;
        private readonly IEntraGroupRoleMappingRepositorio _mappingRepo;
        private readonly ILogger<JitProvisioningService> _logger;

        public JitProvisioningService(
            AppDbContext db,
            UserManager<Usuario> userManager,
            IEntraGroupRoleMappingRepositorio mappingRepo,
            ILogger<JitProvisioningService> logger)
        {
            _db = db;
            _userManager = userManager;
            _mappingRepo = mappingRepo;
            _logger = logger;
        }

        public async Task<JitProvisioningResult> SincronizarAsync(
            string entraObjectId,
            string? email,
            string? nomeExibicao,
            IReadOnlyCollection<string> entraGroupIds,
            CancellationToken token = default)
        {
            var usuarioExistente = await _userManager.FindByLoginAsync("EntraId", entraObjectId);

            var mapeamentos = await _mappingRepo.GetAtivosByGroupIdsAsync(entraGroupIds, token);

            if (mapeamentos.Count == 0)
            {
                if (usuarioExistente is not null)
                {
                    await DesativarAsync(usuarioExistente, token);
                }

                _logger.LogWarning(
                    "Login Entra ID sem grupo autorizado. EntraObjectId={EntraObjectId}",
                    entraObjectId);

                return JitProvisioningResult.Fail(JitProvisioningFailure.SemGrupoMapeado);
            }

            var empresaIdsDistintos = mapeamentos.Select(m => m.EmpresaId).Distinct().ToList();
            if (empresaIdsDistintos.Count > 1)
            {
                _logger.LogError(
                    "Mapeamento ambíguo: grupos do token do EntraObjectId={EntraObjectId} resolvem para {Count} empresas distintas. " +
                    "Isso é um erro de configuração em EntraGroupRoleMapping, não um problema do usuário.",
                    entraObjectId, empresaIdsDistintos.Count);

                return JitProvisioningResult.Fail(JitProvisioningFailure.GruposAmbiguos);
            }

            var empresaId = empresaIdsDistintos[0];
            var empresa = mapeamentos[0].Empresa
                ?? await _db.Empresa.FirstAsync(e => e.Id == empresaId, token);

            // Defesa em profundidade (ENTRA-06): mesmo com grupo mapeado, a Empresa
            // precisa permitir explicitamente login via Entra ID.
            if (!EmpresaAuthMethods.Permite(empresa.Configuracoes, EmpresaAuthMethods.EntraId))
            {
                _logger.LogWarning(
                    "Login Entra ID rejeitado: Empresa {EmpresaId} não tem EntraId habilitado em Configuracoes.authMethods.",
                    empresaId);

                return JitProvisioningResult.Fail(JitProvisioningFailure.EmpresaNaoPermiteEntraId);
            }

            var roleNames = mapeamentos
                .Select(m => m.RoleName)
                .Where(r => Roles.AdminLevel.Contains(r))
                .Distinct()
                .ToList();

            if (roleNames.Count != mapeamentos.Select(m => m.RoleName).Distinct().Count())
            {
                _logger.LogWarning(
                    "EntraGroupRoleMapping para Empresa {EmpresaId} contém RoleName fora de Roles.AdminLevel — ignorado.",
                    empresaId);
            }

            var usuario = usuarioExistente ?? await CriarUsuarioAsync(entraObjectId, email);

            usuario.EmpresaId = empresaId;
            usuario.UltimoSyncEntraEm = DateTime.UtcNow;

            // Estava desativado (ex.: removido do grupo antes) e voltou a ter grupo válido.
            // Usa o mesmo campo (DeletedAt) que o resto do app já usa pra "ativo/inativo" —
            // é o que a tela de gestão de usuários e o login local realmente checam.
            usuario.DeletedAt = null;

            await SincronizarRolesAsync(usuario, roleNames);
            await GarantirAdministradorAsync(usuario, nomeExibicao, email, token);

            _db.Users.Update(usuario);
            await _db.SaveChangesAsync(token);

            var usuarioFinal = await _db.Users
                .Include(u => u.Administrador)
                .FirstAsync(u => u.Id == usuario.Id, token);

            _logger.LogInformation(
                "JIT Entra ID concluído. UsuarioId={UsuarioId} EmpresaId={EmpresaId} Roles={Roles}",
                usuarioFinal.Id, empresaId, string.Join(",", roleNames));

            return JitProvisioningResult.Ok(usuarioFinal);
        }

        private async Task<Usuario> CriarUsuarioAsync(string entraObjectId, string? email)
        {
            var usuario = new Usuario
            {
                Id = Guid.NewGuid(),
                UserName = email ?? entraObjectId,
                Email = email,
                CreatedAt = DateTime.UtcNow,
                Origem = OrigemUsuario.EntraId,
            };

            var createResult = await _userManager.CreateAsync(usuario);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Falha ao criar usuário via Entra ID: {string.Join(" | ", createResult.Errors.Select(e => e.Description))}");
            }

            var loginResult = await _userManager.AddLoginAsync(
                usuario,
                new UserLoginInfo("EntraId", entraObjectId, "Microsoft Entra ID"));

            if (!loginResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Falha ao vincular login Entra ID: {string.Join(" | ", loginResult.Errors.Select(e => e.Description))}");
            }

            return usuario;
        }

        private async Task GarantirAdministradorAsync(
            Usuario usuario,
            string? nomeExibicao,
            string? email,
            CancellationToken token)
        {
            var jaTemAdministrador = await _db.Administrador.AnyAsync(a => a.UsuarioId == usuario.Id, token);
            if (jaTemAdministrador)
                return;

            var administrador = new Administrador
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuario.Id,
                Usuario = usuario,
                Nome = nomeExibicao ?? email ?? usuario.UserName ?? "Usuário Entra ID",
                UserName = usuario.UserName!,
                CreatedAt = DateTime.UtcNow,
            };

            _db.Administrador.Add(administrador);
        }

        private async Task SincronizarRolesAsync(Usuario usuario, IReadOnlyCollection<string> roleNamesDesejados)
        {
            var rolesAtuais = await _userManager.GetRolesAsync(usuario);

            var rolesStaffAtuais = rolesAtuais.Where(r => Roles.AdminLevel.Contains(r)).ToList();

            var paraAdicionar = roleNamesDesejados.Except(rolesStaffAtuais).ToList();
            var paraRemover = rolesStaffAtuais.Except(roleNamesDesejados).ToList();

            if (paraAdicionar.Count > 0)
                await _userManager.AddToRolesAsync(usuario, paraAdicionar);

            if (paraRemover.Count > 0)
                await _userManager.RemoveFromRolesAsync(usuario, paraRemover);
        }

        private async Task DesativarAsync(Usuario usuario, CancellationToken token)
        {
            // DeletedAt é o mesmo campo que SetAdminStatusAsync usa pra "inativar" um usuário
            // local — reaproveitado aqui de propósito, pra tela de gestão de usuários (e o
            // gate de login local) verem o estado real sem precisar saber que a origem é Entra ID.
            usuario.DeletedAt ??= DateTime.UtcNow;
            await SincronizarRolesAsync(usuario, []);

            usuario.UltimoSyncEntraEm = DateTime.UtcNow;
            _db.Users.Update(usuario);
            await _db.SaveChangesAsync(token);

            _logger.LogInformation(
                "Usuário desativado por ausência de grupo Entra ID mapeado. UsuarioId={UsuarioId}",
                usuario.Id);
        }
    }
}
