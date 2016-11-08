using System.Collections.Generic;
using System.Diagnostics;
using SenseNet.Diagnostics;

namespace SenseNet.Client.Tests
{
    public class EventLoggerForTests : IEventLogger
    {
        public List<EventLogEntryForTests> Entries { get; } = new List<EventLogEntryForTests>();

        public void Write(object message, ICollection<string> categories, int priority, int eventId, TraceEventType severity, string title,
            IDictionary<string, object> properties)
        {
            Entries.Add(new EventLogEntryForTests(message, categories, priority, eventId, severity, title, properties));
        }

    }
}
