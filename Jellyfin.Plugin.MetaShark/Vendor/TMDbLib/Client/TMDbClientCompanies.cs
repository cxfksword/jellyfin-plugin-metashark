using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TMDbLib.Objects.Companies;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;
using TMDbLib.Rest;
using TMDbLib.Utilities;

namespace TMDbLib.Client
{
    public partial class TMDbClient
    {
        private async Task<T> GetCompanyMethodInternal<T>(int companyId, CompanyMethods companyMethod, int page = 0, string language = null, CancellationToken cancellationToken = default) where T : new()
        {
            RestRequest req = _client.Create("company/{companyId}/{method}");
            req.AddUrlSegment("companyId", companyId.ToString());
            req.AddUrlSegment("method", companyMethod.GetDescription());

            if (page >= 1)
                req.AddParameter("page", page.ToString());

            language ??= DefaultLanguage;
            if (!string.IsNullOrWhiteSpace(language))
                req.AddParameter("language", language);

            T resp = await req.GetOfT<T>(cancellationToken).ConfigureAwait(false);

            return resp;
        }

        public async Task<Company> GetCompanyAsync(int companyId, CompanyMethods extraMethods = CompanyMethods.Undefined, CancellationToken cancellationToken = default)
        {
            RestRequest req = _client.Create("company/{companyId}");
            req.AddUrlSegment("companyId", companyId.ToString());

            string appends = string.Join(",",
                                         Enum.GetValues(typeof(CompanyMethods))
                                             .OfType<CompanyMethods>()
                                             .Except(new[] { CompanyMethods.Undefined })
                                             .Where(s => extraMethods.HasFlag(s))
                                             .Select(s => s.GetDescription()));

            if (appends != string.Empty)
                req.AddParameter("append_to_response", appends);

            //req.DateFormat = "yyyy-MM-dd";

            Company resp = await req.GetOfT<Company>(cancellationToken).ConfigureAwait(false);

            return resp;
        }

        public async Task<SearchContainerWithId<SearchMovie>> GetCompanyMoviesAsync(int companyId, int page = 0, CancellationToken cancellationToken = default)
        {
            return await GetCompanyMoviesAsync(companyId, DefaultLanguage, page, cancellationToken).ConfigureAwait(false);
        }

        public async Task<SearchContainerWithId<SearchMovie>> GetCompanyMoviesAsync(int companyId, string language, int page = 0, CancellationToken cancellationToken = default)
        {
            return await GetCompanyMethodInternal<SearchContainerWithId<SearchMovie>>(companyId, CompanyMethods.Movies, page, language, cancellationToken).ConfigureAwait(false);
        }
    }
}