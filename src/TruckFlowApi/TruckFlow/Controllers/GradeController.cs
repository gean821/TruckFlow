using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Grade;


namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/[Controller]")]
    public class GradeController(IGradeService service) : ControllerBase
    {
        private readonly IGradeService _service = service;

        [HttpPost]
        public async Task<IActionResult> CreateGrade(
            [FromBody] GradeCreateDto grade,
            CancellationToken ct = default)
        {
            var gradeCriada = await _service.CreateGrade(grade, ct);
            return CreatedAtAction(
                nameof(GetById),
                new { id = gradeCriada.Id },
                gradeCriada
            );
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(
            [FromRoute] Guid id,
            CancellationToken ct = default)
        {
            var grade = await _service.GetById(id, ct);
            return Ok(grade);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] GradeListQueryDto query,
            CancellationToken ct = default)
        {
            var lista = await _service.GetPagedGrades(query, ct);
            return Ok(lista);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Update(
            [FromRoute] Guid id,
            [FromBody] GradeUpdateDto dto,
            CancellationToken ct)
        {
            var gradeEncontrada = await _service.UpdateGrade(id, dto, ct);
            return Ok(gradeEncontrada);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(
            [FromRoute] Guid id,
            CancellationToken ct)
        {
            await _service.DeleteGrade(id, ct);
            return NoContent();
        }
    }
}
