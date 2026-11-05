using ChillSharp.EF;

namespace ChillSharp.Test;

[TestClass]
public sealed class ChillDataAnnotationsValidatorTests
{
    [TestMethod]
    public void ThrowIfInvalid_FormatsFieldlessAndFieldSpecificErrors()
    {
        var exception = Assert.Throws<ChillValidationException>(() =>
            ChillDataAnnotationsValidator.ThrowIfInvalid(
            [
                new ChillValidationError { FieldName = null, Message = "General error" },
                new ChillValidationError { FieldName = "Title", Message = "Required" }
            ]));

        Assert.AreEqual($"General error{Environment.NewLine}Title: Required", exception.Message);
    }

    [TestMethod]
    public void ThrowIfInvalid_DoesNotThrowForEmptyErrors()
    {
        ChillDataAnnotationsValidator.ThrowIfInvalid([]);
    }
}
