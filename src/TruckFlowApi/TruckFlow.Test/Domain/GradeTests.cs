using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Test.Domain;

public class GradeTests
{
    private static Grade CriarGrade(
        DateOnly? inicio = null,
        DateOnly? fim = null,
        TimeOnly? horaInicial = null,
        TimeOnly? horaFinal = null,
        int intervaloMinutos = 60,
        string? diasSemana = null)
    {
        var empresaId = Guid.NewGuid();
        var unidade = new UnidadeEntrega { Nome = "Unidade", Localizacao = "Pátio A", EmpresaId = empresaId };
        var localDescarga = new LocalDescarga
        {
            Nome = "LD Teste",
            UnidadeEntrega = unidade,
            UnidadeEntregaId = unidade.Id,
            EmpresaId = empresaId
        };
        var produto = new Produto
        {
            Nome = "Milho",
            LocalDescarga = localDescarga,
            LocalDescargaId = localDescarga.Id,
            EmpresaId = empresaId
        };
        return new Grade
        {
            Produto = produto,
            ProdutoId = produto.Id,
            DataInicio = inicio ?? DateOnly.FromDateTime(DateTime.Today),
            DataFim = fim ?? DateOnly.FromDateTime(DateTime.Today),
            HoraInicial = horaInicial ?? new TimeOnly(8, 0),
            HoraFinal = horaFinal ?? new TimeOnly(17, 0),
            IntervaloMinutos = intervaloMinutos,
            DiasSemana = diasSemana ?? "0,1,2,3,4,5,6",
            EmpresaId = empresaId
        };
    }

    // --- DiasSemanaEnum ---

    [Fact]
    public void DiasSemanaEnum_StringVazia_RetornaTodosDiasDaSemana()
    {
        var grade = CriarGrade(diasSemana: "");
        var dias = grade.DiasSemanaEnum;
        Assert.Equal(7, dias.Count);
    }

    [Fact]
    public void DiasSemanaEnum_DiasEspecificos_RetornaDiasCorretos()
    {
        var grade = CriarGrade(diasSemana: "1,2,3"); // seg, ter, qua
        var dias = grade.DiasSemanaEnum;
        Assert.Equal(3, dias.Count);
        Assert.Contains(DayOfWeek.Monday, dias);
        Assert.Contains(DayOfWeek.Tuesday, dias);
        Assert.Contains(DayOfWeek.Wednesday, dias);
    }

    // --- DefinirDiasSemana ---

