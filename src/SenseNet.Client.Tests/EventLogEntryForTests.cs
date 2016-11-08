using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Client.Tests
{
    public class EventLogEntryForTests
    {
        public object Message { get; }
        public ICollection<string> Categories { get; }
        public int Priority { get; }
        public int EventId { get; }
        public TraceEventType Severity { get; }
        public string Title { get; }
        public IDictionary<string, object> Properties { get; }

        public EventLogEntryForTests(object message, ICollection<string> categories, int priority, int eventId, TraceEventType severity, string title,
            IDictionary<string, object> properties)
        {
            Message = message;
            Categories = categories;
            Priority = priority;
            EventId = eventId;
            Severity = severity;
            Title = title;
            Properties = properties;
        }

        public override string ToString()
        {
            return JsonHelper.Serialize(this);
        }
    }
}
