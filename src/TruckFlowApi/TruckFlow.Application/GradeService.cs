using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Factories;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Fornecedor;
using TruckFlow.Domain.Dto.Grade;
using TruckFlow.Domain.Dto.Produto;
using TruckFlow.Domain.Dto.Shared;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class GradeService : IGradeService
    {
        private readonly IGradeRepositorio _repo;
        private readonly IValidator<GradeCreateDto> _createValidator;
        private readonly IValidator<GradeUpdateDto> _updateValidator;
        private readonly IProdutoRepositorio _produtoRepo;
        private readonly IFornecedorRepositorio _fornecedorRepo;
        private readonly IUnidadeEntregaRepositorio _unidadeRepo;
        private readonly IAgendamentoRepositorio _agendamentoRepo;
        private readonly ILocalDescargaRepositorio _localDescargaRepo;
        private readonly CurrentUserGuard _currentUser;
        private readonly ILogger<GradeService> _logger;

        public GradeService(
            IGradeRepositorio repo,
            IValidator<GradeCreateDto> createValidator,
            IValidator<GradeUpdateDto> updateValidator,
            IProdutoRepositorio produtoRepo,
            IFornecedorRepositorio fornecedorRepo,
            IUnidadeEntregaRepositorio unidadeEntregaRepo,
            IAgendamentoRepositorio agendamentoRepositorio,
            ILocalDescargaRepositorio descargaRepo,
            CurrentUserGuard guard,
            ILogger<GradeService> logger
            )
        {
            _repo = repo;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _produtoRepo = produtoRepo;
            _fornecedorRepo = fornecedorRepo;
            _unidadeRepo = unidadeEntregaRepo;
            _agendamentoRepo = agendamentoRepositorio;
            _localDescargaRepo = descargaRepo;
            _currentUser = guard;
            _logger = logger;
        }

        public async Task<PagedResponse<GradeResponse>> GetPagedGrades(
            GradeListQueryDto query,
            CancellationToken token = default)
        {
            var result = await _repo.GetPagedAsync(query, token);

            return new PagedResponse<GradeResponse>
            {
                Items = result.Items.Select(MapToResponse).ToList(),
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalCount = result.TotalCount,
                TotalPages = result.TotalPages
            };
        }
        public async Task<GradeResponse> CreateGrade(
            GradeCreateDto dto,
            CancellationToken token = default)
        {
            var validation = await _createValidator.ValidateAsync(dto, token);

            if (!validation.IsValid)
            {
                _logger.LogWarning(
                    "Validação falhou ao criar grade: {Errors}",
                    string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
                throw new ValidationException(validation.Errors);
            }

            var empresaId = _currentUser.GetEmpresaId();

            var produto = await _produtoRepo.GetById(dto.ProdutoId, token)
                ?? throw new NotFoundException("Produto não encontrado");

            Fornecedor? fornecedor = null;
            if (dto.FornecedorId.HasValue)
            {
                fornecedor = await _fornecedorRepo.GetById(dto.FornecedorId.Value, token)
                    ?? throw new NotFoundException("Fornecedor não encontrado");
            }

            var descarga = await _localDescargaRepo.GetById(dto.LocalDescargaId, token)
                ?? throw new NotFoundException("Local de descarga não encontrado");

            var grade = GradeFactory.Create(
                dto,
                empresaId,
                produto,
                fornecedor,
                descarga,
                descarga.UnidadeEntrega
            );

            var slots = grade.GerarSlots();

            if (slots.Count > 0)
            {
                var minInicio = slots.Min(s => s.DataInicio);
                var maxFim = slots.Max(s => s.DataFim);

                var candidatos = await _agendamentoRepo.GetConflitosAsync(
                    descarga.Id,
                    minInicio,
                    maxFim,
                    excludeAgendamentoId: null,
                    token);

                foreach (var slot in slots)
                {
                    var conflito = candidatos.FirstOrDefault(c =>
                        c.DataInicio < slot.DataFim && slot.DataInicio < c.DataFim);

                    if (conflito is not null)
                    {
                        var inicioBrt = TimeZoneInfo.ConvertTimeFromUtc(conflito.DataInicio, Grade.OperationalTimeZone);
                        var fimBrt = TimeZoneInfo.ConvertTimeFromUtc(conflito.DataFim, Grade.OperationalTimeZone);
                        var docaNome = conflito.LocalDescarga?.Nome ?? descarga.Nome;
                        var produtoNome = conflito.Produto?.Nome ?? "carga geral";

                        throw new BusinessException(
                            $"Conflito ao criar grade: a doca '{docaNome}' já tem um agendamento de '{produtoNome}' " +
                            $"entre {inicioBrt:dd/MM/yyyy HH:mm} e {fimBrt:dd/MM/yyyy HH:mm}.");
                    }
                }
            }

            try
            {
                await _repo.CreateGrade(grade, token);

                if (slots.Count > 0)
                    await _agendamentoRepo.AddRangeAsync(slots, token);

                await _repo.SaveChangesAsync(token);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
                when (ex.InnerException is PostgresException pg && pg.SqlState == "23P01")
            {
                throw new BusinessException(
                    "Conflito detectado ao criar a grade: algum dos slots gerados sobrepõe " +
                    "um agendamento existente nesta doca. Pode ter sido criado em paralelo. " +
                    "Atualize e tente novamente.");
            }

            _logger.LogInformation(
                "Grade criada: {GradeId} (empresa {EmpresaId}, slots {Slots})",
                grade.Id, empresaId, slots.Count);

            return MapToResponse(grade);
        }
        public async Task<List<GradeResponse>> GetAll(CancellationToken cancellationToken = default)
        {
            var listarGrades = await _repo.GetAll(cancellationToken);
            return listarGrades.Select(MapToResponse).ToList();
        }
        public async Task<GradeResponse> GetById(
            Guid id,
            CancellationToken cancellationToken = default
            )
        {
            var gradeEncontrada = await _repo.GetById(id, cancellationToken) ??
                throw new NotFoundException("Grade não encontrado");

            return MapToResponse(gradeEncontrada);
        }

        public async Task<GradeResponse> UpdateGrade(
            Guid id,
            GradeUpdateDto dto,
            CancellationToken token = default
            )
        {
            var validation = await _updateValidator.ValidateAsync(dto, token);

            if (!validation.IsValid)
            {
                _logger.LogWarning(
                    "Validação falhou ao atualizar grade {GradeId}: {Errors}",
                    id, string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
                throw new ValidationException(validation.Errors);
            }

            var grade = await _repo.GetById(id, token)
                ?? throw new NotFoundException("Grade não encontrada");

            var conflitos = await _agendamentoRepo.ExisteAgendamentoBloqueantePorGrade(id, token);

            if (conflitos.Count > 0)
            {
                // DataInicio avançada → agendamentos antes da nova data ficariam fora da vigência
                if (dto.DataInicio.HasValue && dto.DataInicio.Value > grade.DataInicio)
                {
                    var count = conflitos.Count(a =>
                        DateOnly.FromDateTime(
                            TimeZoneInfo.ConvertTimeFromUtc(a.DataInicio, Grade.OperationalTimeZone))
                        < dto.DataInicio.Value);

                    if (count > 0)
                        throw new BusinessException(
                            $"Não é possível avançar a data de início: {count} agendamento(s) ativo(s) ficariam fora da vigência.");
                }

                // DataFim recuada → agendamentos após a nova data ficariam fora da vigência
                if (dto.DataFim.HasValue && dto.DataFim.Value < grade.DataFim)
                {
                    var count = conflitos.Count(a =>
                        DateOnly.FromDateTime(
                            TimeZoneInfo.ConvertTimeFromUtc(a.DataInicio, Grade.OperationalTimeZone))
                        > dto.DataFim.Value);

                    if (count > 0)
                        throw new BusinessException(
                            $"Não é possível recuar a data de fim: {count} agendamento(s) ativo(s) ficariam fora da vigência.");
                }

                // Dias removidos → agendamentos nesses dias ficariam sem grade
                if(!string.IsNullOrEmpty(dto.DiasSemana))
                {
                    var novosDias = dto.DiasSemana
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => (DayOfWeek)int.Parse(x.Trim()))
                        .ToHashSet();

                    var diasRemovidos = grade.DiasSemanaEnum.Except(novosDias).ToList();

                    if (diasRemovidos.Count > 0)
                    {
                        var count = conflitos.Count(a =>
                            diasRemovidos.Contains(
                                TimeZoneInfo.ConvertTimeFromUtc(a.DataInicio, Grade.OperationalTimeZone).DayOfWeek));

                        if (count > 0)
                            throw new BusinessException(
                                $"Não é possível remover dia(s) da semana: {count} agendamento(s) ativo(s) estariam em dias removidos.");
                    }
                }

                // HoraInicial avançada → agendamentos que começam antes do novo horário
                if (dto.HoraInicial.HasValue && dto.HoraInicial.Value > grade.HoraInicial)
                {
                    var count = conflitos.Count(a =>
                    {
                        var localInicio = TimeZoneInfo.ConvertTimeFromUtc(a.DataInicio, Grade.OperationalTimeZone);
                        return TimeOnly.FromDateTime(localInicio) < dto.HoraInicial.Value;
                    });

                    if (count > 0)
                        throw new BusinessException(
                            $"Não é possível avançar o horário de abertura: {count} agendamento(s) ativo(s) começam antes das {dto.HoraInicial.Value:HH\\:mm}.");
                }

                // HoraFinal recuada → agendamentos que terminam após o novo fechamento
                if (dto.HoraFinal.HasValue && dto.HoraFinal.Value < grade.HoraFinal)
                {
                    var count = conflitos.Count(a =>
                    {
                        var localFim = TimeZoneInfo.ConvertTimeFromUtc(a.DataFim, Grade.OperationalTimeZone);
                        return TimeOnly.FromDateTime(localFim) > dto.HoraFinal.Value;
                    });

                    if (count > 0)
                        throw new BusinessException(
                            $"Não é possível recuar o horário de fechamento: {count} agendamento(s) ativo(s) terminam após as {dto.HoraFinal.Value:HH\\:mm}.");
                }

                // Intervalo alterado → reestrutura todos os slots, bloqueia sempre
                if (dto.IntervaloMinutos.HasValue && dto.IntervaloMinutos.Value != grade.IntervaloMinutos)
                {
                    throw new BusinessException(
                        $"Não é possível alterar o intervalo: existem {conflitos.Count} agendamento(s) ativo(s) nesta grade.");
                }

                // LocalDescarga alterada → agendamentos ficariam vinculados à doca errada
                if (dto.LocalDescargaId.HasValue && dto.LocalDescargaId.Value != grade.LocalDescargaId)
                {
                    throw new BusinessException(
                        $"Não é possível alterar a doca: existem {conflitos.Count} agendamento(s) ativo(s) nesta grade.");
                }
            }

            await ApplyPatch(grade, dto, token);

            // Apaga os slots disponíveis e regenera do zero com a config atualizada
            await _agendamentoRepo.DeleteDisponiveisPorGrade(id, token);

            var novosSlots = grade.GerarSlots();

            if (novosSlots.Count > 0 && grade.LocalDescargaId.HasValue)
            {
                var minInicio = novosSlots.Min(s => s.DataInicio);
                var maxFim = novosSlots.Max(s => s.DataFim);

                var conflitosNovos = await _agendamentoRepo.GetConflitosAsync(
                    grade.LocalDescargaId.Value,
                    minInicio,
                    maxFim,
                    excludeAgendamentoId: null,
                    token);

                var slotsValidos = novosSlots
                    .Where(slot => !conflitosNovos.Any(c => c.DataInicio < slot.DataFim && slot.DataInicio < c.DataFim))
                    .ToList();

                if (slotsValidos.Count > 0)
                    await _agendamentoRepo.AddRangeAsync(slotsValidos, token);
            }

            await _repo.Update(grade, token);

            _logger.LogInformation(
                "Grade atualizada: {GradeId} (empresa {EmpresaId})",
                id, grade.EmpresaId);

            return MapToResponse(grade);
        }
        public async Task DeleteGrade(
            Guid id, 
            CancellationToken cancellationToken = default
            )
        {
            var gradeEncontrada = await _repo.GetById(id, cancellationToken)
                ?? throw new NotFoundException("Grade não encontrado");

            var agendamentosAtivos = await _agendamentoRepo.ExisteAgendamentoBloqueantePorGrade(id, cancellationToken);

            if (agendamentosAtivos.Any())
            {
                throw new BusinessException(
                    "Não é possível remover a grade pois existem agendamentos pendentes, agendados ou em andamento.");
            }

            // Remove todos os slots da grade (disponíveis, cancelados, expirados, finalizados)
            await _agendamentoRepo.DeleteTodosPorGrade(id, cancellationToken);

            await _repo.Delete(gradeEncontrada, cancellationToken);

            _logger.LogInformation("Grade excluída: {GradeId} (empresa {EmpresaId})", id, gradeEncontrada.EmpresaId);
        }

        private async Task ApplyPatch(
            Grade grade,
            GradeUpdateDto dto,
            CancellationToken token = default)
        {
            if (dto.ProdutoId is not null)
            {
                var produto = await _produtoRepo.GetById(dto.ProdutoId.Value, token)
                    ?? throw new NotFoundException("Produto não encontrado");

                grade.Produto = produto;
                grade.ProdutoId = produto.Id;
            }

            if (dto.FornecedorId is not null)
            {
                var fornecedor = await _fornecedorRepo.GetById(dto.FornecedorId.Value, token)
                    ?? throw new NotFoundException("Fornecedor não encontrado");

                grade.Fornecedor = fornecedor;
                grade.FornecedorId = fornecedor.Id;
            }

            if (dto.LocalDescargaId is not null)
            {
                var descarga = await _localDescargaRepo.GetById(dto.LocalDescargaId.Value, token)
                    ?? throw new NotFoundException("Local não encontrado");

                grade.LocalDescarga = descarga;
                grade.LocalDescargaId = descarga.Id;
            }

            if (dto.DataInicio is not null)
                grade.DataInicio = dto.DataInicio.Value;

            if (dto.DataFim is not null)
                grade.DataFim = dto.DataFim.Value;

            if (dto.HoraInicial is not null)
                grade.HoraInicial = dto.HoraInicial.Value;

            if (dto.HoraFinal is not null)
                grade.HoraFinal = dto.HoraFinal.Value;

            if (dto.IntervaloMinutos is not null)
                grade.IntervaloMinutos = dto.IntervaloMinutos.Value;

            if (!string.IsNullOrEmpty(dto.DiasSemana))
                grade.DiasSemana = dto.DiasSemana;

            grade.UpdatedAt = DateTime.UtcNow;
        }
        private static GradeResponse MapToResponse(Grade g) =>
            new GradeResponse
            {
                Id = g.Id,
                Produto = g.Produto.Nome,
                Fornecedor = g.Fornecedor?.Nome ?? "-",
                ProdutoId = g.ProdutoId,
                DataInicio = g.DataInicio,
                DataFim = g.DataFim,
                HoraInicial = g.HoraInicial,
                HoraFinal = g.HoraFinal,
                IntervaloMinutos = g.IntervaloMinutos,
                DiasSemana = g.DiasSemana,
                UnidadeEntrega = g.UnidadeEntrega != null ? g.UnidadeEntrega.Nome.ToString() : string.Empty,
                LocalDescarga = g.LocalDescarga != null ? g.LocalDescarga.Nome.ToString() : string.Empty,
                CreatedAt = g.CreatedAt,
                UpdatedAt = g.UpdatedAt
            };
    }
}
