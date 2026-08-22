using Eclipse.Models;
using System.Collections.Generic;
using System.Linq;

namespace Eclipse.Service
{
    // What building game lists needs from the game library, and nothing else.
    //
    // Deliberately narrow. This is not the LaunchBox adapter that B-11 describes; it is the
    // seam that lets GameListBuilder run without a live BigBox behind it, so that list
    // membership and ordering can be covered by tests.
    public interface IGameCatalogSource
    {
        // Every game that passes the broken/hidden filters, once each.
        IReadOnlyList<GameMatch> Games { get; }

        // Games grouped by the values they carry for a category. Category types that are not
        // browsable this way return an empty lookup rather than null.
        ILookup<string, GameMatch> ByCategory(ListCategoryType listCategoryType);
    }
}
