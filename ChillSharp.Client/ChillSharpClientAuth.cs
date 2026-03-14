using ChillSharp.Auth.Contracts;
using ChillSharp.Auth.Model;
using System.Text;
using System.Text.Json;

namespace ChillSharp.Client
{
    /// <summary>
    /// Adds client methods for interacting with the ChillSharp auth API module.
    /// </summary>
    public partial class ChillSharpClient
    {
        /// <summary>
        /// Returns all auth users.
        /// </summary>
        public List<AuthUser> GetAuthUsers()
        {
            return GetAuth<List<AuthUser>>("users") ?? new List<AuthUser>();
        }

        /// <summary>
        /// Returns a single auth user by identifier.
        /// </summary>
        public AuthUser? GetAuthUser(Guid userGuid)
        {
            return GetAuth<AuthUser>($"users/{userGuid}");
        }

        /// <summary>
        /// Creates a new auth user.
        /// </summary>
        public AuthUser CreateAuthUser(CreateAuthUserRequest request)
        {
            var result = PostAuth<AuthUser>("users", request);
            if (result == null) throw new ChillClientException("Unexpected null auth user result");
            return result;
        }

        /// <summary>
        /// Updates an existing auth user.
        /// </summary>
        public AuthUser? UpdateAuthUser(Guid userGuid, UpdateAuthUserRequest request)
        {
            return PutAuth<AuthUser>($"users/{userGuid}", request);
        }

        /// <summary>
        /// Deletes an auth user.
        /// </summary>
        public void DeleteAuthUser(Guid userGuid)
        {
            DeleteAuth($"users/{userGuid}");
        }

        /// <summary>
        /// Returns the roles assigned to a user.
        /// </summary>
        public List<AuthRole> GetAuthUserRoles(Guid userGuid)
        {
            return GetAuth<List<AuthRole>>($"users/{userGuid}/roles") ?? new List<AuthRole>();
        }

        /// <summary>
        /// Assigns a role to a user.
        /// </summary>
        public void AssignAuthRole(Guid userGuid, Guid roleGuid)
        {
            PutAuthNoBody($"users/{userGuid}/roles/{roleGuid}");
        }

        /// <summary>
        /// Removes a role assignment from a user.
        /// </summary>
        public void RemoveAuthRole(Guid userGuid, Guid roleGuid)
        {
            DeleteAuth($"users/{userGuid}/roles/{roleGuid}");
        }

        /// <summary>
        /// Returns all auth roles.
        /// </summary>
        public List<AuthRole> GetAuthRoles()
        {
            return GetAuth<List<AuthRole>>("roles") ?? new List<AuthRole>();
        }

        /// <summary>
        /// Returns a single auth role by identifier.
        /// </summary>
        public AuthRole? GetAuthRole(Guid roleGuid)
        {
            return GetAuth<AuthRole>($"roles/{roleGuid}");
        }

        /// <summary>
        /// Creates a new auth role.
        /// </summary>
        public AuthRole CreateAuthRole(CreateAuthRoleRequest request)
        {
            var result = PostAuth<AuthRole>("roles", request);
            if (result == null) throw new ChillClientException("Unexpected null auth role result");
            return result;
        }

        /// <summary>
        /// Updates an existing auth role.
        /// </summary>
        public AuthRole? UpdateAuthRole(Guid roleGuid, UpdateAuthRoleRequest request)
        {
            return PutAuth<AuthRole>($"roles/{roleGuid}", request);
        }

        /// <summary>
        /// Deletes an auth role.
        /// </summary>
        public void DeleteAuthRole(Guid roleGuid)
        {
            DeleteAuth($"roles/{roleGuid}");
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
            return GetAuth<List<AuthPermissionRule>>($"permissions{suffix}") ?? new List<AuthPermissionRule>();
        }

        /// <summary>
        /// Returns a single permission rule by identifier.
        /// </summary>
        public AuthPermissionRule? GetAuthPermissionRule(Guid ruleGuid)
        {
            return GetAuth<AuthPermissionRule>($"permissions/{ruleGuid}");
        }

        /// <summary>
        /// Creates a permission rule.
        /// </summary>
        public AuthPermissionRule CreateAuthPermissionRule(CreateAuthPermissionRuleRequest request)
        {
            var result = PostAuth<AuthPermissionRule>("permissions", request);
            if (result == null) throw new ChillClientException("Unexpected null auth permission rule result");
            return result;
        }

