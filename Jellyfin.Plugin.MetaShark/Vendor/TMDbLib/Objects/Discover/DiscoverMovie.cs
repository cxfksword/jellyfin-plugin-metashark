using System;
using System.Collections.Generic;
using System.Linq;
using TMDbLib.Objects.Companies;
using TMDbLib.Objects.General;
using TMDbLib.Client;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;
using TMDbLib.Utilities;

namespace TMDbLib.Objects.Discover
{
    public class DiscoverMovie : DiscoverBase<SearchMovie>
    {
        public DiscoverMovie(TMDbClient client)
            : base("discover/movie", client)
        {
        }

        private void ClearCertification()
        {
            Parameters.Remove("certification_country");
            Parameters.Remove("certification");
            Parameters.Remove("certification.lte");
            Parameters.Remove("certification.gte");
        }

        /// <summary>
        /// Toggle the inclusion of adult titles. Expected value is a boolean, true or false. Default is false.
        /// </summary>
        public DiscoverMovie IncludeAdultMovies(bool include = true)
        {
            Parameters["include_adult"] = include.ToString();
            return this;
        }

        /// <summary>
        /// Toggle the inclusion of items marked as a video. Expected value is a boolean, true or false. Default is true.
        /// </summary>
        public DiscoverMovie IncludeVideoMovies(bool include = true)
        {
            Parameters["include_video"] = include.ToString();
            return this;
        }

        /// <summary>
        /// Only include movies that have this person id added as a cast member. Expected value is an integer (the id of a person).
        /// This method performs an AND query.
        /// </summary>
        public DiscoverMovie IncludeWithAllOfCast(IEnumerable<int> castIds)
        {
            Parameters["with_cast"] = string.Join(",", castIds.Select(s => s.ToString()));
            return this;
        }

        /// <summary>
        /// Only include movies that have this person id added as a cast member.
        /// This method performs an AND query.
        /// </summary>
        public DiscoverMovie IncludeWithAllOfCast(IEnumerable<Cast> casts)
        {
            return IncludeWithAllOfCast(casts.Select(s => s.Id));
        }

        /// <summary>
        /// Only include movies that have this company id added as a crew member. Expected value is an integer (the id of a company).
        /// This method performs an AND query.
        /// </summary>
        public DiscoverMovie IncludeWithAllOfCompany(IEnumerable<int> companyIds)
        {
            Parameters["with_companies"] = string.Join(",", companyIds.Select(s => s.ToString()));
            return this;
        }

        /// <summary>
        /// Only include movies that have this company id added as a crew member.
        /// This method performs an AND query.
        /// </summary>
        public DiscoverMovie IncludeWithAllOfCompany(IEnumerable<Company> companies)
        {
            return IncludeWithAllOfCompany(companies.Select(s => s.Id));
        }

        /// <summary>
        /// Only include movies that have this person id added as a crew member. Expected value is an integer (the id of a person).
        /// This method performs an AND query.
        /// </summary>
        public DiscoverMovie IncludeWithAllOfCrew(IEnumerable<int> crewIds)
        {
            Parameters["with_crew"] = string.Join(",", crewIds.Select(s => s.ToString()));
            return this;
        }

        /// <summary>
        /// Only include movies that have this person id added as a crew member.
        /// This method performs an AND query.
        /// </summary>
        public DiscoverMovie IncludeWithAllOfCrew(IEnumerable<Crew> crews)
        {
            return IncludeWithAllOfCrew(crews.Select(s => s.Id));
        }

        /// <summary>
        /// Only include movies with the specified genres. Expected value is an integer (the id of a genre).
        /// This method performs an AND query.
        /// </summary>
        public DiscoverMovie IncludeWithAllOfGenre(IEnumerable<int> genreIds)
        {
            Parameters["with_genres"] = string.Join(",", genreIds.Select(s => s.ToString()));
            return this;
        }

        /// <summary>
        /// Only include movies with the specified genres.
        /// This method performs an AND query.
        /// </summary>
        public DiscoverMovie IncludeWithAllOfGenre(IEnumerable<Genre> genres)
        {
            return IncludeWithAllOfGenre(genres.Select(s => s.Id));
        }

        /// <summary>
        /// Only include movies with the specified keywords. Expected value is an integer (the id of a keyword).
        /// This method performs an AND query.
        /// </summary>
        public DiscoverMovie IncludeWithAllOfKeywords(IEnumerable<int> keywordIds)
        {
            Parameters["with_keywords"] = string.Join(",", keywordIds.Select(s => s.ToString()));
            return this;
        }

        /// <summary>
        /// Only include movies with the specified keywords.
        /// This method performs an AND query.
        /// </summary>
        public DiscoverMovie IncludeWithAllOfKeywords(IEnumerable<Genre> keywords)
        {
            return IncludeWithAllOfKeywords(keywords.Select(s => s.Id));
        }

        /// <summary>
        /// Only include movies that have these person id's added as a cast or crew member. Expected value is an integer (the id or ids of a person).
        /// This method performs an AND query.
        /// </summary>
        public DiscoverMovie IncludeWithAllOfPeople(IEnumerable<int> peopleIds)
        {
            Parameters["with_people"] = string.Join(",", peopleIds.Select(s => s.ToString()));
            return this;
        }

