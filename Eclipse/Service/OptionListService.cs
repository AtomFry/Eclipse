using Eclipse.Models;
using System.Collections.Generic;

namespace Eclipse.Service
{

    public sealed class OptionListService
    {
        private bool optionListSet = false;
        private OptionList optionList;
        public OptionList OptionList
        {
            get
            {
                if (!optionListSet)
                {
                    optionListSet = true;
                    optionList = new OptionList(new List<Option<ListCategoryType>>
                    {
                        new Option<ListCategoryType> { Name = "Platform", EnumOption = ListCategoryType.Platform, SortOrder = 1, ShortDescription = "Platform", LongDescription = "Platform" },
                        new Option<ListCategoryType> { Name = "Genre", EnumOption = ListCategoryType.Genre, SortOrder = 2, ShortDescription = "Genre", LongDescription = "Genre" },
                        new Option<ListCategoryType> { Name = "Series", EnumOption = ListCategoryType.Series, SortOrder = 3, ShortDescription = "Series", LongDescription = "Series" },
                        new Option<ListCategoryType> { Name = "Playlist", EnumOption = ListCategoryType.Playlist, SortOrder = 4, ShortDescription = "Playlist", LongDescription = "Playlist" },
                        new Option<ListCategoryType> { Name = "Play mode", EnumOption = ListCategoryType.PlayMode, SortOrder = 5, ShortDescription = "Mode", LongDescription = "Play mode" },
                        new Option<ListCategoryType> { Name = "Developer", EnumOption = ListCategoryType.Developer, SortOrder = 6, ShortDescription = "Dev", LongDescription = "Developer" },
                        new Option<ListCategoryType> { Name = "Publisher", EnumOption = ListCategoryType.Publisher, SortOrder = 7, ShortDescription = "Pub", LongDescription = "Publisher" },
                        new Option<ListCategoryType> { Name = "Year", EnumOption = ListCategoryType.ReleaseYear, SortOrder = 8, ShortDescription = "Year", LongDescription = "Release year" },
                        new Option<ListCategoryType> { Name = "Random", EnumOption = ListCategoryType.RandomGame, SortOrder = 9, ShortDescription = "Random", LongDescription = "Random game" },
                        new Option<ListCategoryType> { Name = "Voice search", EnumOption = ListCategoryType.VoiceSearch, SortOrder = 10, ShortDescription = "Voice", LongDescription = "Voice search" }
                    });
                }
                return optionList;
            }
        }

        #region singleton implementation 
        public static OptionListService Instance => instance;

        private static readonly OptionListService instance = new OptionListService();

        static OptionListService()
        {
        }

        private OptionListService()
        {
        }
        #endregion
    }
}