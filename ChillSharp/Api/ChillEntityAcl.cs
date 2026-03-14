using System.Security.Claims;

namespace ChillSharp.Api;

/// <summary>
/// Describes the entity-level actions that can be authorized by ChillSharp.
/// </summary>
public enum ChillEntityAclAction
{
    /// <summary>
    /// Query or find access.
    /// </summary>
    Query = 1,

    /// <summary>
    /// Create access.
    /// </summary>
    Create = 2,

    /// <summary>
    /// Update access.
    /// </summary>
    Update = 3,

    /// <summary>
    /// Delete access.
    /// </summary>
    Delete = 4
}

/// <summary>
/// Defines the contract for entity-level ACL checks used by the Chill API controller.
/// </summary>
public interface IChillEntityAclService
{
    /// <summary>
    /// Evaluates whether the current principal can perform an action on a logical entity resource.
    /// </summary>
    /// <param name="principal">The authenticated principal to evaluate.</param>
    /// <param name="module">The logical module containing the entity.</param>
    /// <param name="entityName">The logical entity name.</param>
    /// <param name="action">The entity-level action to authorize.</param>
    /// <param name="cancellationToken">Token used to cancel the authorization request.</param>
    /// <returns><see langword="true"/> when the action is allowed; otherwise <see langword="false"/>.</returns>
    Task<bool> AuthorizeAsync(ClaimsPrincipal principal, string module, string entityName, ChillEntityAclAction action, CancellationToken cancellationToken = default);
}
