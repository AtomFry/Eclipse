using Eclipse.Models;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;

namespace Eclipse.Service
{
    // Voice-search index, in its original shape: one GameMatch clone per recognisable
    // phrase, tagged with how well that phrase matches the game's title.
    //
    // The category clones this used to build now live in GameCatalog as an index rather
    // than as copies. What is left here is only the voice-search half, which is replaced by
    // a purpose-built index in the next stage.
    public sealed class GameBagService
    {
        private bool isSetup = false;
        private ConcurrentBag<GameMatch> gameBag;

        public ConcurrentBag<GameMatch> GameBag
        {
            get
            {
                if (!isSetup)
                {
                    Setup();
                }
                return gameBag;
            }
        }

        private void Setup()
        {
            gameBag = new ConcurrentBag<GameMatch>();

            if (!EclipseSettingsDataProvider.Instance.EclipseSettings.EnableVoiceSearch)
            {
                isSetup = true;
                return;
            }

            Parallel.ForEach(GameCatalog.Instance.Games, (gameMatch) =>
            {
                // create voice recognition grammar library for the game
                GameTitleGrammarBuilder gameTitleGrammarBuilder = new GameTitleGrammarBuilder(gameMatch.Game);
                foreach (GameTitleGrammar gameTitleGrammar in gameTitleGrammarBuilder.gameTitleGrammars)
                {
                    if (!string.IsNullOrWhiteSpace(gameTitleGrammar.Title))
                    {
                        gameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.VoiceSearch, gameTitleGrammar.Title, TitleMatchType.FullTitleMatch, gameTitleGrammar.Title));
                    }

                    if (!string.IsNullOrWhiteSpace(gameTitleGrammar.MainTitle))
                    {
                        gameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.VoiceSearch, gameTitleGrammar.MainTitle, TitleMatchType.MainTitleMatch, gameTitleGrammar.Title));
                    }

                    if (!string.IsNullOrWhiteSpace(gameTitleGrammar.Subtitle))
                    {
                        gameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.VoiceSearch, gameTitleGrammar.Subtitle, TitleMatchType.SubtitleMatch, gameTitleGrammar.Title));
                    }

                    for (int i = 0; i < gameTitleGrammar.TitleWords.Count; i++)
                    {
                        StringBuilder sb = new StringBuilder();
                        for (int j = i; j < gameTitleGrammar.TitleWords.Count; j++)
                        {
                            sb.Append($"{gameTitleGrammar.TitleWords[j]} ");
                            if (!GameTitleGrammar.IsNoiseWord(sb.ToString().Trim()))
                            {
                                gameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.VoiceSearch, sb.ToString().Trim(), TitleMatchType.FullTitleContains, gameTitleGrammar.Title));
                            }
                        }
                    }
                }
            });

            isSetup = true;
        }

        #region singleton implementation
        public static GameBagService Instance => instance;

        private static readonly GameBagService instance = new GameBagService();

        static GameBagService()
        {
        }

        private GameBagService()
        {
        }
        #endregion
    }
}
