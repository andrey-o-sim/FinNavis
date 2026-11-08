using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FinNavis.Application.Categories;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Infrastructure.IntegrationTests.Errors;
using FinNavis.Infrastructure.IntegrationTests.Fixtures;
using FinNavis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FinNavis.Infrastructure.IntegrationTests.Endpoints.MappingProfiles;

[Collection(DatabaseCollection.Name)]
public sealed class ConfirmMappingProfileEndpointTests : IAsyncLifetime
{
    private const string ConfirmPath = "/api/v1/mapping-profiles";

    private const string InspectPath = "/api/v1/mapping-profiles/inspect";

    private const string ShapeKey = "date|description|category|amount";

    private const string GroceriesSource = "TEST GROCERIES";

    private const string TransferSource = "TEST TRANSFER";

    private static readonly string[] ProfileKeys = ["id", "shape_key", "rules", "created_at"];

    private static readonly string[] RuleKeys = ["source_category", "target", "target_category_id"];

    private readonly PostgresFixture _database;

    private readonly ErrorTestHost _host;

    private readonly HttpClient _client;

    public ConfirmMappingProfileEndpointTests(PostgresFixture database)
    {
        _database = database;
        _host = new ErrorTestHost(database, Environments.Development);
        _client = _host.CreateClient();
    }

    public static TheoryData<string> BodiesTheContractRefuses => new()
    {
        """{"shape_key":"date|amount","rules":[{"source_category":"A","target":1,"target_category_id":null}]}""",
        """{"shape_key":"date|amount","rules":[{"source_category":"A","target":"Nothing","target_category_id":null}]}""",
        """{"shape_key":"date|amount"}""",
        """{"shape_key":"date|amount","rules":null}""",
        """{"rules":[]}""",
    };

    public Task InitializeAsync() => _database.ResetAsync();

    public Task DisposeAsync()
    {
        _client.Dispose();
        _host.Dispose();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task Post_NewShape_Returns201WithTheRulesAndWhereTheProfileLives()
    {
        Guid groceriesId = await CreateCategoryAsync("Groceries");

        HttpResponseMessage response = await ConfirmAsync(
            ShapeKey,
            CategoryRule(GroceriesSource, groceriesId),
            TransferRule(TransferSource));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        JsonElement body = await ReadJsonAsync(response);
        Guid profileId = body.GetProperty("id").GetGuid();
        response.Headers.Location?.OriginalString.Should().Be($"/api/v1/mapping-profiles/{profileId}");
        body.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(ProfileKeys);
        body.GetProperty("shape_key").GetString().Should().Be(ShapeKey);
        body.GetProperty("created_at").GetDateTimeOffset()
            .Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));

        JsonElement[] rules = [.. body.GetProperty("rules").EnumerateArray()];
        rules.Should().HaveCount(2).And.AllSatisfy(
            rule => rule.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(RuleKeys));
        rules[0].GetProperty("source_category").GetString().Should().Be(GroceriesSource);
        rules[0].GetProperty("target").GetString().Should().Be("Category");
        rules[0].GetProperty("target_category_id").GetGuid().Should().Be(groceriesId);
        rules[1].GetProperty("source_category").GetString().Should().Be(TransferSource);
        rules[1].GetProperty("target").GetString().Should().Be("TransferBetweenOwnAccounts");
        rules[1].GetProperty("target_category_id").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Post_ShapeConfirmedBefore_Returns200AndReplacesItsRules()
    {
        Guid groceriesId = await CreateCategoryAsync("Groceries");
        Guid foodId = await CreateCategoryAsync("Food");
        HttpResponseMessage first = await ConfirmAsync(
            ShapeKey,
            CategoryRule(GroceriesSource, groceriesId),
            TransferRule(TransferSource));
        Guid profileId = (await ReadJsonAsync(first)).GetProperty("id").GetGuid();

        HttpResponseMessage again = await ConfirmAsync(ShapeKey, CategoryRule(GroceriesSource, foodId));

        again.StatusCode.Should().Be(HttpStatusCode.OK);
        again.Headers.Location.Should().BeNull();
        JsonElement body = await ReadJsonAsync(again);
        body.GetProperty("id").GetGuid().Should().Be(profileId);
        JsonElement rule = body.GetProperty("rules").EnumerateArray().Should().ContainSingle().Subject;
        rule.GetProperty("target_category_id").GetGuid().Should().Be(foodId);

        await using FinNavisDbContext context = _database.CreateDbContext();
        MappingProfile stored = await context.MappingProfiles
            .Include(profile => profile.Rules)
            .SingleAsync(profile => profile.ShapeKey == ShapeKey);
        stored.Rules.Should().ContainSingle().Which.TargetCategoryId.Should().Be(foodId);
    }