    [Fact]
    public void DefinirDiasSemana_SerializaDiasCorretamente()
    {
        var grade = CriarGrade();
        grade.DefinirDiasSemana(new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday });
        Assert.Equal("1,3,5", grade.DiasSemana);
    }

    [Fact]
    public void DefinirDiasSemana_ListaVazia_SalvaStringVazia()
    {
        var grade = CriarGrade();
        grade.DefinirDiasSemana(Array.Empty<DayOfWeek>());
        Assert.Equal("", grade.DiasSemana);
    }

    // --- GerarSlots ---

    [Fact]
    public void GerarSlots_UmDia_IntervaloUmaHora_GeraSlotsCertos()
    {
        // 08:00 a 17:00 com intervalos de 60min = 9 slots (8,9,10,11,12,13,14,15,16)
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var grade = CriarGrade(
            inicio: hoje,
            fim: hoje,
            horaInicial: new TimeOnly(8, 0),
            horaFinal: new TimeOnly(17, 0),
            intervaloMinutos: 60,
            diasSemana: $"{(int)DateTime.Today.DayOfWeek}");

        var slots = grade.GerarSlots();

        Assert.Equal(9, slots.Count);
    }

    [Fact]
    public void GerarSlots_DoisDias_DobraQuantidadeDeSlots()
    {
        var inicio = DateOnly.FromDateTime(DateTime.Today);
        var fim = inicio.AddDays(1);
        var grade = CriarGrade(
            inicio: inicio,
            fim: fim,
            horaInicial: new TimeOnly(8, 0),
            horaFinal: new TimeOnly(10, 0),
            intervaloMinutos: 60,
            diasSemana: "0,1,2,3,4,5,6"); // todos os dias

        var slots = grade.GerarSlots();

        Assert.Equal(4, slots.Count); // 2 slots × 2 dias
    }

    [Fact]
    public void GerarSlots_DiasFiltrados_NaoGeraSlotsDiasNaoPermitidos()
    {
        // Gera slots apenas para segunda-feira num intervalo de 7 dias
        var inicio = DateOnly.FromDateTime(ObterProximoDia(DayOfWeek.Monday));
        var fim = inicio.AddDays(6); // segunda a domingo

        var grade = CriarGrade(
            inicio: inicio,
            fim: fim,
            horaInicial: new TimeOnly(8, 0),
            horaFinal: new TimeOnly(10, 0),
            intervaloMinutos: 60,
            diasSemana: "1"); // somente segunda

        var slots = grade.GerarSlots();

        Assert.Equal(2, slots.Count); // 2 slots somente na segunda
        Assert.All(slots, s => Assert.Equal(DayOfWeek.Monday,
            TimeZoneInfo.ConvertTimeFromUtc(s.DataInicio, Grade.OperationalTimeZone).DayOfWeek));
    }

    [Fact]
    public void GerarSlots_SlotsEmUTC_DataInicioEhUtc()
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var grade = CriarGrade(
            inicio: hoje,
            fim: hoje,
            horaInicial: new TimeOnly(8, 0),
            horaFinal: new TimeOnly(9, 0),
            intervaloMinutos: 60,
            diasSemana: $"{(int)DateTime.Today.DayOfWeek}");

        var slots = grade.GerarSlots();

        Assert.Single(slots);
        Assert.Equal(DateTimeKind.Utc, slots[0].DataInicio.Kind);
        Assert.Equal(DateTimeKind.Utc, slots[0].DataFim.Kind);
    }

    [Fact]
    public void GerarSlots_TodosSlotsComStatusDisponivel()
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var grade = CriarGrade(
            inicio: hoje,
            fim: hoje,
            horaInicial: new TimeOnly(8, 0),
            horaFinal: new TimeOnly(10, 0),
            intervaloMinutos: 60,
            diasSemana: $"{(int)DateTime.Today.DayOfWeek}");

        var slots = grade.GerarSlots();

        Assert.All(slots, s => Assert.Equal(StatusAgendamento.Disponivel, s.StatusAgendamento));
    }

    [Fact]
    public void GerarSlots_SlotsComDuracaoIgualAoIntervalo()
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var grade = CriarGrade(
            inicio: hoje,
            fim: hoje,
            horaInicial: new TimeOnly(8, 0),
            horaFinal: new TimeOnly(10, 0),
            intervaloMinutos: 30,
            diasSemana: $"{(int)DateTime.Today.DayOfWeek}");

        var slots = grade.GerarSlots();

        Assert.All(slots, s =>
            Assert.Equal(TimeSpan.FromMinutes(30), s.DataFim - s.DataInicio));
    }

    [Fact]
    public void GerarSlots_SemDiaPermitidoNoPeriodo_RetornaListaVazia()
    {
        // Intervalo de uma segunda, mas só permite domingo (0)
        var proxSegunda = DateOnly.FromDateTime(ObterProximoDia(DayOfWeek.Monday));
        var grade = CriarGrade(
            inicio: proxSegunda,
            fim: proxSegunda,
            diasSemana: "0"); // só domingo

        var slots = grade.GerarSlots();

        Assert.Empty(slots);
    }

    private static DateTime ObterProximoDia(DayOfWeek dia)
    {
        var hoje = DateTime.Today;
        var diff = ((int)dia - (int)hoje.DayOfWeek + 7) % 7;
        return hoje.AddDays(diff == 0 ? 7 : diff);
    }
}