        /// <summary>
        /// Only include movies that have these person id's added as a cast or crew member.
        /// This method performs an AND query.
        /// </summary>
        public DiscoverMovie IncludeWithAllOfPeople(IEnumerable<Genre> people)
        {
            return IncludeWithAllOfPeople(people.Select(s => s.Id));
        }

        /// <summary>
        /// Only include movies that have this person id added as a cast member. Expected value is an integer (the id of a person).
        /// This method performs an OR query.
        /// </summary>
        public DiscoverMovie IncludeWithAnyOfCast(IEnumerable<int> castIds)
        {
            Parameters["with_cast"] = string.Join("|", castIds.Select(s => s.ToString()));
            return this;
        }

        /// <summary>
        /// Only include movies that have this person id added as a cast member.
        /// This method performs an OR query.
        /// </summary>
        public DiscoverMovie IncludeWithAnyOfCast(IEnumerable<Cast> casts)
        {
            return IncludeWithAnyOfCast(casts.Select(s => s.Id));
        }

        /// <summary>
        /// Only include movies that have this company id added as a crew member. Expected value is an integer (the id of a company).
        /// This method performs an OR query.
        /// </summary>
        public DiscoverMovie IncludeWithAnyOfCompany(IEnumerable<int> companyIds)
        {
            Parameters["with_companies"] = string.Join("|", companyIds.Select(s => s.ToString()));
            return this;
        }

        /// <summary>
        /// Only include movies that have this company id added as a crew member.
        /// This method performs an OR query.
        /// </summary>
        public DiscoverMovie IncludeWithAnyOfCompany(IEnumerable<Company> companies)
        {
            return IncludeWithAnyOfCompany(companies.Select(s => s.Id));
        }

        /// <summary>
        /// Only include movies that have this person id added as a crew member. Expected value is an integer (the id of a person).
        /// This method performs an OR query.
        /// </summary>
        public DiscoverMovie IncludeWithAnyOfCrew(IEnumerable<int> crewIds)
        {
            Parameters["with_crew"] = string.Join("|", crewIds.Select(s => s.ToString()));
            return this;
        }

        /// <summary>
        /// Only include movies that have this person id added as a crew member.
        /// This method performs an OR query.
        /// </summary>
        public DiscoverMovie IncludeWithAnyOfCrew(IEnumerable<Crew> crews)
        {
            return IncludeWithAnyOfCrew(crews.Select(s => s.Id));
        }

        /// <summary>
        /// Only include movies with the specified genres. Expected value is an integer (the id of a genre).
        /// This method performs an OR query.
        /// </summary>
        public DiscoverMovie IncludeWithAnyOfGenre(IEnumerable<int> castIds)
        {
            Parameters["with_genres"] = string.Join("|", castIds.Select(s => s.ToString()));
            return this;
        }

        /// <summary>
        /// Only include movies with the specified genres.
        /// This method performs an OR query.
        /// </summary>
        public DiscoverMovie IncludeWithAnyOfGenre(IEnumerable<Genre> genres)
        {
            return IncludeWithAnyOfGenre(genres.Select(s => s.Id));
        }

        /// <summary>
        /// Only include movies with the specified keywords. Expected value is an integer (the id of a keyword).
        /// This method performs an OR query.
        /// </summary>
        public DiscoverMovie IncludeWithAnyOfKeywords(IEnumerable<int> keywordIds)
        {
            Parameters["with_keywords"] = string.Join("|", keywordIds.Select(s => s.ToString()));
            return this;
        }

        /// <summary>
        /// Only include movies with the specified keywords.
        /// This method performs an OR query.
        /// </summary>
        public DiscoverMovie IncludeWithAnyOfKeywords(IEnumerable<Genre> keywords)
        {
            return IncludeWithAnyOfKeywords(keywords.Select(s => s.Id));
        }

        /// <summary>
        /// Only include movies that have these person id's added as a cast or crew member. Expected value is an integer (the id or ids of a person).
        /// This method performs an OR query.
        /// </summary>
        public DiscoverMovie IncludeWithAnyOfPeople(IEnumerable<int> peopleIds)
        {
            Parameters["with_people"] = string.Join("|", peopleIds.Select(s => s.ToString()));
            return this;
        }

        /// <summary>
        /// Only include movies that have these person id's added as a cast or crew member.
        /// This method performs an OR query.
        /// </summary>
        public DiscoverMovie IncludeWithAnyOfPeople(IEnumerable<Genre> people)
        {
            return IncludeWithAnyOfPeople(people.Select(s => s.Id));
        }

        /// <summary>
        /// Available options are: popularity.ascpopularity.descrelease_date.ascrelease_date.descrevenue.ascrevenue.descprimary_release_date.ascprimary_release_date.descoriginal_title.ascoriginal_title.descvote_average.ascvote_average.descvote_count.ascvote_count.desc
        /// </summary>
        public DiscoverMovie OrderBy(DiscoverMovieSortBy sortBy)
        {
            Parameters["sort_by"] = sortBy.GetDescription();
            return this;
        }