    // The next file of a confirmed shape needs no confirmation, because inspecting it now finds the
    // profile by its shape key.
    [Fact]
    public async Task Post_ShapeOfTheSampleStatement_InspectingThatFileAgainFindsTheProfile()
    {
        byte[] statement = await File.ReadAllBytesAsync(SampleFiles.StatementPath);
        Guid groceriesId = await CreateCategoryAsync("Groceries");
        JsonElement inspection = await InspectAsync(statement);
        string shapeKey = inspection.GetProperty("shape_key").GetString()!;

        HttpResponseMessage confirmed = await ConfirmAsync(shapeKey, CategoryRule("Продукти", groceriesId));
        Guid profileId = (await ReadJsonAsync(confirmed)).GetProperty("id").GetGuid();
        JsonElement inspectionAfter = await InspectAsync(statement);

        confirmed.StatusCode.Should().Be(HttpStatusCode.Created);
        inspection.GetProperty("mapping_profile_id").ValueKind.Should().Be(JsonValueKind.Null);
        inspectionAfter.GetProperty("mapping_profile_id").GetGuid().Should().Be(profileId);
    }

    [Fact]
    public async Task Post_RuleNamingAnUnknownCategory_ReturnsCategoryNotFoundAndRemembersNothing()
    {
        Guid unknownCategoryId = Guid.CreateVersion7();

        HttpResponseMessage response =
            await ConfirmAsync(ShapeKey, CategoryRule(GroceriesSource, unknownCategoryId));

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.NotFound);
        body.GetProperty("code").GetString().Should().Be("Category.NotFound");
        body.GetProperty("message").GetString()
            .Should().Be("There is no category with that name, so a rule cannot point at it.");
        body.GetProperty("details").GetProperty("source_category").GetString().Should().Be(GroceriesSource);
        body.GetProperty("details").GetProperty("target_category_id").GetGuid().Should().Be(unknownCategoryId);
        await using FinNavisDbContext context = _database.CreateDbContext();
        (await context.MappingProfiles.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Post_CategoryRuleThatNamesNoCategory_ReturnsInvalidRule()
    {
        HttpResponseMessage response = await ConfirmAsync(
            ShapeKey,
            new { source_category = GroceriesSource, target = "Category", target_category_id = (Guid?)null });

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.BadRequest);
        body.GetProperty("code").GetString().Should().Be("MappingProfile.InvalidRule");
        body.GetProperty("message").GetString()
            .Should().Be("A rule that files a row under a category must name that category.");
    }

    [Fact]
    public async Task Post_EmptyShapeKey_ReturnsShapeKeyInvalid()
    {
        HttpResponseMessage response = await ConfirmAsync(string.Empty, TransferRule(TransferSource));

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.BadRequest);
        body.GetProperty("code").GetString().Should().Be("MappingProfile.ShapeKeyInvalid");
        body.GetProperty("message").GetString().Should().Be("A file header must be between 1 and 1000 characters.");
    }

    [Fact]
    public async Task Post_TwoRulesForOneFileCategory_ReturnsDuplicateSourceCategory()
    {
        Guid groceriesId = await CreateCategoryAsync("Groceries");

        HttpResponseMessage response = await ConfirmAsync(
            ShapeKey,
            CategoryRule(GroceriesSource, groceriesId),
            TransferRule(GroceriesSource));

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.Conflict);
        body.GetProperty("code").GetString().Should().Be("MappingProfile.DuplicateSourceCategory");
    }

    [Theory]
    [MemberData(nameof(BodiesTheContractRefuses))]
    public async Task Post_BodyTheContractDoesNotAllow_ReturnsInvalidRequestEnvelope(string json)
    {
        using StringContent content = new(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _client.PostAsync(ConfirmPath, content);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.BadRequest);
        body.GetProperty("code").GetString().Should().Be("Request.Invalid");
    }

    [Fact]
    public async Task Post_EmptyRuleList_Returns201WithNoRules()
    {
        HttpResponseMessage response = await ConfirmAsync(ShapeKey);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await ReadJsonAsync(response)).GetProperty("rules").GetArrayLength().Should().Be(0);
    }

    private static object CategoryRule(string sourceCategory, Guid categoryId) =>
        new { source_category = sourceCategory, target = "Category", target_category_id = (Guid?)categoryId };

    private static object TransferRule(string sourceCategory) =>
        new { source_category = sourceCategory, target = "TransferBetweenOwnAccounts", target_category_id = (Guid?)null };

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    private Task<HttpResponseMessage> ConfirmAsync(string shapeKey, params object[] rules) =>
        _client.PostAsJsonAsync(ConfirmPath, new { shape_key = shapeKey, rules });

    private async Task<JsonElement> InspectAsync(byte[] statement)
    {
        using MultipartFormDataContent upload = StatementUpload.Of(statement);
        HttpResponseMessage response = await _client.PostAsync(InspectPath, upload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await ReadJsonAsync(response);
    }

    private async Task<Guid> CreateCategoryAsync(string name)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        CreateCategory createCategory = scope.ServiceProvider.GetRequiredService<CreateCategory>();

        Category category = await createCategory.ExecuteAsync(name, CategoryKind.Expense, null, CancellationToken.None);

        return category.Id;
    }
}
