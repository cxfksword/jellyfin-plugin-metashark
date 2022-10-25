using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TMDbLib.Objects.General;
using TMDbLib.Objects.People;
using TMDbLib.Rest;
using TMDbLib.Utilities;

namespace TMDbLib.Client
{
    public partial class TMDbClient
    {
        private async Task<T> GetPersonMethodInternal<T>(int personId, PersonMethods personMethod, string dateFormat = null, string country = null, string language = null,
            int page = 0, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default) where T : new()
        {
            RestRequest req = _client.Create("person/{personId}/{method}");
            req.AddUrlSegment("personId", personId.ToString());
            req.AddUrlSegment("method", personMethod.GetDescription());

            // TODO: Dateformat?
            //if (dateFormat != null)
            //    req.DateFormat = dateFormat;

            if (country != null)
                req.AddParameter("country", country);

            language ??= DefaultLanguage;
            if (!string.IsNullOrWhiteSpace(language))
                req.AddParameter("language", language);

            if (page >= 1)
                req.AddParameter("page", page.ToString());
            if (startDate.HasValue)
                req.AddParameter("startDate", startDate.Value.ToString("yyyy-MM-dd"));
            if (endDate != null)
                req.AddParameter("endDate", endDate.Value.ToString("yyyy-MM-dd"));

            T resp = await req.GetOfT<T>(cancellationToken).ConfigureAwait(false);

            return resp;
        }

        public async Task<Person> GetLatestPersonAsync(CancellationToken cancellationToken = default)
        {
            RestRequest req = _client.Create("person/latest");

            // TODO: Dateformat?
            //req.DateFormat = "yyyy-MM-dd";

            Person resp = await req.GetOfT<Person>(cancellationToken).ConfigureAwait(false);

            return resp;
        }

        public async Task<Person> GetPersonAsync(int personId, PersonMethods extraMethods = PersonMethods.Undefined,
            CancellationToken cancellationToken = default)
        {
            return await GetPersonAsync(personId, DefaultLanguage, extraMethods, cancellationToken).ConfigureAwait(false);
        }

        public async Task<Person> GetPersonAsync(int personId, string language, PersonMethods extraMethods = PersonMethods.Undefined, CancellationToken cancellationToken = default)
        {
            RestRequest req = _client.Create("person/{personId}");
            req.AddUrlSegment("personId", personId.ToString());

            if (language != null)
                req.AddParameter("language", language);

            string appends = string.Join(",",
                                         Enum.GetValues(typeof(PersonMethods))
                                             .OfType<PersonMethods>()
                                             .Except(new[] { PersonMethods.Undefined })
                                             .Where(s => extraMethods.HasFlag(s))
                                             .Select(s => s.GetDescription()));

            if (appends != string.Empty)
                req.AddParameter("append_to_response", appends);

            // TODO: Dateformat?
            //req.DateFormat = "yyyy-MM-dd";

            using RestResponse<Person> response = await req.Get<Person>(cancellationToken).ConfigureAwait(false);

            if (!response.IsValid)
                return null;

            Person item = await response.GetDataObject().ConfigureAwait(false);

            // Patch up data, so that the end user won't notice that we share objects between request-types.
            if (item != null)
            {
                if (item.Images != null)
                    item.Images.Id = item.Id;

                if (item.TvCredits != null)
                    item.TvCredits.Id = item.Id;

                if (item.MovieCredits != null)
                    item.MovieCredits.Id = item.Id;
            }

            return item;
        }

        public async Task<ExternalIdsPerson> GetPersonExternalIdsAsync(int personId, CancellationToken cancellationToken = default)
        {
            return await GetPersonMethodInternal<ExternalIdsPerson>(personId, PersonMethods.ExternalIds, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<ProfileImages> GetPersonImagesAsync(int personId, CancellationToken cancellationToken = default)
        {
            return await GetPersonMethodInternal<ProfileImages>(personId, PersonMethods.Images, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<SearchContainer<PersonResult>> GetPersonListAsync(PersonListType type, int page = 0, CancellationToken cancellationToken = default)
        {
            RestRequest req;
            switch (type)
            {
                case PersonListType.Popular:
                    req = _client.Create("person/popular");
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }

            if (page >= 1)
                req.AddParameter("page", page.ToString());

            // TODO: Dateformat?
            //req.DateFormat = "yyyy-MM-dd";

            SearchContainer<PersonResult> resp = await req.GetOfT<SearchContainer<PersonResult>>(cancellationToken).ConfigureAwait(false);

            return resp;
        }

        public async Task<MovieCredits> GetPersonMovieCreditsAsync(int personId, CancellationToken cancellationToken = default)
        {
            return await GetPersonMovieCreditsAsync(personId, DefaultLanguage, cancellationToken).ConfigureAwait(false);
        }

        public async Task<MovieCredits> GetPersonMovieCreditsAsync(int personId, string language, CancellationToken cancellationToken = default)
        {
            return await GetPersonMethodInternal<MovieCredits>(personId, PersonMethods.MovieCredits, language: language, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<SearchContainerWithId<TaggedImage>> GetPersonTaggedImagesAsync(int personId, int page, CancellationToken cancellationToken = default)
        {
            return await GetPersonTaggedImagesAsync(personId, DefaultLanguage, page, cancellationToken).ConfigureAwait(false);
        }

        public async Task<SearchContainerWithId<TaggedImage>> GetPersonTaggedImagesAsync(int personId, string language, int page, CancellationToken cancellationToken = default)
        {
            return await GetPersonMethodInternal<SearchContainerWithId<TaggedImage>>(personId, PersonMethods.TaggedImages, language: language, page: page, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<TvCredits> GetPersonTvCreditsAsync(int personId, CancellationToken cancellationToken = default)
        {
            return await GetPersonTvCreditsAsync(personId, DefaultLanguage, cancellationToken).ConfigureAwait(false);
        }

        public async Task<TvCredits> GetPersonTvCreditsAsync(int personId, string language, CancellationToken cancellationToken = default)
        {
            return await GetPersonMethodInternal<TvCredits>(personId, PersonMethods.TvCredits, language: language, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}