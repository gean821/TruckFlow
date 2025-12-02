using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace TruckFlow.Filters
{
    public class ActionLoggingFilter : IActionFilter
    {
        private readonly ILogger<ActionLoggingFilter> _logger;
        private readonly Stopwatch _sw;

        public ActionLoggingFilter(ILogger<ActionLoggingFilter> logger)
        {
            _logger = logger;
            _sw = new Stopwatch();
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            _sw.Restart();

            _logger.LogInformation("→ {Action} started",
                context.ActionDescriptor.DisplayName);
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            _sw.Stop();

            _logger.LogInformation("← {Action} finished in {Elapsed}ms",
                context.ActionDescriptor.DisplayName,
                _sw.ElapsedMilliseconds);
        }
    }
}
