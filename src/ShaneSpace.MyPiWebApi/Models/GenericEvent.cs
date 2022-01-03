using System;

namespace ShaneSpace.MyPiWebApi.Models
{
    public class GenericEvent
    {
        public DateTimeOffset EventStart { get; set; }
        public DateTimeOffset? EventEnd { get; set; }
        public TimeSpan EventDuration => EventEnd != null
            ? EventStart - (DateTimeOffset)EventEnd
            : TimeSpan.Zero;
        public string Message { get; set; }
    }
}