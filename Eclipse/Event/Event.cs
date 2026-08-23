using Eclipse.Models;
using System;

namespace Eclipse.Event
{
    /// <summary>
    /// The four notifications the two settings windows send each other.
    ///
    /// These were Prism <c>PubSubEvent</c> types routed through an <c>EventAggregator</c>
    /// singleton. Four events and a command class were the entire Prism dependency, for a
    /// package the integration notes record as not resolving cleanly in the host load context.
    ///
    /// One behavioural difference worth knowing: Prism's <c>Subscribe</c> held its subscribers
    /// weakly, so a handler that was never detached was eventually collected. Plain events hold
    /// them strongly, which makes a missing <c>-=</c> a real leak rather than a silent one. That
    /// is the intended trade - both windows detach on Closed, and if one ever stops doing so the
    /// consequence is visible instead of hidden.
    /// </summary>
    public static class SettingsEvents
    {
        /// <summary>The settings window should close.</summary>
        public static event Action EclipseSettingsClose;

        /// <summary>
        /// The editor accepted an edit, carrying the edited copy for the settings window to take
        /// into its list. It used to carry only an id, because the editor had already written to
        /// disk and the settings window reloaded from there; nothing reaches disk until the
        /// settings window is saved now, so the object itself is the message.
        /// </summary>
        public static event Action<CustomListDefinition> CustomListDefinitionSaved;

        /// <summary>The custom list editor is closing, by whatever route.</summary>
        public static event Action CustomListDefinitionEditClosing;

        /// <summary>The custom list editor should close.</summary>
        public static event Action CustomListDefinitionEditClose;

        public static void RaiseEclipseSettingsClose()
        {
            EclipseSettingsClose?.Invoke();
        }

        public static void RaiseCustomListDefinitionSaved(CustomListDefinition customListDefinition)
        {
            CustomListDefinitionSaved?.Invoke(customListDefinition);
        }

        public static void RaiseCustomListDefinitionEditClosing()
        {
            CustomListDefinitionEditClosing?.Invoke();
        }

        public static void RaiseCustomListDefinitionEditClose()
        {
            CustomListDefinitionEditClose?.Invoke();
        }
    }
}
