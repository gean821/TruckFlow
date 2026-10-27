using System.Linq.Expressions;
using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Enums;
using TruckFlow.Domain.Events;
using TruckFlow.Domain.Exceptions;
using TruckFlow.Domain.Rules;


namespace TruckFlow.Domain.Entities
{
    public class Agendamento : EntidadeBase, IEmpresaScoped
    {
        public static readonly Expression<Func<Agendamento, bool>> TemReserva =
            a => a.UsuarioId != null || a.PlacaVeiculo != null;

        public Produto? Produto { get; set; }
        public Guid? ProdutoId { get; set; }
        
        public Grade? Grade { get; set; }
        public Guid? GradeId { get; set; }
        public Usuario? Usuario { get; set; }
        public Guid? UsuarioId { get; set; }
        public Fornecedor? Fornecedor { get; set; }
        public Guid? FornecedorId { get; set; }
        public required TipoCarga TipoCarga { get; set; }
        public decimal? VolumeCarga { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public StatusAgendamento StatusAgendamento { get; set; }
        public UnidadeEntrega? UnidadeEntrega { get; set; }
        public Guid? UnidadeEntregaId { get; set; }
        public LocalDescarga? LocalDescarga { get; set; }
        public Guid? LocalDescargaId { get; set; }
        public NotaFiscal? NotaFiscal { get; set; }
        public Guid? NotaFiscalId { get; set; }
        public Guid EmpresaId { get; set; }
        public Empresa? Empresa { get; set; }

        public string? PlacaVeiculo { get; set; }
        public DateTime? LembreteJanelaEnviadoEm { get; set; }
        public TipoVeiculo? TipoVeiculo { get; set; }

        public DateTime? ChegouEm { get; set; }
        public DateTime? ChamadoDocaEm { get; set; }
        public DateTime? InicioDescargaEm { get; set; }
        public DateTime? FimDescargaEm { get; set; }
        public DateTime? SaiuEm { get; set; }

        public void AlterarStatus(StatusAgendamento novoStatus)
        {
            if (!StatusAgendamento.PodeTransitarPara(novoStatus))
            {
                throw new DomainException(
                    $"Transição inválida: {StatusAgendamento} → {novoStatus}");
            }

            StatusAgendamento = novoStatus;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Reservar(
            Guid usuarioId,
            NotaFiscal notaFiscal,
            TipoVeiculo? tipoVeiculo,
            string? placaVeiculo
            )
        {
            if (StatusAgendamento != StatusAgendamento.Disponivel)
            {
                throw new DomainException("A vaga não está disponível para reserva.");
            }

            if (DataFim <= DateTime.UtcNow)
            {
                throw new DomainException("Esta vaga já expirou e não pode mais ser reservada.");
            }

            if (!ProdutoId.HasValue)
            {
                throw new DomainException("Vaga sem produto definido. Contate o administrador.");
            }

            var temProdutoNaNota = notaFiscal.Itens?
                .Any(i => i.ProdutoId == ProdutoId) == true;

            if (!temProdutoNaNota)
            {
                throw new DomainException("A nota fiscal não contém o produto desta vaga.");
            }

            if (FornecedorId.HasValue && FornecedorId != notaFiscal.FornecedorId)
            {
                throw new DomainException("Esta vaga é exclusiva de outro fornecedor.");
            }

            StatusAgendamento = StatusAgendamento.Agendado;
            UsuarioId = usuarioId;
            NotaFiscalId = notaFiscal.Id;
            VolumeCarga = notaFiscal.PesoBruto ?? 0;
            PlacaVeiculo =
         !string.IsNullOrWhiteSpace(notaFiscal.PlacaVeiculo)
             ? notaFiscal.PlacaVeiculo
             : placaVeiculo;

            TipoVeiculo = tipoVeiculo;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new AgendamentoEvent.AgendamentoConfirmadoEvent(
                AgendamentoId: Id,
                EmpresaId: EmpresaId,
                MotoristaUsuarioId: usuarioId,
                DataInicio: DataInicio,
                OcorridoEm: DateTime.UtcNow));
        }

        public void RegistrarChegada()
        {
            AlterarStatus(StatusAgendamento.EmAndamento);
            ChegouEm ??= UpdatedAt;

            if (UsuarioId.HasValue)
            {
                AddDomainEvent(new AgendamentoEvent.MotoristaChegouEvent(
                    AgendamentoId: Id,
                    EmpresaId: EmpresaId,
                    MotoristaUsuarioId: UsuarioId.Value,
                    OcorridoEm: DateTime.UtcNow));
            }
        }

        public void FinalizarOperacao()
        {
            AlterarStatus(StatusAgendamento.Finalizado);
            FimDescargaEm ??= UpdatedAt;
        }

        public void RegistrarSaida()
        {
            if (StatusAgendamento != StatusAgendamento.Finalizado)
            {
                FinalizarOperacao();
            }

            SaiuEm ??= DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Cancelar(string? motivo = null)
        {
            AlterarStatus(StatusAgendamento.Cancelado);

            AddDomainEvent(new AgendamentoCanceladoEvent(
                AgendamentoId: Id,
                EmpresaId: EmpresaId,
                MotoristaUsuarioId: UsuarioId,
                MotivoCancelamento: motivo,
                OcorridoEm: DateTime.UtcNow));
        }

        public void Reagendar(
            DateTime novaDataInicio,
            DateTime novaDataFim)
        {
            var inicioAnterior = DataInicio;
            var fimAnterior = DataFim;

            if (inicioAnterior == novaDataInicio && fimAnterior == novaDataFim)
            {
                return;
            }

            DataInicio = novaDataInicio;
            DataFim = novaDataFim;
            LembreteJanelaEnviadoEm = null;
            UpdatedAt = DateTime.UtcNow;

            var motoristaReservou = UsuarioId.HasValue
                && (StatusAgendamento == StatusAgendamento.Agendado
                    || StatusAgendamento == StatusAgendamento.EmAndamento);

            if (!motoristaReservou)
            {
                return;
            }

            AddDomainEvent(new AgendamentoEvent.AgendamentoReagendadoEvent(
                AgendamentoId: Id,
                EmpresaId: EmpresaId,
                MotoristaUsuarioId: UsuarioId,
                DataInicioAnterior: inicioAnterior,
                DataFimAnterior: fimAnterior,
                DataInicioNova: novaDataInicio,
                DataFimNova: novaDataFim,
                OcorridoEm: DateTime.UtcNow));
        }

        public bool PodeExpirarNaData(DateTime referenciaUtc)
            => DataFim <= referenciaUtc
               && (StatusAgendamento == StatusAgendamento.Disponivel
                   || StatusAgendamento == StatusAgendamento.Pendente
                   || StatusAgendamento == StatusAgendamento.Agendado);

        public void Expirar()
        {
            var motoristaReservou = UsuarioId.HasValue
                && StatusAgendamento == StatusAgendamento.Agendado;

            AlterarStatus(StatusAgendamento.Expirado);

            if (motoristaReservou)
            {
                AddDomainEvent(new AgendamentoEvent.AgendamentoExpiradoEvent(
                    AgendamentoId: Id,
                    EmpresaId: EmpresaId,
                    MotoristaUsuarioId: UsuarioId!.Value,
                    DataInicio: DataInicio,
                    OcorridoEm: DateTime.UtcNow));
            }
        }
    }
}
