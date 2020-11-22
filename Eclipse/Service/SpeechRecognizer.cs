using Eclipse.Models;
using System;
using System.Collections.Generic;
using System.Linq;
// todo: fix create voice recognizer for 11.3 and later
using System.Speech.Recognition;
using System.Text;
using System.Threading.Tasks;

namespace Eclipse.Service
{
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
        private RecognitionCompletedDelegate RecognitionCompletedDelegate;
        private SpeechRecognizerResult SpeechRecognizerResult;

        private SpeechRecognitionEngine Recognizer { get; set; }

        public SpeechRecognizer(List<string> grammarPhrases, RecognitionCompletedDelegate recognitionCompletedDelegate)
        {
            RecognitionCompletedDelegate = recognitionCompletedDelegate;

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

        public void DoSpeechRecognition()
        {
            // reset the result
            SpeechRecognizerResult = new SpeechRecognizerResult();

            // kick off voice recognition 
            Recognizer.RecognizeAsync(RecognizeMode.Single);
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
