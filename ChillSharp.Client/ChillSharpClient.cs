/*
 * ChillSharp is a lightweight .NET library that sits on top of Entity Framework Core
 * and turns an existing data model into a fully working REST API with almost no setup.
 * Copyright (C) 2025 Andrea Piovesan
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using ChillSharp.Auth.Contracts;
using ChillSharp.Client.Dto;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ChillSharp.Client
{
    /// <summary>
    /// Lightweight client for interacting with the ChillSharp API.
    /// Provides methods for querying, CRUD operations, batch actions, and optional auth-token management.
    /// </summary>
    public partial class ChillSharpClient
    {
        private readonly object _authSyncRoot = new();
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private string _BaseUrl = string.Empty;
        private string? _UserName;
        private string? _Password;
        private string? _CultureName;
        private string? _AccessToken;
        private DateTimeOffset? _AccessTokenIssuedUtc;
        private DateTimeOffset? _AccessTokenExpiresUtc;
        private string? _RefreshToken;
        private DateTimeOffset? _RefreshTokenExpiresUtc;

        /// <summary>
        /// Initializes the client with the base URL of the ChillSharp API.
        /// Removes trailing slashes for consistent request formatting.
        /// </summary>
        /// <param name="BaseUrl">Base endpoint of the ChillSharp server.</param>
        public ChillSharpClient(string BaseUrl, string? CultureName = null)
        {
            _BaseUrl = NormalizeBaseUrl(BaseUrl);
            _CultureName = NormalizeOptionalValue(CultureName);
        }

        /// <summary>
        /// Initializes the client with a pre-issued bearer token.
        /// </summary>
        /// <param name="BaseUrl">Base endpoint of the ChillSharp server.</param>
        /// <param name="AuthToken">Bearer token applied to outgoing requests.</param>
        public ChillSharpClient(string BaseUrl, string AuthToken, string? CultureName = null)
            : this(BaseUrl, CultureName)
        {
            _AccessToken = NormalizeRequiredValue(AuthToken, nameof(AuthToken));
        }

        /// <summary>
        /// Initializes the client with user credentials. The client will exchange them for tokens on demand.
        /// </summary>
        /// <param name="BaseUrl">Base endpoint of the ChillSharp server.</param>
        /// <param name="UserName">User name or email used to authenticate.</param>
        /// <param name="Password">Password used to authenticate.</param>
        public ChillSharpClient(string BaseUrl, string UserName, string Password, string? CultureName = null)
            : this(BaseUrl, CultureName)
        {
            _UserName = NormalizeRequiredValue(UserName, nameof(UserName));
            _Password = NormalizeRequiredValue(Password, nameof(Password));
        }

        /// <summary>
        /// Sends a query request to the ChillSharp API.
        /// </summary>
        /// <param name="Query">Query DTO defining filters and parameters.</param>
        /// <returns>The response mapped back into a ChillDtoQuery object.</returns>
        public ChillDtoQuery Query(ChillDtoQuery Query)
        {
            var result = SendJson<ChillDtoQuery>(HttpMethod.Post, BuildChillUrl("query"), Query);
            if (result == null)
                throw new ChillClientException("Unexpected null query result");
            return result;
        }

        /// <summary>
        /// Executes a FIND operation on the given entity.
        /// </summary>
        public ChillDtoEntity? Find(ChillDtoEntity Entity)
        {
            return Action("FIND", Entity);
        }

        /// <summary>
        /// Executes a CREATE operation on the given entity.
        /// </summary>
        /// <param name="Entity">Entity payload for the action.</param>
        /// <returns>The server response mapped to ChillDtoEntity.</returns>
        public ChillDtoEntity Create(ChillDtoEntity Entity)
        {
            var res = Action("CREATE", Entity);
            if (res == null) throw new ChillClientException("Unexpected null entity result");
            return res;
        }

        /// <summary>
        /// Executes an UPDATE operation on the given entity.
        /// </summary>
        /// <param name="Entity">Entity payload for the action.</param>
        /// <returns>The server response mapped to ChillDtoEntity.</returns>
        public ChillDtoEntity Update(ChillDtoEntity Entity)
        {
            var res = Action("UPDATE", Entity);
            if (res == null) throw new ChillClientException("Unexpected null entity result");
            return res;
        }

        /// <summary>
        /// Executes a DELETE operation on the given entity.
        /// </summary>
        /// <param name="Entity">Entity payload for the action.</param>
        public void Delete(ChillDtoEntity Entity)
        {
            Action("DELETE", Entity);
        }

        /// <summary>
        /// Internal method used by the CRUD helpers to send
        /// an action-based request to the API.
        /// </summary>
        /// <param name="Action">Action verb (FIND, CREATE, UPDATE, DELETE).</param>
        /// <param name="Entity">Entity payload for the action.</param>
        /// <returns>The server response mapped to ChillDtoEntity.</returns>
        protected ChillDtoEntity? Action(string Action, ChillDtoEntity Entity)
        {
            var expectResponseBody = !string.Equals(Action, "DELETE", StringComparison.OrdinalIgnoreCase);
            return SendJson<ChillDtoEntity>(HttpMethod.Post, BuildChillUrl(Action.ToLowerInvariant()), Entity, expectResponseBody);
        }

        /// <summary>
        /// Sends a batch (chunk) of ChillOperation objects to the API.
        /// </summary>
        /// <param name="Chunk">List of operations to process.</param>
        /// <returns>The processed operations returned by the server.</returns>
        public List<ChillOperation> Chunk(List<ChillOperation> Chunk)
        {
            var result = SendJson<List<ChillOperation>>(HttpMethod.Post, BuildChillUrl("chunk"), Chunk);
            if (result == null)
                throw new ChillClientException("Unexpected null chunk result");
            return result;
        }

        /// <summary>
        /// Retrieves the schema definition for a specified chill type and view code from the remote service.
        /// </summary>
        /// <param name="chillType">The identifier of the chill type for which to retrieve the schema. Cannot be null or empty.</param>
        /// <param name="chillViewCode">The code representing the specific view of the chill type. Cannot be null or empty.</param>
        /// <returns>A <see cref="ChillDtoSchema"/> object containing the schema definition if found; otherwise, <see langword="null"/>.</returns>
        /// <exception cref="ChillClientException">Thrown if the remote service returns an error response or if an unexpected error occurs during the request.</exception>
        public ChillDtoSchema? GetSchema(string chillType, string chillViewCode, string? cultureName = null)
        {
            var encodedType = Uri.EscapeDataString(chillType);
            var encodedView = Uri.EscapeDataString(chillViewCode);
            var effectiveCultureName = NormalizeOptionalValue(cultureName) ?? _CultureName;
            var relativeUrl = $"get-schema?chillType={encodedType}&chillViewCode={encodedView}";
            if (!string.IsNullOrWhiteSpace(effectiveCultureName))
            {
                relativeUrl += $"&cultureName={Uri.EscapeDataString(effectiveCultureName)}";
            }

            return SendJson<ChillDtoSchema>(HttpMethod.Get, BuildChillUrl(relativeUrl), payload: null);
        }

        /// <summary>
        /// Sends a schema definition to the remote service.
        /// </summary>
        /// <param name="schema">
        /// The <see cref="ChillDtoSchema"/> object containing the schema definition,
        /// including chillType and chillViewCode. Cannot be null.
        /// </param>
        /// <exception cref="ChillClientException">
        /// Thrown if the remote service returns an error response or if an unexpected error occurs during the request.
        /// </exception>
        public void SetSchema(ChillDtoSchema schema)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            SendJson<object>(HttpMethod.Post, BuildChillUrl("set-schema"), schema, expectResponseBody: false);
        }

        internal T? SendAuthJson<T>(HttpMethod method, string relativeUrl, object? payload = null, bool expectResponseBody = true, bool allowAnonymous = false)
        {
            return SendJson<T>(method, BuildAuthUrl(relativeUrl), payload, expectResponseBody, allowAnonymous);
        }

        internal AuthTokenResponse GetAuthTokenWithPasswordIfNecessary(bool forceRefresh = false)
        {
            lock (_authSyncRoot)
            {
                if (!forceRefresh && HasUsableAccessToken() && !ShouldRefreshAccessToken())
                {
                    return CreateCurrentTokenResponse();
                }

                if (!string.IsNullOrWhiteSpace(_RefreshToken) && (!forceRefresh || string.IsNullOrWhiteSpace(_Password)))
                {
                    try
                    {
                        var refreshedToken = SendAuthJson<AuthTokenResponse>(
                            HttpMethod.Post,
                            "account/refresh",
                            new RefreshAuthTokenRequest { RefreshToken = _RefreshToken },
                            allowAnonymous: true);

                        if (refreshedToken != null)
                        {
                            ApplyAuthToken(refreshedToken, forgetPassword: true);
                            return refreshedToken;
                        }
                    }
                    catch (ChillClientException)
                    {
                        _RefreshToken = null;
                        _RefreshTokenExpiresUtc = null;
                    }
                }

                if (!string.IsNullOrWhiteSpace(_UserName) && !string.IsNullOrWhiteSpace(_Password))
                {
                    var token = SendAuthJson<AuthTokenResponse>(
                        HttpMethod.Post,
                        "account/login",
                        new LoginAuthIdentityRequest
                        {
                            UserNameOrEmail = _UserName,
                            Password = _Password
                        },
                        allowAnonymous: true);

                    if (token == null)
                    {
                        throw new ChillClientException("Unexpected null token result.");
                    }

                    ApplyAuthToken(token, forgetPassword: true);
                    return token;
                }

                if (HasUsableAccessToken())
                {
                    return CreateCurrentTokenResponse();
                }

                throw new ChillClientException("No auth token is available and the client cannot obtain a new one.");
            }
        }

        internal void ApplyAuthToken(AuthTokenResponse tokenResponse, bool forgetPassword)
        {
            _AccessToken = tokenResponse.AccessToken;
            _AccessTokenIssuedUtc = tokenResponse.AccessTokenIssuedUtc;
            _AccessTokenExpiresUtc = tokenResponse.AccessTokenExpiresUtc;
            _RefreshToken = string.IsNullOrWhiteSpace(tokenResponse.RefreshToken) ? null : tokenResponse.RefreshToken;
            _RefreshTokenExpiresUtc = string.IsNullOrWhiteSpace(tokenResponse.RefreshToken) ? null : tokenResponse.RefreshTokenExpiresUtc;

            if (!string.IsNullOrWhiteSpace(tokenResponse.UserName))
            {
                _UserName = tokenResponse.UserName;
            }

            if (forgetPassword)
            {
                _Password = null;
            }
        }

        internal string GetAuthBaseUrl()
        {
            const string chillSuffix = "/chill";
            if (_BaseUrl.EndsWith(chillSuffix, StringComparison.OrdinalIgnoreCase))
            {
                return _BaseUrl.Substring(0, _BaseUrl.Length - chillSuffix.Length) + "/chill-auth";
            }

            return _BaseUrl.TrimEnd('/') + "-auth";
        }

        private T? SendJson<T>(HttpMethod method, string url, object? payload = null, bool expectResponseBody = true, bool allowAnonymous = false, bool allowRetry = true)
        {
            var start = DateTime.Now;

            try
            {
                if (!allowAnonymous && CanUseAuthentication())
                {
                    GetAuthTokenWithPasswordIfNecessary();
                }

                using var client = new HttpClient();
                using var request = new HttpRequestMessage(method, url);

                if (!allowAnonymous && !string.IsNullOrWhiteSpace(_AccessToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _AccessToken);
                }

                if (payload != null)
                {
                    var jsonString = JsonSerializer.Serialize(payload, _jsonOptions);
                    request.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                }

                var response = client.SendAsync(request).GetAwaiter().GetResult();

                if ((response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden) &&
                    !allowAnonymous &&
                    allowRetry &&
                    TryRefreshAuthentication())
                {
                    return SendJson<T>(method, url, payload, expectResponseBody, allowAnonymous, allowRetry: false);
                }

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"\n\nExecution time {Math.Round((DateTime.Now - start).TotalMilliseconds / 1000, 2)} s");
                    if (!expectResponseBody)
                    {
                        return default;
                    }

                    var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (string.IsNullOrWhiteSpace(responseBody))
                    {
                        return default;
                    }

                    return JsonSerializer.Deserialize<T>(responseBody, _jsonOptions);
                }

                var errorDetails = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                throw new ChillClientException($"Error: {response.StatusCode} {errorDetails}");
            }
            catch (ChillClientException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ChillClientException($"Unexpected error executing {method} {url}, see inner exception for details", ex);
            }
        }

        private string BuildChillUrl(string relativeUrl)
        {
            return $"{_BaseUrl}/{relativeUrl.TrimStart('/')}";
        }

        private string BuildAuthUrl(string relativeUrl)
        {
            return $"{GetAuthBaseUrl().TrimEnd('/')}/{relativeUrl.TrimStart('/')}";
        }

        private bool CanUseAuthentication()
        {
            return !string.IsNullOrWhiteSpace(_AccessToken) ||
                   !string.IsNullOrWhiteSpace(_RefreshToken) ||
                   (!string.IsNullOrWhiteSpace(_UserName) && !string.IsNullOrWhiteSpace(_Password));
        }

        private bool HasUsableAccessToken()
        {
            if (string.IsNullOrWhiteSpace(_AccessToken))
            {
                return false;
            }

            if (!_AccessTokenExpiresUtc.HasValue)
            {
                return true;
            }

            return DateTimeOffset.UtcNow < _AccessTokenExpiresUtc.Value;
        }

        private bool ShouldRefreshAccessToken()
        {
            if (!_AccessTokenIssuedUtc.HasValue || !_AccessTokenExpiresUtc.HasValue)
            {
                return false;
            }

            var issued = _AccessTokenIssuedUtc.Value;
            var expires = _AccessTokenExpiresUtc.Value;
            if (expires <= issued)
            {
                return true;
            }

            var refreshThreshold = issued + TimeSpan.FromTicks((expires - issued).Ticks * 3 / 4);
            return DateTimeOffset.UtcNow >= refreshThreshold;
        }

        private bool TryRefreshAuthentication()
        {
            if (string.IsNullOrWhiteSpace(_RefreshToken) && string.IsNullOrWhiteSpace(_Password))
            {
                return false;
            }

            try
            {
                GetAuthTokenWithPasswordIfNecessary(forceRefresh: true);
                return true;
            }
            catch (ChillClientException)
            {
                return false;
            }
        }

        private AuthTokenResponse CreateCurrentTokenResponse()
        {
            return new AuthTokenResponse
            {
                AccessToken = _AccessToken ?? string.Empty,
                AccessTokenIssuedUtc = _AccessTokenIssuedUtc ?? DateTimeOffset.MinValue,
                AccessTokenExpiresUtc = _AccessTokenExpiresUtc ?? DateTimeOffset.MaxValue,
                RefreshToken = _RefreshToken ?? string.Empty,
                RefreshTokenExpiresUtc = _RefreshTokenExpiresUtc ?? DateTimeOffset.MinValue,
                UserName = _UserName ?? string.Empty
            };
        }

        private static string NormalizeBaseUrl(string baseUrl)
        {
            var normalized = NormalizeRequiredValue(baseUrl, nameof(baseUrl));
            return normalized.EndsWith("/") ? normalized[..^1] : normalized;
        }

        private static string? NormalizeOptionalValue(string? value)
        {
            var normalized = value?.Trim();
            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }

        private static string NormalizeRequiredValue(string value, string argumentName)
        {
            var normalized = value?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new ArgumentException($"{argumentName} is required.", argumentName);
            }

            return normalized;
        }
    }
}
