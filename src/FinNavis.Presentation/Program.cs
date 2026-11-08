using System.Text.Json;
using System.Text.Json.Serialization;
using FinNavis.Application.DependencyInjection;
using FinNavis.Infrastructure.DependencyInjection;
using FinNavis.Presentation.Endpoints.ImportRuns.LoadStatement;
using FinNavis.Presentation.Endpoints.ImportRuns.MigrateHistory;
using FinNavis.Presentation.Endpoints.MappingProfiles;
using FinNavis.Presentation.Errors;

var builder = WebApplication.CreateBuilder(args);

// The composition root, and the only place that names a layer's registrations. The connection
// string comes from configuration — never from a literal here.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddErrorEnvelope();

// The contract marks a field required and says when it may be null, so a body that leaves one out
// is refused at binding instead of reaching a use case half-filled. Enums cross the wire by their
// name, the same string the database stores, never by their number.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.RespectNullableAnnotations = true;
    options.SerializerOptions.RespectRequiredConstructorParameters = true;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
});

var app = builder.Build();

// First in the pipeline, so a failure anywhere after it answers the same envelope.
app.UseErrorEnvelope();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// TODO: map feature endpoints (accounts, categories, transactions, transfers).
app.MapMappingProfiles();

RouteGroupBuilder importRuns = app.MapGroup("/api/v1/import-runs").WithTags("Import runs");
importRuns.MapLoadStatement();
importRuns.MapMigrateHistory();

app.Run();

// Exposed so WebApplicationFactory<Program> can boot the API in integration tests.
public partial class Program;
