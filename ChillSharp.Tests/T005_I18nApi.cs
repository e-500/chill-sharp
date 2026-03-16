using ChillSharp.EF.ServiceModel.I18n;
using ChillSharp.I18n.Contracts;
using System.Net;
using System.Net.Http.Json;

namespace ChillSharp.Tests;

[TestClass]
public sealed class I18nApi
{
    [TestMethod]
    public async Task Step001_GetTextByLabelGuidAndCultureName()
    {
        TestApiHost.EnsureStarted();

        var labelGuid = Guid.NewGuid();

        await using (var context = TestApiHost.CreateDbContext())
        {
            context.Texts.Add(new Text
            {
                Guid = Guid.NewGuid(),
                LabelGuid = labelGuid,
                CultureCode = "it-IT",
                Value = "Ciao mondo"
            });
            await context.SaveChangesAsync();
        }

        using var client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000/")
        };

        var response = await client.GetAsync($"api/chill-i18n/text/{labelGuid}/it-IT");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<GetTextResponse>();
        Assert.IsNotNull(payload);
        Assert.AreEqual(labelGuid, payload.LabelGuid);
        Assert.AreEqual("it-IT", payload.CultureName);
        Assert.AreEqual("Ciao mondo", payload.Value);
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
}