        /// <summary>
        /// Filter the results by all available release dates that have the specified value added as a year. Expected value is an integer (year).
        /// </summary>
        public DiscoverMovie WhereAnyReleaseDateIsInYear(int year)
        {
            Parameters["year"] = year.ToString("0000");
            return this;
        }

        /// <summary>
        /// Only include movies with this certification. Expected value is a valid certification for the specificed 'certification_country'.
        /// </summary>
        public DiscoverMovie WhereCertificationIs(string country, string certification)
        {
            ClearCertification();

            Parameters["certification_country"] = country;
            Parameters["certification"] = certification;

            return this;
        }

        /// <summary>
        /// Only include movies with this certification and lower. Expected value is a valid certification for the specificed 'certification_country'.
        /// </summary>
        public DiscoverMovie WhereCertificationIsAtMost(string country, string maxCertification)
        {
            ClearCertification();

            Parameters["certification_country"] = country;
            Parameters["certification.lte"] = maxCertification;

            return this;
        }

        /// <summary>
        /// Only include movies with this certification and higher. Expected value is a valid certification for the specificed 'certification_country'.
        /// </summary>
        public DiscoverMovie WhereCertificationIsAtLeast(string country, string minCertification)
        {
            ClearCertification();

            Parameters["certification_country"] = country;
            Parameters["certification.gte"] = minCertification;

            return this;
        }

        /// <summary>
        /// Filter by the primary release date and only include those which are greater than or equal to the specified value. Expected format is YYYY-MM-DD.
        /// </summary>
        public DiscoverMovie WherePrimaryReleaseDateIsAfter(DateTime date)
        {
            Parameters["primary_release_date.gte"] = date.ToString("yyyy-MM-dd");
            return this;
        }

        /// <summary>
        /// Filter by the primary release date and only include those which are greater than or equal to the specified value. Expected format is YYYY-MM-DD.
        /// </summary>
        public DiscoverMovie WherePrimaryReleaseDateIsBefore(DateTime date)
        {
            Parameters["primary_release_date.lte"] = date.ToString("yyyy-MM-dd");
            return this;
        }

        /// <summary>
        /// Filter the results so that only the primary release date year has this value. Expected value is a year.
        /// </summary>
        public DiscoverMovie WherePrimaryReleaseIsInYear(int year)
        {
            Parameters["primary_release_year"] = year.ToString("0000");
            return this;
        }

        /// <summary>
        /// Filter by all available release dates and only include those which are greater or equal to the specified value. Expected format is YYYY-MM-DD.
        /// </summary>
        public DiscoverMovie WhereReleaseDateIsAfter(DateTime date)
        {
            Parameters["release_date.gte"] = date.ToString("yyyy-MM-dd");
            return this;
        }

        /// <summary>
        /// Filter by all available release dates and only include those which are less or equal to the specified value. Expected format is YYYY-MM-DD.
        /// </summary>
        public DiscoverMovie WhereReleaseDateIsBefore(DateTime date)
        {
            Parameters["release_date.lte"] = date.ToString("yyyy-MM-dd");
            return this;
        }

        /// <summary>
        /// Filter movies by their vote average and only include those that have an average rating that is equal to or higher than the specified value. Expected value is a float.
        /// </summary>
        public DiscoverMovie WhereVoteAverageIsAtLeast(double score)
        {
            // TODO: Apply culture to the ToString
            Parameters["vote_average.gte"] = score.ToString();
            return this;
        }

        /// <summary>
        /// Filter movies by their vote average and only include those that have an average rating that is equal to or lower than the specified value. Expected value is a float.
        /// </summary>
        public DiscoverMovie WhereVoteAverageIsAtMost(double score)
        {
            // TODO: Apply culture to the ToString
            Parameters["vote_average.lte"] = score.ToString();
            return this;
        }

        /// <summary>
        /// Filter movies by their vote count and only include movies that have a vote count that is equal to or lower than the specified value.
        /// </summary>
        public DiscoverMovie WhereVoteCountIsAtLeast(int count)
        {
            Parameters["vote_count.gte"] = count.ToString();
            return this;
        }

        /// <summary>
        /// Filter movies by their vote count and only include movies that have a vote count that is equal to or lower than the specified value. Expected value is an integer.
        /// </summary>
        public DiscoverMovie WhereVoteCountIsAtMost(int count)
        {
            Parameters["vote_count.lte"] = count.ToString();
            return this;
        }

        /// <summary>
        /// Specifies which region to use for release date filtering (using ISO 3166-1 code)
        /// </summary>
        public DiscoverMovie WhereReleaseDateIsInRegion(string region)
        {
            Parameters["region"] = region;
            return this;
        }

        /// <summary>
        /// Specifies which language to use for translatable fields
        /// </summary>
        public DiscoverMovie WhereLanguageIs(string language)
        {
            Parameters["language"] = language;
            return this;
        }

        /// <summary>
        /// Specifies which language to use for translatable fields
        /// </summary>
        public DiscoverMovie WhereOriginalLanguageIs(string language)
        {
            Parameters["with_original_language"] = language;
            return this;
        }
    }
}
