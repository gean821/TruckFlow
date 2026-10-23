using NBomber.CSharp;
using NBomber.Http.CSharp;
using System.Text;
using System.Text.Json;

namespace TruckFlow.LoadTests.Scenarios;

public static class LoginScenario
{
    public static async Task RunAsync(HttpClient http)
    {
        var scenario = Scenario.Create("login_admin", async context =>
        {
            var body = JsonSerializer.Serialize(new
            {
                Login = "adminteste1@gmail.com",
                Password = "Senha@123456"
            });

            var request = Http.CreateRequest("POST", "/v1/AuthAdmin/login")
                .WithHeader("Content-Type", "application/json")
                .WithBody(new StringContent(body, Encoding.UTF8, "application/json"));

            var response = await Http.Send(http, request);
            return response;
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(
            Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            Simulation.Inject(rate: 30, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFileName("login_report")
            .WithReportFolder("reports")
            .Run();

        await Task.CompletedTask;
    }
}
