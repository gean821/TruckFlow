using NBomber.CSharp;
using NBomber.Http.CSharp;
using System.Text;
using System.Text.Json;

namespace TruckFlow.LoadTests.Scenarios;

public static class AgendamentoScenario
{
    public static async Task RunAsync(HttpClient http)
    {
        var token = await ObterTokenAsync(http);

        if (token is null)
        {
            Console.WriteLine("[AVISO] Não foi possível obter token. Configure email/senha em AgendamentoScenario.cs.");
            return;
        }

        var listarScenario = Scenario.Create("listar_agendamentos", async context =>
        {
            var request = Http.CreateRequest("GET", "/v1/AgendamentoAdmin")
                .WithHeader("Authorization", $"Bearer {token}");

            return await Http.Send(http, request);
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(
            Simulation.Inject(rate: 20, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
        );

        var reservarScenario = Scenario.Create("dashboard_concorrente", async context =>
        {
            var request = Http.CreateRequest("GET", "/v1/Dashboard/summary")
                .WithHeader("Authorization", $"Bearer {token}");

            return await Http.Send(http, request);
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(3))
        .WithLoadSimulations(
            Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)),
            Simulation.Inject(rate: 30, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)),
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20))
        );

        NBomberRunner
            .RegisterScenarios(listarScenario, reservarScenario)
            .WithReportFileName("agendamento_report")
            .WithReportFolder("reports")
            .Run();

        await Task.CompletedTask;
    }

    private static async Task<string?> ObterTokenAsync(HttpClient http)
    {
        try
        {
            var body = JsonSerializer.Serialize(new
            {
                Login = "adminteste1@gmail.com",
                Password = "Senha@123456"
            });

            var response = await http.PostAsync("/v1/AuthAdmin/login",
                new StringContent(body, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement.TryGetProperty("token", out var t) ? t.GetString()
                 : doc.RootElement.TryGetProperty("Token", out var t2) ? t2.GetString() : null;
        }
        catch
        {
            return null;
        }
    }
}
