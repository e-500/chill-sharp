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

using ChillSharp.I18n.Contracts;
using System.Net;
using System.Net.Http;

namespace ChillSharp.Client
{
    /// <summary>
    /// Adds client methods for interacting with the ChillSharp i18n API module.
    /// </summary>
    public partial class ChillSharpClient
    {
        /// <summary>
        /// Gets a single localized text.
        /// </summary>
        public GetTextResponse? GetText(GetTextRequest request)
        {
            return SendJson<GetTextResponse>(HttpMethod.Post, BuildI18nUrl("get-text"), PrepareGetTextRequest(request), allowAnonymous: true);
        }

        /// <summary>
        /// Gets multiple localized texts.
        /// </summary>
        public List<GetTextResponse?> GetTexts(IEnumerable<GetTextRequest> requests)
        {
            if (requests == null)
                throw new ArgumentNullException(nameof(requests));

            var preparedRequests = requests.Select(PrepareGetTextRequest).ToList();
            return SendJson<List<GetTextResponse?>>(HttpMethod.Post, BuildI18nUrl("get-multiple-text"), preparedRequests, allowAnonymous: true)
                ?? new List<GetTextResponse?>();
        }

        /// <summary>
        /// Creates or updates a localized text.
        /// </summary>
        public GetTextResponse SetText(SetTextRequest request)
        {
            var result = SendJson<GetTextResponse>(HttpMethod.Put, BuildI18nUrl("set-text"), request);
            if (result == null)
                throw new ChillClientException("Unexpected null i18n set-text result");
            return result;
        }

        private GetTextRequest PrepareGetTextRequest(GetTextRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var effectiveCultureName = NormalizeOptionalValue(request.CultureName) ?? _CultureName;
            if (string.IsNullOrWhiteSpace(effectiveCultureName))
                throw new ArgumentException("CultureName is required.", nameof(request));

            return new GetTextRequest
            {
                LabelGuid = request.LabelGuid,
                CultureName = effectiveCultureName,
                PrimaryCultureName = request.PrimaryCultureName,
                PrimaryDefaultText = request.PrimaryDefaultText,
                SecondaryCultureName = request.SecondaryCultureName,
                SecondaryDefaultText = request.SecondaryDefaultText
            };
        }

        private string GetI18nBaseUrl()
        {
            const string chillSuffix = "/api/chill";
            const string i18nSuffix = "/api/chill-i18n";

            if (_BaseUrl.EndsWith(i18nSuffix, StringComparison.OrdinalIgnoreCase))
            {
                return _BaseUrl;
            }

            if (_BaseUrl.EndsWith(chillSuffix, StringComparison.OrdinalIgnoreCase))
            {
                return _BaseUrl.Substring(0, _BaseUrl.Length - chillSuffix.Length) + i18nSuffix;
            }

            return _BaseUrl.TrimEnd('/') + i18nSuffix;
        }

        private string BuildI18nUrl(string relativeUrl)
        {
            return $"{GetI18nBaseUrl().TrimEnd('/')}/{relativeUrl.TrimStart('/')}";
        }
    }
}
