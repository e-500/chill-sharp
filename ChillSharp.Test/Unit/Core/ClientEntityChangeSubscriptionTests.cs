using ChillSharp.Client;

namespace ChillSharp.Test;

[TestClass]
public sealed class ClientEntityChangeSubscriptionTests
{
    [TestMethod]
    public async Task UnsubscribeAndDispose_InvokeCallbackOnlyOnce()
    {
        var subscriptionId = Guid.NewGuid();
        var calls = new List<Guid>();
        var subscription = new ChillClientEntityChangeSubscription(
            subscriptionId,
            "Blog",
            null,
            id =>
            {
                calls.Add(id);
                return Task.CompletedTask;
            });

        Assert.AreEqual("Blog", subscription.ChillType);
        Assert.IsNull(subscription.Guid);

        await subscription.UnsubscribeAsync();
        await subscription.DisposeAsync();
        await subscription.UnsubscribeAsync();

        CollectionAssert.AreEqual(new[] { subscriptionId }, calls);
    }

    [TestMethod]
    public async Task DisposeAsync_UnsubscribesEntitySpecificSubscription()
    {
        var subscriptionId = Guid.NewGuid();
        var entityGuid = Guid.NewGuid();
        Guid? unsubscribedId = null;
        var subscription = new ChillClientEntityChangeSubscription(
            subscriptionId,
            "Post",
            entityGuid,
            id =>
            {
                unsubscribedId = id;
                return Task.CompletedTask;
            });

        await subscription.DisposeAsync();

        Assert.AreEqual("Post", subscription.ChillType);
        Assert.AreEqual(entityGuid, subscription.Guid);
        Assert.AreEqual(subscriptionId, unsubscribedId);
    }
}
