using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyRatingPlugin
{
    public class SafetyRatingEvent
    {
        public SafetyRatingEvent(SafetyEventType type, long time)
        {
            Type = type;
            Time = time;
        }

        public SafetyEventType Type { get; set; }
        public long Time { get; set; }
    }

    public enum SafetyEventType
    {
        Environment,
        Traffic,
        Player
    }
}
