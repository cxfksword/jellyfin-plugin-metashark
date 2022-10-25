using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TMDbLib.Objects.Configuration;
using TMDbLib.Objects.Countries;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Languages;
using TMDbLib.Objects.Timezones;
using TMDbLib.Rest;

namespace TMDbLib.Client
{
    public partial class TMDbClient
    {
        public async Task<APIConfiguration> GetAPIConfiguration(CancellationToken cancellationToken = default)
        {
            RestRequest req = _client.Create("configuration");

            using RestResponse<APIConfiguration> response = await req.Get<APIConfiguration>(cancellationToken).ConfigureAwait(false);

            return (await response.GetDataObject().ConfigureAwait(false));
        }

        public async Task<List<Country>> GetCountriesAsync(CancellationToken cancellationToken = default)
        {
            RestRequest req = _client.Create("configuration/countries");

            using RestResponse<List<Country>> response = await req.Get<List<Country>>(cancellationToken).ConfigureAwait(false);

            return await response.GetDataObject().ConfigureAwait(false);
        }

        public async Task<List<Language>> GetLanguagesAsync(CancellationToken cancellationToken = default)
        {
            RestRequest req = _client.Create("configuration/languages");

            using RestResponse<List<Language>> response = await req.Get<List<Language>>(cancellationToken).ConfigureAwait(false);

            return (await response.GetDataObject().ConfigureAwait(false));
        }
        
        public async Task<List<string>> GetPrimaryTranslationsAsync(CancellationToken cancellationToken = default)
        {
            RestRequest req = _client.Create("configuration/primary_translations");

            using RestResponse<List<string>> response = await req.Get<List<string>>(cancellationToken).ConfigureAwait(false);

            return (await response.GetDataObject().ConfigureAwait(false));
        }

        public async Task<Timezones> GetTimezonesAsync(CancellationToken cancellationToken = default)
        {
            RestRequest req = _client.Create("timezones/list");

            using RestResponse<List<Dictionary<string, List<string>>>> resp = await req.Get<List<Dictionary<string, List<string>>>>(cancellationToken).ConfigureAwait(false);

            List<Dictionary<string, List<string>>> item = await resp.GetDataObject().ConfigureAwait(false);

            if (item == null)
                return null;

            Timezones result = new Timezones();
            result.List = new Dictionary<string, List<string>>();

            foreach (Dictionary<string, List<string>> dictionary in item)
            {
                KeyValuePair<string, List<string>> item1 = dictionary.First();

                result.List[item1.Key] = item1.Value;
            }

            return result;
        }

        /// <summary>
        /// Retrieves a list of departments and positions within
        /// </summary>
        /// <returns>Valid jobs and their departments</returns>
        public async Task<List<Job>> GetJobsAsync(CancellationToken cancellationToken = default)
        {
            RestRequest req = _client.Create("configuration/jobs");

            using RestResponse<List<Job>> response = await req.Get<List<Job>>(cancellationToken).ConfigureAwait(false);

            return (await response.GetDataObject().ConfigureAwait(false));
        }
    }
}
