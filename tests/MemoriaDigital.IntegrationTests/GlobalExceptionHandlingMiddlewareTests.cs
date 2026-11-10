using MemoriaDigital.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace MemoriaDigital.IntegrationTests;

public sealed class GlobalExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task UnhandledException_ReturnsGenericProblemDetailsWithoutInternalDetails()
    {
        var context = new DefaultHttpContext();
        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/test";

        var middleware = new GlobalExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("secret database details"),
            NullLogger<GlobalExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        responseBody.Position = 0;
        var body = await new StreamReader(responseBody).ReadToEndAsync();

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.Contains("An unexpected error occurred.", body);
        Assert.DoesNotContain("secret database details", body);
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
    }
}
