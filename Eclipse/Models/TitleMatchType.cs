using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigBoxNetflixUI.Models
{
    public enum TitleMatchType
    {
        FullTitleMatch = 0,
        MainTitleMatch = 1,
        SubtitleMatch = 2,
        FullTitleContains = 3,
        None = 100
    }
}
