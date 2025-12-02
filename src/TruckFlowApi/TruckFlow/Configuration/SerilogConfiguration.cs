using Microsoft.Extensions.Configuration;
using Serilog;

namespace TruckFlow.Configuration
{
    public static class SerilogConfiguration
    {
        public static void AddSerilogLogging(this WebApplicationBuilder builder)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .CreateLogger();

            builder.Host.UseSerilog(Log.Logger);
        }
    }
}
