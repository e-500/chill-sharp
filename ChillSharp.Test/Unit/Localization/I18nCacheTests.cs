using ChillSharp.I18n.Contracts;
using ChillSharp.I18n.Services;

namespace ChillSharp.Test;

[TestClass]
public sealed class I18nCacheTests
{
    [TestMethod]
    public void SetTextAndTryGet_NormalizesCultureAndReplacesExistingValue()
    {
        var cache = new ChillI18nCache();
        var labelGuid = Guid.NewGuid();
        var original = new GetTextResponse { LabelGuid = labelGuid, CultureName = " en-GB ", Value = "first" };
        var replacement = new GetTextResponse { LabelGuid = labelGuid, CultureName = "en-GB", Value = "second" };

        Assert.AreSame(original, cache.SetText(original));
        Assert.AreSame(replacement, cache.SetText(replacement));
        Assert.IsTrue(cache.TryGet(labelGuid, " en-GB ", out var cached));
        Assert.AreSame(replacement, cached);
        Assert.IsFalse(cache.TryGet(labelGuid, "it-IT", out _));
        Assert.Throws<ArgumentNullException>(() => cache.SetText(null!));
    }

    [TestMethod]
    public void InvalidateAndInvalidateAll_RemoveCachedTexts()
    {
        var cache = new ChillI18nCache();
        var firstGuid = Guid.NewGuid();
        var secondGuid = Guid.NewGuid();
        cache.SetText(new GetTextResponse { LabelGuid = firstGuid, CultureName = "it-IT", Value = "uno" });
        cache.SetText(new GetTextResponse { LabelGuid = secondGuid, CultureName = "en-GB", Value = "one" });
        cache.SetText(new GetTextResponse { LabelGuid = firstGuid, CultureName = " ", Value = "default" });

        cache.Invalidate(firstGuid, " it-IT ");

        Assert.IsFalse(cache.TryGet(firstGuid, "it-IT", out _));
        Assert.IsTrue(cache.TryGet(secondGuid, "en-GB", out _));
        Assert.IsTrue(cache.TryGet(firstGuid, string.Empty, out _));

        cache.InvalidateAll();

        Assert.IsFalse(cache.TryGet(secondGuid, "en-GB", out _));
        Assert.IsFalse(cache.TryGet(firstGuid, string.Empty, out _));
    }
}
