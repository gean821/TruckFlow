using TruckFlow.LoadTests.Scenarios;

var baseUrl = args.FirstOrDefault() ?? "http://localhost:5000";

Console.WriteLine($"Executando load tests contra: {baseUrl}");
Console.WriteLine("Selecione o cenário:");
Console.WriteLine("  1 - Login de administrador");
Console.WriteLine("  2 - Listagem de agendamentos");
Console.WriteLine("  3 - Todos os cenários");
Console.Write("Opção: ");

var opcao = Console.ReadLine();

using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };

switch (opcao)
{
    case "1":
        await LoginScenario.RunAsync(http);
        break;
    case "2":
        await AgendamentoScenario.RunAsync(http);
        break;
    default:
        await LoginScenario.RunAsync(http);
        await AgendamentoScenario.RunAsync(http);
        break;
}

Console.WriteLine("Load tests concluídos. Relatórios em: reports/");
