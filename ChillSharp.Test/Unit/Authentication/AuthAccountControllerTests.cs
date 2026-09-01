using ChillSharp;
using ChillSharp.Auth.Api.Controllers;
using ChillSharp.Auth.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChillSharp.Test.Unit.Authentication;

[TestClass]
public class AuthAccountControllerTests
{
    [TestMethod]
    public void GetCurrentUserPreferences_ReturnsTheAuthenticatedSnapshot()
    {
        var expected = new ChillUserPreferences("it-IT", "Europe/Rome", "dd/MM/yyyy", "N2");
        var controller = new AuthAccountController(userPreferencesAccessor: new TestUserPreferencesAccessor(expected));

        var result = controller.GetCurrentUserPreferences();

        var okResult = result.Result as OkObjectResult;
        Assert.IsNotNull(okResult);
        Assert.AreSame(expected, okResult.Value);
    }

    private sealed class TestUserPreferencesAccessor : IChillAuthUserPreferencesAccessor
    {
        public TestUserPreferencesAccessor(ChillUserPreferences current)
        {
            Current = current;
        }

        public ChillUserPreferences Current { get; }
    }
}
