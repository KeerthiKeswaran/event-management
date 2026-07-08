using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Event.Business.Exceptions;

namespace Event.API.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var responseCode = (int)HttpStatusCode.InternalServerError;
            var errorType = exception.GetType().Name;
            var message = exception.Message;

            // 1. Capture our custom business-level domain exceptions
            if (exception is BaseBusinessException businessException)
            {
                responseCode = businessException.StatusCode;
            }
            // 2. Capture built-in unauthorized access exceptions
            else if (exception is UnauthorizedAccessException)
            {
                responseCode = (int)HttpStatusCode.Unauthorized;
                errorType = nameof(UnauthorizedException);
            }

            context.Response.StatusCode = responseCode;

            var result = JsonSerializer.Serialize(new
            {
                error = errorType,
                message = message
            });

            return context.Response.WriteAsync(result);
        }
    }
}
