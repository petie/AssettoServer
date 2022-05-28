using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyRatingPlugin
{
    public class SafetyRatingConfiguration
    {
        public int BaseValue { get; init; } = 100;
        public int TrafficMultiplier { get; init; } = 2;
        public int PlayerMultiplier { get; init; } = 3;
        public int EnvironmentMultiplier { get; init; } = 1;
        public int Duration { get; init; } = 10;
    }
}
