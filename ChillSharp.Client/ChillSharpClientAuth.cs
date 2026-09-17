using ChillSharp.Auth.Contracts;
using ChillSharp.Auth.Model;
using System.Net.Http;

namespace ChillSharp.Client
{
    /// <summary>
    /// Adds client methods for interacting with the ChillSharp auth API module.
    /// </summary>
    public partial class ChillSharpClient
    {
        /// <summary>
        /// Registers a new Identity account and stores the returned token pair inside the client.
        /// </summary>
        public AuthTokenResponse RegisterAuthAccount(RegisterAuthIdentityRequest request)
        {
            var result = SendAuthJson<AuthTokenResponse>(HttpMethod.Post, "account/register", request, allowAnonymous: true);
            if (result == null) throw new ChillClientException("Unexpected null auth register result");
            ApplyAuthToken(result, forgetPassword: true);
            return result;
        }

        /// <summary>
        /// Authenticates an Identity account with user name and password and stores the returned token pair.
        /// </summary>
        public AuthTokenResponse LoginAuthAccount(LoginAuthIdentityRequest request)
        {
            var result = SendAuthJson<AuthTokenResponse>(HttpMethod.Post, "account/login", request, allowAnonymous: true);
            if (result == null) throw new ChillClientException("Unexpected null auth login result");
            ApplyAuthToken(result, forgetPassword: true);
            return result;
        }

        /// <summary>
        /// Exchanges the current refresh token for a new token pair and stores it inside the client.
        /// </summary>
        public AuthTokenResponse RefreshAuthAccount()
        {
            var result = GetAuthTokenWithPasswordIfNecessary(forceRefresh: true);
            if (result == null) throw new ChillClientException("Unexpected null auth refresh result");
            return result;
        }

        /// <summary>
        /// Changes the password of the authenticated user.
        /// </summary>
        public ChangePasswordResponse ChangeAuthPassword(ChangePasswordRequest request)
        {
            var result = SendAuthJson<ChangePasswordResponse>(HttpMethod.Post, "account/change-password", request);
            if (result == null) throw new ChillClientException("Unexpected null change-password result");
            return result;
        }

        /// <summary>
        /// Requests a password-reset token for a user.
        /// </summary>
        public PasswordResetTokenResponse RequestAuthPasswordReset(RequestPasswordResetRequest request)
        {
            var result = SendAuthJson<PasswordResetTokenResponse>(HttpMethod.Post, "account/request-password-reset", request, allowAnonymous: true);
            if (result == null) throw new ChillClientException("Unexpected null request-password-reset result");
            return result;
        }

        /// <summary>
        /// Resets a password by using a reset token.
        /// </summary>
        public ResetPasswordResponse ResetAuthPassword(ResetPasswordRequest request)
        {
            var result = SendAuthJson<ResetPasswordResponse>(HttpMethod.Post, "account/reset-password", request, allowAnonymous: true);
            if (result == null) throw new ChillClientException("Unexpected null reset-password result");
            return result;
        }

        /// <summary>
        /// Returns all auth users.
        /// </summary>
        public List<AuthUser> GetAuthUsers()
        {
            return SendAuthJson<List<AuthUser>>(HttpMethod.Get, "users") ?? new List<AuthUser>();
        }

        /// <summary>
        /// Returns a single auth user by identifier.
        /// </summary>
        public AuthUser? GetAuthUser(Guid userGuid)
        {
            return SendAuthJson<AuthUser>(HttpMethod.Get, $"users/{userGuid}");
        }

        /// <summary>
        /// Creates a new auth user.
        /// </summary>
        public AuthUser CreateAuthUser(CreateAuthUserRequest request)
        {
            var result = SendAuthJson<AuthUser>(HttpMethod.Post, "users", request);
            if (result == null) throw new ChillClientException("Unexpected null auth user result");
            return result;
        }

        /// <summary>
        /// Updates an existing auth user.
        /// </summary>
        public AuthUser? UpdateAuthUser(Guid userGuid, UpdateAuthUserRequest request)
        {
            return SendAuthJson<AuthUser>(HttpMethod.Put, $"users/{userGuid}", request);
        }

        /// <summary>
        /// Deletes an auth user.
        /// </summary>
        public void DeleteAuthUser(Guid userGuid)
        {
            SendAuthJson<object>(HttpMethod.Delete, $"users/{userGuid}", expectResponseBody: false);
        }

        /// <summary>
        /// Returns the roles assigned to a user.
        /// </summary>
        public List<AuthRole> GetAuthUserRoles(Guid userGuid)
        {
            return SendAuthJson<List<AuthRole>>(HttpMethod.Get, $"users/{userGuid}/roles") ?? new List<AuthRole>();
        }

        /// <summary>
        /// Assigns a role to a user.
        /// </summary>
        public void AssignAuthRole(Guid userGuid, Guid roleGuid)
        {
            SendAuthJson<object>(HttpMethod.Put, $"users/{userGuid}/roles/{roleGuid}", payload: null, expectResponseBody: false);
        }

