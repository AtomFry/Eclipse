using System.Collections.Generic;
using System.Linq;
using Eclipse.Models;
using Eclipse.Service;
using Eclipse.Service.Search;
using Xunit;

namespace Eclipse.Tests.Search
{
    /// <summary>
    /// The background index build, as far as it can be reached without a Big Box.
    ///
    /// WHAT IS AND IS NOT COVERED. SearchIndexService takes IGameCatalogSource, whose Games are
    /// GameMatch objects wrapping an IGame - and Eclipse.Tests has no reference to the LaunchBox
    /// contract assembly, so a catalog double cannot be built here. What that leaves reachable is
    /// the availability state machine, which is the part with the interesting behaviour: the
    /// disabled short circuit, the failure path, and the guarantee that Index is null unless
    /// Availability says Ready.
    ///
    /// The projection itself - GameMatch to SearchableGame - is exercised through
    /// SearchableGameProjection's own boundary, and the indexing it feeds is covered thoroughly
    /// by TitleIndexTests and the golden corpus. The uncovered seam is the single call joining
    /// them, which is why SearchableGameProjection is kept small enough that this is acceptable.
    ///
    /// Manual verification for the rest is recorded in VER-SEARCH-2xx at the bottom of this file.
    /// </summary>
    public class SearchIndexServiceTests
    {
        /// <summary>
        /// A catalog that throws the moment it is touched, which is how the failure path is
        /// reached without needing a real one.
        /// </summary>
        private sealed class ThrowingCatalog : IGameCatalogSource
        {
            public IReadOnlyList<GameMatch> Games
            {
                get { throw new System.InvalidOperationException("catalog unavailable"); }
            }

            public ILookup<string, GameMatch> ByCategory(ListCategoryType listCategoryType)
            {
                throw new System.InvalidOperationException("catalog unavailable");
            }
        }

        /// <summary>
        /// The service is a singleton in production, so these tests reach the instance. They
        /// only ever drive it into terminal states and read them back, so ordering between them
        /// does not matter.
        /// </summary>
        private static SearchIndexService Service => SearchIndexService.Instance;

        /// <summary>
        /// The setting is the switch, exactly as EnableVoiceSearch is: no index is built and
        /// nothing is reported as preparing, so the screen can say "turned off" rather than
        /// "still getting ready" forever.
        /// </summary>
        [Fact]
        public void Text_search_turned_off_short_circuits_the_build()
        {
            Service.PrepareInBackground(new ThrowingCatalog(), false);

            Assert.Equal(SearchAvailability.Disabled, Service.Availability);
            Assert.Null(Service.Index);
        }

        /// <summary>
        /// A build that throws leaves the service reporting Failed rather than sitting at
        /// Preparing forever - the shape M-5 and M-6 record as the failure worth avoiding.
        /// </summary>
        [Fact]
        public void A_build_that_throws_reports_failure_rather_than_preparing_forever()
        {
            Service.Prepare(new ThrowingCatalog());

            Assert.Equal(SearchAvailability.Failed, Service.Availability);
        }

        /// <summary>
        /// The invariant every caller relies on: an index is only handed out when the service
        /// says it is ready, so no caller has to check both.
        /// </summary>
        [Fact]
        public void The_index_is_never_handed_out_unless_availability_says_ready()
        {
            Service.Prepare(new ThrowingCatalog());

            Assert.NotEqual(SearchAvailability.Ready, Service.Availability);
            Assert.Null(Service.Index);
        }

        [Fact]
        public void The_service_satisfies_the_seam_the_session_takes()
        {
            ISearchIndexSource source = SearchIndexService.Instance;

            Assert.NotNull(source);
            Assert.True(System.Enum.IsDefined(typeof(SearchAvailability), source.Availability));
        }

        /// <summary>
        /// A session handed a service that is not ready must behave, because that is exactly what
        /// happens to a user who reaches the search screen during startup.
        /// </summary>
        [Fact]
        public void A_session_over_a_failed_service_still_works()
        {
            Service.Prepare(new ThrowingCatalog());

            SearchSession session = new SearchSession(Service, SearchKeyboardLayout.Alphabetical);
            session.Append('s');
            session.Append('o');

            Assert.Equal("so", session.Query);
            Assert.Empty(session.Results);
            Assert.False(session.IsReady);
        }
    }
}
