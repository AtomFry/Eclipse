using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unbroken.LaunchBox.Plugins.Data;

namespace Eclipse.Models
{
    public class GameTitleGrammar
    {
        private static readonly string[] SpaceSplitter = new string[1] { " " };

        public string Title { get; set; }
        public string MainTitle { get; set; }
        public string Subtitle { get; set; }
        public List<string> TitleWords { get; set; }

        public GameTitleGrammar(string title)
        {
            Title = title;
            TitleWords = new List<string>();

            // find the index of the colon which indicates separation between title and subtitle
            int indexOfTitleSplit = Title.IndexOf(":");
            if (indexOfTitleSplit >= 1)
            {
                MainTitle = Title.Substring(0, indexOfTitleSplit).Trim();
                Subtitle = Title.Substring(indexOfTitleSplit + 1).Trim();
                ProcessTitle(MainTitle, TitleWords);
                ProcessTitle(Subtitle, TitleWords);
                Title = $"{MainTitle} {Subtitle}";
            }
            else
            {
                ProcessTitle(Title, TitleWords);
            }
        }

        public void ProcessTitle(string title, List<string> wordList)
        {
            // processing on title 
            title = RemoveUnwantedCharacters(title);

            // split the title into individual words
            string[] allTitleWords = title.Split(SpaceSplitter, StringSplitOptions.RemoveEmptyEntries);
            foreach (string word in allTitleWords)
            {
                // replace roman numerals 
                string wordRomanNumeralReplaced = GetRomanNumeralReplacement(word);
                if (!string.Equals(word, wordRomanNumeralReplaced, StringComparison.InvariantCultureIgnoreCase))
                {
                    // replace the roman numeral in the title
                    title = title.Replace(word, wordRomanNumeralReplaced);
                }

                // add the word to the title words
                wordList.Add(wordRomanNumeralReplaced);
            }
        }

        public static string RemoveUnwantedCharacters(string originalString)
        {
            if (string.IsNullOrWhiteSpace(originalString))
            {
                return string.Empty;
            }

            originalString = originalString.Replace(":", string.Empty);
            originalString = originalString.Replace("\"", string.Empty);
            originalString = originalString.Replace("!", string.Empty);

            return originalString;
        }

        public static string GetRomanNumeralReplacement(string str)
        {
            switch (str)
            {
                case "II":
                    return "2";
                case "III":
                    return "3";
                case "IV":
                    return "4";
                case "V":
                    return "5";
                case "VI":
                    return "6";
                case "VII":
                    return "7";
                case "VIII":
                    return "8";
                case "IX":
                    return "9";
                // there is a large majority of games with X in the title that are not roman numerals, sorry final fantasy X
                // case "X":
                //     return "10";
                case "XI":
                    return "11";
                case "XII":
                    return "12";
                case "XIII":
                    return "13";
                case "XIV":
                    return "14";
                case "XV":
                    return "15";
                case "XVI":
                    return "16";
                case "XVII":
                    return "17";
                case "XVIII":
                    return "18";
                case "XIX":
                    return "19";
                default:
                    return str;
            }
        }

        public static bool IsNoiseWord(string wLower)
        {
            if (string.Equals(wLower, "the", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "a", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "of", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "at", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "as", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "and", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "to", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "n'", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "'n", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "b", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "in", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(wLower, "on", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
            return false;
        }
    }

    // creates the components of the voice search grammar for each game
    public class GameTitleGrammarBuilder
    {
        private readonly string[] ForwardSlashSplitter = new string[1] { "/" };

        public List<GameTitleGrammar> gameTitleGrammars { get; set; }

        public GameTitleGrammarBuilder(IGame _game)
        {
            gameTitleGrammars = new List<GameTitleGrammar>();
            string[] gameTitles = _game.Title.Split(ForwardSlashSplitter, StringSplitOptions.RemoveEmptyEntries);
            foreach(string gameTitle in gameTitles)
            {
                gameTitleGrammars.Add(new GameTitleGrammar(gameTitle.Trim()));
            }
        }
    }
}
