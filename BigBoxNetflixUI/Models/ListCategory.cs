using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigBoxNetflixUI.Models
{
    public enum ListCategoryType
    {
        Group=1,
        VoiceSearch=2,
        RandomGame=3
    }

    public class ListCategory
    {
        public string Name { get; set; }
        public int SortOrder { get; set; }
        public string ShortDescription { get; set; }
        public string LongDescription { get; set; }
        public ListCategoryType ListCategoryType{get; set;}


        private Uri categoryIcon;
        public Uri CategoryIcon
        {
            get
            {
                if (categoryIcon == null)
                {
                    string path = $"{Helpers.MediaFolder}\\Category\\{Name}.png";
                    // todo: set fallback image to local resource if not found
                    categoryIcon = new Uri(path);

                }
                return categoryIcon;
            }
        }
    }
}
