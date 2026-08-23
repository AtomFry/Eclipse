using System.Windows.Threading;

namespace Eclipse.Service
{
    /// <summary>Called once per session, on the dispatcher the session was started with.</summary>
    public delegate void RecognitionCompletedDelegate(SpeechRecognizerResult speechRecognizerResult);

    /// <summary>
    /// Called on the dispatcher each time the recogniser hypothesises a phrase, so the screen can
    /// echo what is being heard while the user is still speaking. A best guess so far, not a
    /// result - it changes as the utterance goes on, and it may be wrong.
    /// </summary>
    public delegate void PhraseHeardDelegate(string phrase);

    /// <summary>
    /// One voice search attempt.
    ///
    /// Recognition is the only part of voice search that is asynchronous and the only part that
    /// touches a device. This interface is the boundary between it and everything downstream -
    /// matching, scoring, ranking, presentation - which is ordinary synchronous UI work.
    ///
    /// Two guarantees, and they are the whole point of the type:
    ///
    /// * The completion callback is raised <b>at most once</b>, and never after <see cref="Cancel"/>.
    ///   The engine raises a completion for a cancel too, and the state machine used to act on it -
    ///   so escaping out of a search transitioned back to browsing twice, and if the user had moved
    ///   on in between, the late completion dragged them back.
    /// * Both callbacks arrive <b>on the dispatcher</b> the session was started with. The engine
    ///   raises its events on a thread pool thread; the boundary is crossed here, once, rather
    ///   than being left to each leaf downstream to notice.
    /// </summary>
    public interface ISpeechSession
    {
        /// <summary>
        /// Stops listening and gives up on the result. The completion callback will not be raised.
        /// Safe to call more than once, and safe to call after the session has already completed.
        /// </summary>
        void Cancel();
    }

    /// <summary>
    /// Hands out recognition attempts. The engine and its grammar behind this are shared and
    /// expensive to build; a session is not.
    /// </summary>
    public interface ISpeechSessionSource
    {
        /// <summary>
        /// Begins listening for a single utterance.
        /// </summary>
        /// <param name="dispatcher">The thread both callbacks are delivered on.</param>
        /// <param name="onCompleted">Raised at most once, when recognition finishes.</param>
        /// <param name="onPhraseHeard">Raised as phrases are hypothesised, or null if the caller
        /// does not want to echo them.</param>
        ISpeechSession StartSession(Dispatcher dispatcher,
                                    RecognitionCompletedDelegate onCompleted,
                                    PhraseHeardDelegate onPhraseHeard);
    }
}
