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
            return SendJson<GetTextResponse>(HttpMethod.Get, BuildI18nUrl("get-text"), PrepareGetTextRequest(request), allowAnonymous: true);
        }

        /// <summary>
        /// Gets multiple localized texts.
        /// </summary>
        public List<GetTextResponse?> GetTexts(IEnumerable<GetTextRequest> requests)
        {
            if (requests == null)
                throw new ArgumentNullException(nameof(requests));

            var preparedRequests = requests.Select(PrepareGetTextRequest).ToList();
            return SendJson<List<GetTextResponse?>>(HttpMethod.Get, BuildI18nUrl("get-multiple-text"), preparedRequests, allowAnonymous: true)
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
