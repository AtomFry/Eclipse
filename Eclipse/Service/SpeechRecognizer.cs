using Eclipse.Helpers;
using Eclipse.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;

namespace Eclipse.Service
{
    public class SpeechRecognizerService
    {
        private bool isSetup;
        private SpeechRecognizer speechRecognizer;        

        public SpeechRecognizer GetRecognizer()
        {
            if (!isSetup)
            {
                isSetup = true;

                try
                {
                    ConcurrentBag<GameMatch> gameBag = GameBagService.Instance.GameBag;

                    // get the distinct set of phrases that can be used with voice recognition
                    List<string> titleElements = gameBag.Where(game => game.CategoryType == ListCategoryType.VoiceSearch)
                        .GroupBy(game => game.CategoryValue)
                        .Distinct()
                        .Select(gameMatch => gameMatch.Key)
                        .ToList();

                    speechRecognizer = new SpeechRecognizer(titleElements);
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex, "CreateRecognizer");
                }
            }

            return speechRecognizer;            
        }

        #region singleton implementation 
        public static SpeechRecognizerService Instance => instance;

        private static readonly SpeechRecognizerService instance = new SpeechRecognizerService();

        static SpeechRecognizerService()
        {
        }

        private SpeechRecognizerService()
        {
        }
        #endregion
    }

    public class RecognizedPhrase
    {
        public string Phrase { get; set; }
        public float Confidence { get; set; }
    }

    public class SpeechRecognizerResult
    {
        public List<RecognizedPhrase> RecognizedPhrases { get; set; } = new List<RecognizedPhrase>();
        public string ErrorMessage { get; set; }
    }

    public delegate void RecognitionCompletedDelegate(SpeechRecognizerResult speechRecognizerResult);

    public class SpeechRecognizer
    {
        public RecognitionCompletedDelegate RecognitionCompletedDelegate { get; set; }
        private SpeechRecognizerResult SpeechRecognizerResult;

        private SpeechRecognitionEngine Recognizer { get; set; }

        public SpeechRecognizer(List<string> grammarPhrases)
        {
            // add the distinct phrases to the list of choices
            Choices choices = new Choices();
            choices.Add(grammarPhrases.ToArray());

            GrammarBuilder grammarBuilder = new GrammarBuilder();
            grammarBuilder.Append(choices);

            Grammar grammar = new Grammar(grammarBuilder)
            {
                Name = "Game title elements"
            };

            // setup the recognizer
            Recognizer = new SpeechRecognitionEngine();
            Recognizer.InitialSilenceTimeout = TimeSpan.FromSeconds(5.0);
            Recognizer.RecognizeCompleted += new EventHandler<RecognizeCompletedEventArgs>(RecognizeCompleted);
            Recognizer.LoadGrammarAsync(grammar);
            Recognizer.SpeechHypothesized += new EventHandler<SpeechHypothesizedEventArgs>(SpeechHypothesized);
            Recognizer.SetInputToDefaultAudioDevice();
            Recognizer.RecognizeAsyncCancel();
        }

        public void DoSpeechRecognition(RecognitionCompletedDelegate recognitionCompletedDelegate)
        {
            RecognitionCompletedDelegate = recognitionCompletedDelegate;

            // reset the result
            SpeechRecognizerResult = new SpeechRecognizerResult();

            // kick off voice recognition 
            Recognizer.RecognizeAsync(RecognizeMode.Single);
        }

        public void TryCancelRecognition()
        {
            try
            {
                Recognizer.RecognizeAsyncCancel();
            }
            finally
            {
                // intentionally left blank
            }
        }

        void SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            // ignore noise words 
            if (GameTitleGrammar.IsNoiseWord(e.Result.Text))
            {
                return;
            }

            // add the phrase to the 
            SpeechRecognizerResult.RecognizedPhrases.Add(new RecognizedPhrase()
            {
                Confidence = e.Result.Confidence,
                Phrase = e.Result.Text
            });
        }

        void RecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            // save any error 
            if (e?.Error != null)
            {
                if (Recognizer != null)
                {
                    Recognizer.RecognizeAsyncCancel();
                }
                SpeechRecognizerResult.ErrorMessage = e.Error.Message;
            }

            // indicate time out error 
            if (e?.InitialSilenceTimeout == true || e?.BabbleTimeout == true)
            {
                if (Recognizer != null)
                {
                    Recognizer.RecognizeAsyncCancel();
                }

                SpeechRecognizerResult.ErrorMessage = "Voice recognition could not hear anything, please try again";
            }

            // trigger delegate to pass results to the caller
            RecognitionCompletedDelegate(SpeechRecognizerResult);
        }
    }
}
