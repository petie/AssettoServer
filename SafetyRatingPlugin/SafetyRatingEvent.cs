using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyRatingPlugin
{
    public class SafetyRatingEvent
    {
        public SafetyRatingEvent(SafetyEventType type, int time)
        {
            Type = type;
            Time = time;
        }

        public SafetyEventType Type { get; set; }
        public int Time { get; set; }
    }

    public enum SafetyEventType
    {
        Environment,
        Traffic,
        Player
    }
}
