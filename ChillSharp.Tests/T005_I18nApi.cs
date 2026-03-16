using Microsoft.EntityFrameworkCore;
using ChillSharp.EF.ServiceModel.I18n;
using ChillSharp.I18n.Contracts;
using System.Net;
using System.Net.Http.Json;

namespace ChillSharp.Tests;

[TestClass]
public sealed class I18nApi
{
    [TestMethod]
    public async Task Step001_SetTextAndGetTextUsesCacheUntilInvalidatedBySetText()
    {
        TestApiHost.EnsureStarted();

        var labelGuid = Guid.NewGuid();

        using var client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000/")
        };

        var setResponse = await client.PutAsJsonAsync("api/chill-i18n/text", new SetTextRequest
        {
            LabelGuid = labelGuid,
            CultureName = "it-IT",
            Value = "Ciao mondo"
        });
        setResponse.EnsureSuccessStatusCode();

        var createdPayload = await setResponse.Content.ReadFromJsonAsync<GetTextResponse>();
        Assert.IsNotNull(createdPayload);
        Assert.AreEqual("Ciao mondo", createdPayload.Value);

        var firstGetResponse = await client.GetAsync($"api/chill-i18n/text/{labelGuid}/it-IT");
        firstGetResponse.EnsureSuccessStatusCode();
        var firstPayload = await firstGetResponse.Content.ReadFromJsonAsync<GetTextResponse>();
        Assert.IsNotNull(firstPayload);
        Assert.AreEqual("Ciao mondo", firstPayload.Value);

        await using (var context = TestApiHost.CreateDbContext())
        {
            var text = await context.Texts.FirstAsync(x => x.LabelGuid == labelGuid && x.CultureCode == "it-IT");
            text.Value = "Aggiornamento esterno";
            await context.SaveChangesAsync();
        }

        var cachedGetResponse = await client.GetAsync($"api/chill-i18n/text/{labelGuid}/it-IT");
        cachedGetResponse.EnsureSuccessStatusCode();
        var cachedPayload = await cachedGetResponse.Content.ReadFromJsonAsync<GetTextResponse>();
        Assert.IsNotNull(cachedPayload);
        Assert.AreEqual("Ciao mondo", cachedPayload.Value);

        var updateResponse = await client.PutAsJsonAsync("api/chill-i18n/text", new SetTextRequest
        {
            LabelGuid = labelGuid,
            CultureName = "it-IT",
            Value = "Ciao Italia"
        });
        updateResponse.EnsureSuccessStatusCode();

        var updatedPayload = await updateResponse.Content.ReadFromJsonAsync<GetTextResponse>();
        Assert.IsNotNull(updatedPayload);
        Assert.AreEqual("Ciao Italia", updatedPayload.Value);

        var refreshedGetResponse = await client.GetAsync($"api/chill-i18n/text/{labelGuid}/it-IT");
        refreshedGetResponse.EnsureSuccessStatusCode();
        var refreshedPayload = await refreshedGetResponse.Content.ReadFromJsonAsync<GetTextResponse>();
        Assert.IsNotNull(refreshedPayload);
        Assert.AreEqual("Ciao Italia", refreshedPayload.Value);
    }

    [TestMethod]
    public async Task Step002_MissingTranslationReturnsNotFound()
    {
        TestApiHost.EnsureStarted();

        using var client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000/")
        };

        var response = await client.GetAsync($"api/chill-i18n/text/{Guid.NewGuid()}/it-IT");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Step003_SetTextRejectsEmptyLabelGuid()
    {
        TestApiHost.EnsureStarted();

        using var client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000/")
        };

        var response = await client.PutAsJsonAsync("api/chill-i18n/text", new SetTextRequest
        {
            LabelGuid = Guid.Empty,
            CultureName = "it-IT",
            Value = "Invalid"
        });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

