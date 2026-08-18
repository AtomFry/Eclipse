using Eclipse.Helpers;
using Eclipse.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Threading.Tasks;

namespace Eclipse.Service
{
    public class SpeechRecognizerService
    {
        private volatile VoiceSearchAvailability availability = VoiceSearchAvailability.Preparing;
        private SpeechRecognizer speechRecognizer;
        private string failureMessage;

        // Whether a voice search can be started right now.
        public VoiceSearchAvailability Availability => availability;

        // Why voice search is unavailable, when Availability is Failed.
        public string FailureMessage => failureMessage;

        // Build the phrase index and the recogniser away from the startup path, so the first
        // screen is not held up by work most sessions never use. Voice search reports itself
        // unavailable until this finishes.
        public void PrepareInBackground()
        {
            if (!EclipseSettingsDataProvider.Instance.EclipseSettings.EnableVoiceSearch)
            {
                availability = VoiceSearchAvailability.Disabled;
                return;
            }

            availability = VoiceSearchAvailability.Preparing;
            Task.Run(() => Prepare());
        }

        private void Prepare()
        {
            try
            {
                // the distinct set of phrases that can be used with voice recognition
                List<string> titleElements = VoiceSearchIndex.Instance.Phrases.ToList();

                speechRecognizer = new SpeechRecognizer(titleElements);

                // assign the recogniser before publishing the status - availability is
                // volatile, so a reader that sees Ready is guaranteed to see the recogniser
                availability = VoiceSearchAvailability.Ready;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "CreateRecognizer");

                // One attempt only. A recogniser that fails to initialise almost never
                // succeeds on a retry, so record why and let the user be told, rather than
                // retrying forever or failing silently.
                failureMessage = $"Voice search could not start: {ex.Message}";
                availability = VoiceSearchAvailability.Failed;
            }
        }

        // The recogniser, or null when voice search is not ready.
        public SpeechRecognizer GetRecognizer()
        {
            return availability == VoiceSearchAvailability.Ready ? speechRecognizer : null;
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

        // The recognition was cancelled rather than finishing - the user backed out, or a
        // cancel raised a completion event. There is nothing to search for, and the caller
        // must not treat it as a search that returned no games.
        public bool Cancelled { get; set; }
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
            // cancelling raises this event too, including the cancel the timeout below issues
            if (e?.Cancelled == true)
            {
                SpeechRecognizerResult.Cancelled = true;
            }

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
