using System.Text.Json;
using ConferenceRoomBooking.Api.ErrorHandling;
using ConferenceRoomBooking.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace ConferenceRoomBooking.Tests.Api;

public sealed class ApiExceptionHandlerTests
{
    [Fact]
    public async Task UnexpectedError_ReturnsProblemWithoutInternalDetails()
    {
        var (status, body) = await HandleAsync(new InvalidOperationException("Private database details"));

        Assert.Equal(StatusCodes.Status500InternalServerError, status);
        Assert.Equal("An unexpected error occurred.", body.GetProperty("title").GetString());
        Assert.Equal("Please try again later.", body.GetProperty("detail").GetString());
        Assert.DoesNotContain("Private database details", body.ToString());
        Assert.False(body.TryGetProperty("stackTrace", out _));
    }

    [Fact]
    public async Task InvalidRequest_ReturnsFieldErrors()
    {
        var (status, body) = await HandleAsync(new RequestValidationException(new()
        {
            ["ServiceIds"] = ["Every selected service must be available for this room."]
        }));

        Assert.Equal(StatusCodes.Status400BadRequest, status);
        Assert.Equal("Every selected service must be available for this room.",
            body.GetProperty("errors").GetProperty("ServiceIds")[0].GetString());
    }

    [Theory]
    [InlineData(false, StatusCodes.Status404NotFound)]
    [InlineData(true, StatusCodes.Status409Conflict)]
    public async Task ExpectedError_ReturnsAppropriateStatusAndMessage(bool conflict, int expectedStatus)
    {
        Exception exception = conflict
            ? new BookingConflictException("The room is already booked for this period.")
            : new EntityNotFoundException("The room was not found.");

        var (status, body) = await HandleAsync(exception);

        Assert.Equal(expectedStatus, status);
        Assert.Equal(exception.Message, body.GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData("application/xml")]
    [InlineData("text/html")]
    public async Task UnsupportedAcceptHeader_StillReturnsSafeValidationProblem(string accept)
    {
        var (status, body) = await HandleAsync(new RequestValidationException(new()
        {
            ["Capacity"] = ["Capacity must be greater than zero."]
        }), accept);

        Assert.Equal(StatusCodes.Status400BadRequest, status);
        Assert.Equal("Capacity must be greater than zero.",
            body.GetProperty("errors").GetProperty("Capacity")[0].GetString());
    }

    private static async Task<(int Status, JsonElement Body)> HandleAsync(
        Exception exception, string accept = "application/json")
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProblemDetails();
        await using var provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Headers.Accept = accept;
        context.Request.Path = "/api/bookings";
        context.Response.Body = new MemoryStream();
        var handler = new ApiExceptionHandler(provider.GetRequiredService<IProblemDetailsService>(),
            NullLogger<ApiExceptionHandler>.Instance);

        Assert.True(await handler.TryHandleAsync(context, exception, CancellationToken.None));
        Assert.StartsWith("application/problem+json", context.Response.ContentType);
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        return (context.Response.StatusCode, document.RootElement.Clone());
    }
}
