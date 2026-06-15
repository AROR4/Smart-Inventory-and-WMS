using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartInventoryManagement.Models.DTOs;
using SmartInventoryManagement.Models.Exceptions;

namespace SmartInventoryManagement.API.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(
                    context,
                    ex);
            }
        }

        private async Task HandleExceptionAsync(
            HttpContext context,
            Exception exception)
        {
            var response =
                new ErrorResponseDto();

            switch (exception)
            {
                case InvalidCredentialsException:
                    context.Response.StatusCode =
                        (int)HttpStatusCode.Unauthorized;

                    response.Message =
                        exception.Message;
                    break;

                case BadRequestException:
                    context.Response.StatusCode =
                        (int)HttpStatusCode.BadRequest;

                    response.Message =
                        exception.Message;
                    break;

                case ConflictException:
                    context.Response.StatusCode =
                        (int)HttpStatusCode.Conflict;

                    response.Message =
                        exception.Message;
                    break;

                case NotFoundException:
                    context.Response.StatusCode =
                        (int)HttpStatusCode.NotFound;

                    response.Message =
                        exception.Message;
                    break;

                case ForbiddenException:
                    _logger.LogWarning(exception.Message);

                    context.Response.StatusCode =
                        (int)HttpStatusCode.Forbidden;

                    response.Message =
                        exception.Message;
                    break;

                case DbUpdateConcurrencyException:
                    _logger.LogWarning(
                        "Concurrency conflict occurred.");

                    context.Response.StatusCode =
                        (int)HttpStatusCode.Conflict;

                    response.Message =
                        "The record was modified by another user. Please refresh and try again.";
                    break;

                case SecurityTokenExpiredException:
                    _logger.LogWarning(
                        "Expired JWT token.");

                    context.Response.StatusCode =
                        (int)HttpStatusCode.Unauthorized;

                    response.Message =
                        "Token has expired.";
                    break;

                case SecurityTokenException:
                    _logger.LogWarning(                        exception,
                        "Invalid JWT token.");

                    context.Response.StatusCode =
                        (int)HttpStatusCode.Unauthorized;

                    response.Message =
                        exception.Message;
                    break;

                case EmailException:
                    _logger.LogWarning(
                        exception,
                        exception.Message);

                    context.Response.StatusCode =
                        (int)HttpStatusCode.OK;

                    response.Message =
                        exception.Message;
                    break;

                default:
                    _logger.LogError(
                        exception,
                        "Unhandled exception occurred while processing request {Path}",
                        context.Request.Path);

                    context.Response.StatusCode =
                        (int)HttpStatusCode.InternalServerError;

                    response.Message =
                        "An unexpected error occurred.";

                    response.Details =
                        exception.Message;
                    break;
            }

            context.Response.ContentType =
                "application/json";

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response));
        }
    }
}