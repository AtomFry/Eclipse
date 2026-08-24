using Eclipse.Helpers;
using Eclipse.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

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
                StartupPerformanceMonitor startupMonitor = StartupPerformanceMonitor.Instance;

                // the distinct set of phrases that can be used with voice recognition
                List<string> titleElements;
                using (startupMonitor.Phase("voice search: build phrase index"))
                {
                    titleElements = VoiceSearchIndex.Instance.Phrases.ToList();
                }

                // Building the SAPI grammar is the expensive half and it grows with the library,
                // so it is measured apart from the index it is built from.
                using (startupMonitor.Phase($"voice search: build recogniser from {titleElements.Count} phrases"))
                {
                    speechRecognizer = new SpeechRecognizer(titleElements);
                }

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
                //
                // The reason the user sees is deliberately not the exception's message - that
                // is a SAPI string aimed at a developer, and this is read off a television. The
                // detail is in the log above.
                failureMessage = "Voice search could not start on this PC";
                availability = VoiceSearchAvailability.Failed;
            }
        }

        // Where voice searches come from, or null when voice search is not ready.
        public ISpeechSessionSource GetSessionSource()
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

        // What happened, as something the caller can branch on. Deliberately not a message: what
        // the user is told is a presentation decision, and it belongs with the rest of the copy
        // in VoiceRecognitionState rather than being composed down here.
        public SpeechRecognitionOutcome Outcome { get; set; } = SpeechRecognitionOutcome.Completed;
    }

    /// <summary>
    /// The speech engine and its grammar, and the source of the one-shot sessions that use them.
    ///
    /// The engine is expensive to build - the grammar holds every phrase in the library - so it
    /// is created once and shared for the life of the process. What is <i>not</i> shared is the
    /// attempt. Every engine event is attributed to the session that is listening at the time,
    /// and a session that has been cancelled or superseded is dropped rather than delivered.
    ///
    /// Before this, the recogniser held one mutable callback and one mutable result, replaced on
    /// each search and cleared on neither. Nothing distinguished the completion for the search
    /// the user was waiting on from the completion for one they had escaped out of two seconds
    /// earlier, and a late hypothesis landed in the next search's results.
    /// </summary>
    public class SpeechRecognizer : ISpeechSessionSource
    {
        private readonly SpeechRecognitionEngine recognizer;

        // The attempt currently listening, or null. The engine raises its events on a thread
        // pool thread while the UI thread starts and cancels sessions, so this is genuinely
        // cross-thread state and every access takes the lock.
        private readonly object sessionLock = new object();
        private Session activeSession;

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
            recognizer = new SpeechRecognitionEngine();
            recognizer.InitialSilenceTimeout = TimeSpan.FromSeconds(5.0);
            recognizer.RecognizeCompleted += new EventHandler<RecognizeCompletedEventArgs>(RecognizeCompleted);
            recognizer.SpeechHypothesized += new EventHandler<SpeechHypothesizedEventArgs>(SpeechHypothesized);

            // Load the grammar synchronously. This constructor already runs on a background
            // thread, so there is nothing here to keep responsive - and the async load returned
            // before the grammar was in place, which let the service publish itself as Ready
            // while the recogniser still had nothing to listen for. A search started in that
            // window heard nothing and the user was told no games matched.
            recognizer.LoadGrammar(grammar);

            recognizer.SetInputToDefaultAudioDevice();
        }

        public ISpeechSession StartSession(Dispatcher dispatcher,
                                           RecognitionCompletedDelegate onCompleted,
                                           PhraseHeardDelegate onPhraseHeard)
        {
            if (dispatcher == null)
            {
                throw new ArgumentNullException(nameof(dispatcher));
            }

            if (onCompleted == null)
            {
                throw new ArgumentNullException(nameof(onCompleted));
            }

            Session session = new Session(this, dispatcher, onCompleted, onPhraseHeard);

            lock (sessionLock)
            {
                // An attempt still in flight cannot be the one the user is waiting for. Drop it
                // rather than leaving it to call back later. Nothing reaches this today - the
                // state machine will not start a second search while one is running - but the
                // guarantee this type makes should not depend on that staying true.
                activeSession?.Abandon();
                activeSession = session;
            }

            try
            {
                recognizer.RecognizeAsync(RecognizeMode.Single);
            }
            catch
            {
                // The caller reports the failure; this only has to leave nothing listening.
                AbandonSession(session);
                throw;
            }

            return session;
        }

        // Stop listening. Failing to stop is not something the caller can act on - it is already
        // on its way out of voice search - but the exception has to stop here.
        private void TryCancel()
        {
            try
            {
                recognizer.RecognizeAsyncCancel();
            }
            catch (Exception ex)
            {
                // Cancelling a recogniser that is not listening, or one whose audio device has
                // gone away, throws. The guard here used to be a finally with an empty body,
                // which swallows nothing, so this escaped through OnEscape into Big Box's key
                // handler.
                LogHelper.LogException(ex, "TryCancelRecognition");
            }
        }

        private void AbandonSession(Session session)
        {
            lock (sessionLock)
            {
                if (activeSession == session)
                {
                    activeSession = null;
                }
            }

            session.Abandon();
        }

        private Session CurrentSession()
        {
            lock (sessionLock)
            {
                return activeSession;
            }
        }

        // The listening session, handed over so nothing else can complete it. A second
        // completion for the same attempt - the cancel below raises one - finds nothing here.
        private Session TakeCurrentSession()
        {
            lock (sessionLock)
            {
                Session session = activeSession;
                activeSession = null;
                return session;
            }
        }

        private void SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            // ignore noise words
            if (GameTitleGrammar.IsNoiseWord(e.Result.Text))
            {
                return;
            }

            // A hypothesis raised after the attempt it belongs to has finished has nowhere to go.
            // It used to land in whichever result object was current, which meant the next
            // search's.
            CurrentSession()?.AddPhrase(e.Result.Text, e.Result.Confidence);
        }

        private void RecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            Session session = TakeCurrentSession();

            // Cancelling raises this event too, and so does the cancel issued below - and an
            // attempt the caller has given up on has nobody waiting for it.
            session?.Complete(e);
        }

        /// <summary>
        /// One recognition attempt: the result being accumulated, and the promise of exactly one
        /// callback on the caller's thread.
        /// </summary>
        private sealed class Session : ISpeechSession
        {
            private readonly SpeechRecognizer owner;
            private readonly Dispatcher dispatcher;
            private readonly SpeechRecognizerResult result = new SpeechRecognizerResult();
            private readonly RecognitionCompletedDelegate onCompleted;
            private readonly PhraseHeardDelegate onPhraseHeard;

            // 0 while the callback is still owed, 1 once it has been handed over or given up on.
            private int settled;

            // Set when the caller gives up. Checked again at delivery, not only when the engine
            // raises the event: a cancel can land in between, because the engine raises on a
            // thread pool thread and delivery is a post back to the UI one.
            private volatile bool abandoned;

            internal Session(SpeechRecognizer owner,
                             Dispatcher dispatcher,
                             RecognitionCompletedDelegate onCompleted,
                             PhraseHeardDelegate onPhraseHeard)
            {
                this.owner = owner;
                this.dispatcher = dispatcher;
                this.onCompleted = onCompleted;
                this.onPhraseHeard = onPhraseHeard;
            }

            public void Cancel()
            {
                owner.AbandonSession(this);
                owner.TryCancel();
            }

            internal void AddPhrase(string phrase, float confidence)
            {
                if (abandoned)
                {
                    return;
                }

                lock (result)
                {
                    result.RecognizedPhrases.Add(new RecognizedPhrase
                    {
                        Confidence = confidence,
                        Phrase = phrase
                    });
                }

                PhraseHeardDelegate heard = onPhraseHeard;
                if (heard != null)
                {
                    OnDispatcher(() =>
                    {
                        if (!abandoned)
                        {
                            heard(phrase);
                        }
                    });
                }
            }

            // Give up on this attempt without calling back.
            internal void Abandon()
            {
                abandoned = true;
                Interlocked.Exchange(ref settled, 1);
            }

            internal void Complete(RecognizeCompletedEventArgs e)
            {
                if (Interlocked.Exchange(ref settled, 1) != 0)
                {
                    return;
                }

                if (abandoned)
                {
                    return;
                }

                // Assigned in order of increasing precedence, which is the precedence the three
                // sequential checks this replaced already had: a timeout beats an error, and an
                // error beats a cancel.

                // cancelling raises this event too, including the cancel below
                if (e?.Cancelled == true)
                {
                    result.Outcome = SpeechRecognitionOutcome.Cancelled;
                }

                // save any error
                if (e?.Error != null)
                {
                    owner.TryCancel();

                    // The SAPI message is for the log, not for someone reading a television.
                    LogHelper.LogException(e.Error, "recognize speech");
                    result.Outcome = SpeechRecognitionOutcome.Failed;
                }

                // nothing said, or nothing that could be made out
                if (e?.InitialSilenceTimeout == true || e?.BabbleTimeout == true)
                {
                    owner.TryCancel();

                    result.Outcome = SpeechRecognitionOutcome.HeardNothing;
                }

                // Everything past here - matching, scoring, ranking, presentation - is ordinary
                // single-threaded UI work, and the engine raised this on a thread pool thread.
                // The boundary between the two is crossed exactly here, once, rather than being
                // left to each leaf downstream to marshal for itself.
                OnDispatcher(() =>
                {
                    if (!abandoned)
                    {
                        onCompleted(result);
                    }
                });
            }

            private void OnDispatcher(Action action)
            {
                if (dispatcher.CheckAccess())
                {
                    action();
                }
                else
                {
                    dispatcher.InvokeAsync(action);
                }
            }
        }
    }
}
