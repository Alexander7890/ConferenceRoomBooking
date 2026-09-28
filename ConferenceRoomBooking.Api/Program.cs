using ConferenceRoomBooking.Api.ErrorHandling;
using ConferenceRoomBooking.Api.OpenApi;
using ConferenceRoomBooking.Application;
using ConferenceRoomBooking.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddOpenApi(OpenApiConfiguration.Configure);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

var app = builder.Build();

await app.Services.InitializeDatabaseAsync(
    applyMigrations: app.Configuration.GetValue<bool>("APPLY_MIGRATIONS"),
    seedInitialData: app.Configuration.GetValue<bool>("Database:SeedOnStartup"),
    cancellationToken: app.Lifetime.ApplicationStopping);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Conference Room Booking API v1");
        options.DocumentTitle = "Conference Room Booking API";
        options.EnableTryItOutByDefault();
    });
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();