        /// <summary>
        /// Updates a permission rule.
        /// </summary>
        public AuthPermissionRule? UpdateAuthPermissionRule(Guid ruleGuid, UpdateAuthPermissionRuleRequest request)
        {
            return PutAuth<AuthPermissionRule>($"permissions/{ruleGuid}", request);
        }

        /// <summary>
        /// Deletes a permission rule.
        /// </summary>
        public void DeleteAuthPermissionRule(Guid ruleGuid)
        {
            DeleteAuth($"permissions/{ruleGuid}");
        }

        /// <summary>
        /// Evaluates an entity-level permission for a user.
        /// </summary>
        public PermissionEvaluationResult EvaluateAuthEntityPermission(EvaluateEntityPermissionRequest request)
        {
            var result = PostAuth<PermissionEvaluationResult>("permissions/evaluate/entity", request);
            if (result == null) throw new ChillClientException("Unexpected null entity permission result");
            return result;
        }

        /// <summary>
        /// Evaluates a property-level permission for a user.
        /// </summary>
        public PermissionEvaluationResult EvaluateAuthPropertyPermission(EvaluatePropertyPermissionRequest request)
        {
            var result = PostAuth<PermissionEvaluationResult>("permissions/evaluate/property", request);
            if (result == null) throw new ChillClientException("Unexpected null property permission result");
            return result;
        }

        /// <summary>
        /// Evaluates a property-level permission for multiple properties.
        /// </summary>
        public PropertyPermissionSetResult EvaluateAuthPropertySetPermission(EvaluatePropertySetPermissionRequest request)
        {
            var result = PostAuth<PropertyPermissionSetResult>("permissions/evaluate/property-set", request);
            if (result == null) throw new ChillClientException("Unexpected null property set permission result");
            return result;
        }

        private T? GetAuth<T>(string relativeUrl)
        {
            return SendAuth<T>(HttpMethod.Get, relativeUrl);
        }

        private T? PostAuth<T>(string relativeUrl, object payload)
        {
            return SendAuth<T>(HttpMethod.Post, relativeUrl, payload);
        }

        private T? PutAuth<T>(string relativeUrl, object payload)
        {
            return SendAuth<T>(HttpMethod.Put, relativeUrl, payload);
        }

        private void PutAuthNoBody(string relativeUrl)
        {
            SendAuth<object>(HttpMethod.Put, relativeUrl, payload: null, expectResponseBody: false);
        }

        private void DeleteAuth(string relativeUrl)
        {
            SendAuth<object>(HttpMethod.Delete, relativeUrl, payload: null, expectResponseBody: false);
        }

        private T? SendAuth<T>(HttpMethod method, string relativeUrl, object? payload = null, bool expectResponseBody = true)
        {
            DateTime start = DateTime.Now;
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            string url = $"{GetAuthBaseUrl().TrimEnd('/')}/{relativeUrl.TrimStart('/')}";

            using (HttpClient client = new HttpClient())
            using (var request = new HttpRequestMessage(method, url))
            {
                if (payload != null)
                {
                    string jsonString = JsonSerializer.Serialize(payload, options);
                    request.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                }

                try
                {
                    var req = client.SendAsync(request);
                    req.Wait();
                    HttpResponseMessage response = req.Result;

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"\n\nExecution time {Math.Round((DateTime.Now - start).TotalMilliseconds / 1000, 2)} s");
                        if (!expectResponseBody)
                        {
                            return default;
                        }

                        var res = response.Content.ReadAsStringAsync();
                        res.Wait();
                        string responseBody = res.Result;
                        if (string.IsNullOrWhiteSpace(responseBody))
                        {
                            return default;
                        }

                        return JsonSerializer.Deserialize<T>(responseBody, options);
                    }
                    else
                    {
                        var res = response.Content.ReadAsStringAsync();
                        res.Wait();
                        string errorDetails = res.Result;
                        throw new ChillClientException($"Error: {response.StatusCode} {errorDetails}");
                    }
                }
                catch (Exception ex)
                {
                    throw new ChillClientException($"Unexpected error executing auth request {method} {relativeUrl}, see inner exception for details", ex);
                }
            }
        }

        private string GetAuthBaseUrl()
        {
            const string chillSuffix = "/chill";
            if (_BaseUrl.EndsWith(chillSuffix, StringComparison.OrdinalIgnoreCase))
            {
                return _BaseUrl.Substring(0, _BaseUrl.Length - chillSuffix.Length) + "/chill-auth";
            }

            return _BaseUrl.TrimEnd('/') + "-auth";
        }
    }
}
