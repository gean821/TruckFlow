using TruckFlow.Application.Exceptions;

namespace TruckFlow.Middlewares
{
    public class ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment env)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;
        private readonly IWebHostEnvironment _env = env;

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (HttpResponseException ex)
            {
                _logger.LogWarning(ex, "Erro de negócio");

                var result = new
                {
                    success = false,
                    code = ex.Code,
                    message = ex.Message
                };

                context.Response.StatusCode = ex switch
                {
                    NotFoundException => StatusCodes.Status404NotFound,
                    BusinessException => StatusCodes.Status400BadRequest,
                    _ => StatusCodes.Status500InternalServerError
                };

                await context.Response.WriteAsJsonAsync(result);
            }

            catch (FluentValidation.ValidationException ex)
            {
                _logger.LogWarning(ex, "Erro de validação");

                var result = new
                {
                    success = false,
                    code = "VALIDATION_ERROR",
                    errors = ex.Errors.Select(e => e.ErrorMessage).ToList()
                };

                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(result);
            }

            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Acesso negado: {Message}", ex.Message);

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = ex.Message
                });
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado");

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                object body = _env.IsDevelopment()
                    ? new
                    {
                        success = false,
                        message = ex.Message,
                        stack = ex.StackTrace
                    }
                    : new
                    {
                        success = false,
                        message = "Erro interno. Tente novamente ou contate o suporte."
                    };

                await context.Response.WriteAsJsonAsync(body);
            }
        }
    }
}