        /// <summary>
        /// Removes a role assignment from a user.
        /// </summary>
        public void RemoveAuthRole(Guid userGuid, Guid roleGuid)
        {
            SendAuthJson<object>(HttpMethod.Delete, $"users/{userGuid}/roles/{roleGuid}", payload: null, expectResponseBody: false);
        }

        /// <summary>
        /// Returns all auth roles.
        /// </summary>
        public List<AuthRole> GetAuthRoles()
        {
            return SendAuthJson<List<AuthRole>>(HttpMethod.Get, "roles") ?? new List<AuthRole>();
        }

        /// <summary>
        /// Returns a single auth role by identifier.
        /// </summary>
        public AuthRole? GetAuthRole(Guid roleGuid)
        {
            return SendAuthJson<AuthRole>(HttpMethod.Get, $"roles/{roleGuid}");
        }

        /// <summary>
        /// Creates a new auth role.
        /// </summary>
        public AuthRole CreateAuthRole(CreateAuthRoleRequest request)
        {
            var result = SendAuthJson<AuthRole>(HttpMethod.Post, "roles", request);
            if (result == null) throw new ChillClientException("Unexpected null auth role result");
            return result;
        }

        /// <summary>
        /// Updates an existing auth role.
        /// </summary>
        public AuthRole? UpdateAuthRole(Guid roleGuid, UpdateAuthRoleRequest request)
        {
            return SendAuthJson<AuthRole>(HttpMethod.Put, $"roles/{roleGuid}", request);
        }

        /// <summary>
        /// Deletes an auth role.
        /// </summary>
        public void DeleteAuthRole(Guid roleGuid)
        {
            SendAuthJson<object>(HttpMethod.Delete, $"roles/{roleGuid}", payload: null, expectResponseBody: false);
        }

        /// <summary>
        /// Returns permission rules filtered by optional user and role identifiers.
        /// </summary>
        public List<AuthPermissionRule> GetAuthPermissionRules(Guid? userGuid = null, Guid? roleGuid = null)
        {
            var query = new List<string>();
            if (userGuid.HasValue)
                query.Add($"userGuid={Uri.EscapeDataString(userGuid.Value.ToString())}");
            if (roleGuid.HasValue)
                query.Add($"roleGuid={Uri.EscapeDataString(roleGuid.Value.ToString())}");

            var suffix = query.Count == 0 ? string.Empty : "?" + string.Join("&", query);
            return SendAuthJson<List<AuthPermissionRule>>(HttpMethod.Get, $"permissions{suffix}") ?? new List<AuthPermissionRule>();
        }

        /// <summary>
        /// Returns a single permission rule by identifier.
        /// </summary>
        public AuthPermissionRule? GetAuthPermissionRule(Guid ruleGuid)
        {
            return SendAuthJson<AuthPermissionRule>(HttpMethod.Get, $"permissions/{ruleGuid}");
        }

        /// <summary>
        /// Creates a permission rule.
        /// </summary>
        public AuthPermissionRule CreateAuthPermissionRule(CreateAuthPermissionRuleRequest request)
        {
            var result = SendAuthJson<AuthPermissionRule>(HttpMethod.Post, "permissions", request);
            if (result == null) throw new ChillClientException("Unexpected null auth permission rule result");
            return result;
        }

        /// <summary>
        /// Updates a permission rule.
        /// </summary>
        public AuthPermissionRule? UpdateAuthPermissionRule(Guid ruleGuid, UpdateAuthPermissionRuleRequest request)
        {
            return SendAuthJson<AuthPermissionRule>(HttpMethod.Put, $"permissions/{ruleGuid}", request);
        }

        /// <summary>
        /// Deletes a permission rule.
        /// </summary>
        public void DeleteAuthPermissionRule(Guid ruleGuid)
        {
            SendAuthJson<object>(HttpMethod.Delete, $"permissions/{ruleGuid}", payload: null, expectResponseBody: false);
        }

        /// <summary>
        /// Evaluates an entity-level permission for a user.
        /// </summary>
        public PermissionEvaluationResult EvaluateAuthEntityPermission(EvaluateEntityPermissionRequest request)
        {
            var result = SendAuthJson<PermissionEvaluationResult>(HttpMethod.Post, "permissions/evaluate/entity", request);
            if (result == null) throw new ChillClientException("Unexpected null entity permission result");
            return result;
        }

        /// <summary>
        /// Evaluates a property-level permission for a user.
        /// </summary>
        public PermissionEvaluationResult EvaluateAuthPropertyPermission(EvaluatePropertyPermissionRequest request)
        {
            var result = SendAuthJson<PermissionEvaluationResult>(HttpMethod.Post, "permissions/evaluate/property", request);
            if (result == null) throw new ChillClientException("Unexpected null property permission result");
            return result;
        }

        /// <summary>
        /// Evaluates a property-level permission for multiple properties.
        /// </summary>
        public PropertyPermissionSetResult EvaluateAuthPropertySetPermission(EvaluatePropertySetPermissionRequest request)
        {
            var result = SendAuthJson<PropertyPermissionSetResult>(HttpMethod.Post, "permissions/evaluate/property-set", request);
            if (result == null) throw new ChillClientException("Unexpected null property set permission result");
            return result;
        }
    }
}
