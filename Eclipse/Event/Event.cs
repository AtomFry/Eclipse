using Prism.Events;

namespace Eclipse.Event
{
    // events for eclipse settings views
    public class EclipseSettingsClose : PubSubEvent { }
    public class CustomListDefinitionSaved : PubSubEvent<string> { }
    public class CustomListDefinitionEditClosing : PubSubEvent { }
    public class CustomListDefinitionEditClose : PubSubEvent { }
}
