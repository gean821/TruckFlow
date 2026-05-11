using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Dto.User.Administrador;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/usuarios")]
    [Authorize(Roles = RoleGroups.CanManageUsers)]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioService _service;
        private readonly ICurrentUserService _currentUser;

        public UsuarioController(
            IUsuarioService service,
            ICurrentUserService currentUser
            )
        {
            _service = service;
            _currentUser = currentUser;
        }

        [HttpGet]
        public async Task<IActionResult> GetPaged(
            [FromQuery] UsuarioListQueryDto query,
            CancellationToken token)
        {
            var empresaId = _currentUser.EmpresaId
                ?? throw new BusinessException("Usuário não vinculado a empresa.");

            var result = await _service.GetPagedAdminsAsync(query, empresaId, token);
            return Ok(result);
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles(CancellationToken token)
        {
            var roles = await _service.GetRolesAsync(token);
            return Ok(roles);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(
            [FromRoute] Guid id,
            CancellationToken token)
        {
            var usuario = await _service.GetAdminByIdAsync(id, token);
            return Ok(usuario);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] UserAdminRegisterDto dto,
            CancellationToken token)
        {
            var empresaId = _currentUser.EmpresaId
                ?? throw new BusinessException("Usuário não vinculado a empresa.");

            dto.EmpresaId = empresaId;
            var usuario = await _service.RegisterAdminAsync(dto, token);

            return CreatedAtAction(
                nameof(GetById),
                new { id = usuario.Id },
                usuario);
        }

        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> Update(
            [FromRoute] Guid id,
            [FromBody] UserAdminEditDto dto,
            CancellationToken token)
        {
            var usuario = await _service.UpdateAdminAsync(id, dto, token);
            return Ok(usuario);
        }

        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> SetStatus(
            [FromRoute] Guid id,
            [FromBody] AlterarStatusUsuarioDto dto,
            CancellationToken token)
        {
            var usuario = await _service.SetAdminStatusAsync(id, dto.Ativo, token);
            return Ok(usuario);
        }
    }
}
