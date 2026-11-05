namespace ChillSharp.Test;

[TestClass]
public sealed class ChillSharpInitOptionsTests
{
    [TestMethod]
    public void FromEnvironment_UsesDefaultsAndNormalizesApiBasePath()
    {
        WithEnvironment(null, null, () =>
        {
            var defaults = ChillSharpInitOptions.FromEnvironment();
            Assert.AreEqual(ChillSharpInitOptions.DefaultSystemTimeZoneId, defaults.SystemTimeZoneId);
            Assert.AreEqual(ChillSharpInitOptions.DefaultApiBasePath, defaults.ApiBasePath);
        });

        WithEnvironment("  UTC  ", "  custom/api///  ", () =>
        {
            var configured = ChillSharpInitOptions.FromEnvironment();
            Assert.AreEqual("UTC", configured.SystemTimeZoneId);
            Assert.AreEqual("/custom/api", configured.ApiBasePath);
        });

        WithEnvironment(null, "/", () =>
        {
            Assert.AreEqual("/", ChillSharpInitOptions.FromEnvironment().ApiBasePath);
        });
    }

    [TestMethod]
    public void TryGetTimeZone_HandlesBlankValidAndInvalidIds()
    {
        Assert.IsFalse(ChillSharpInitOptions.TryGetTimeZone("  ", out _));
        Assert.IsTrue(ChillSharpInitOptions.TryGetTimeZone(" UTC ", out var utc));
        Assert.AreEqual(TimeSpan.Zero, utc.BaseUtcOffset);
        Assert.IsFalse(ChillSharpInitOptions.TryGetTimeZone("not-a-real-time-zone", out _));
    }

    [TestMethod]
    public void InitializeAndGetSystemTimeZone_UseConfiguredAndThreadOverrides()
    {
        var previous = ChillSharpInitOptions.Current;

        try
        {
            ChillSharpInitOptions.Initialize(new ChillSharpInitOptions { SystemTimeZoneId = "UTC" });
            Assert.AreEqual("UTC", ChillSharpInitOptions.GetSystemTimeZone().Id);

            using (ChillSharpThreadContext.Begin(
                System.Globalization.CultureInfo.InvariantCulture,
                TimeZoneInfo.Utc))
            {
                Assert.AreSame(TimeZoneInfo.Utc, ChillSharpInitOptions.GetSystemTimeZone());
            }

            ChillSharpInitOptions.Initialize();
            Assert.IsNotNull(ChillSharpInitOptions.Current);
        }
        finally
        {
            ChillSharpInitOptions.Initialize(previous);
        }
    }

    private static void WithEnvironment(string? timeZone, string? basePath, Action action)
    {
        var previousTimeZone = Environment.GetEnvironmentVariable(ChillSharpInitOptions.SystemTimeZoneEnvironmentVariableName);
        var previousBasePath = Environment.GetEnvironmentVariable(ChillSharpInitOptions.ApiBasePathEnvironmentVariableName);

        try
        {
            Environment.SetEnvironmentVariable(ChillSharpInitOptions.SystemTimeZoneEnvironmentVariableName, timeZone);
            Environment.SetEnvironmentVariable(ChillSharpInitOptions.ApiBasePathEnvironmentVariableName, basePath);
            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable(ChillSharpInitOptions.SystemTimeZoneEnvironmentVariableName, previousTimeZone);
            Environment.SetEnvironmentVariable(ChillSharpInitOptions.ApiBasePathEnvironmentVariableName, previousBasePath);
        }
    }
}
