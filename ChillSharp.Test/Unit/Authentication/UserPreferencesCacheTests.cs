using ChillSharp;
using ChillSharp.Auth.Services;

namespace ChillSharp.Test;

[TestClass]
public class ChillAuthUserPreferencesCacheTests
{
    [TestMethod]
    public void SetThenInvalidate_RemovesOnlyTheTargetUsersSnapshot()
    {
        var cache = new ChillAuthUserPreferencesCache();
        var first = new ChillUserPreferences("it-IT", "Europe/Rome", "dd/MM/yyyy", "N2", "cini");
        var second = new ChillUserPreferences("en-GB", "Europe/London", "dd/MM/yyyy", "N2", "soft");

        cache.Set(" user-1 ", first);
        cache.Set("user-2", second);
        cache.Invalidate("user-1");

        Assert.IsFalse(cache.TryGet("user-1", out _));
        Assert.IsTrue(cache.TryGet("user-2", out var retained));
        Assert.AreSame(second, retained);
    }
}
