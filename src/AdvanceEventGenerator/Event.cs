using System;
using System.Collections.Generic;

namespace AdvanceEventGenerator
{
    public enum EventType
    {
        Plain,
        CommandResponse,
        StateChange
    }

    public class Event
    {
        public Guid MessageId { get; set; }
        public Guid CorrelationId { get; set; }
        public Guid? PreviousMessageId { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
        public string Entity { get; set; }
        public string EventName { get; set; }
        public bool DiagEvent { get; set; }
        public EventType EventType { get; set; }
        public Dictionary<string,string> Contents { get; set; } = new Dictionary<string, string>();

        public override string ToString()
        {
            return $"{Entity}:{EventName} ({EventType})";
        }
    }
}
