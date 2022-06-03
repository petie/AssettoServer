using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyRatingPlugin
{
    public class PlayerSafetyRating
    {
        public string Name { get; set; }
        public List<SafetyRatingEvent> Events { get; set; }
        private int _baseValue;

        public PlayerSafetyRating(string name, int baseValue)
        {
            Name = name;
            _baseValue = baseValue;
            Events = new List<SafetyRatingEvent>();
        }

        public void AddEvent(SafetyEventType type, long time)
        {
            Events.Add(new SafetyRatingEvent(type, time));
        }

        public int Calculate(int envMulti, int trafficMulti, int playerMulti, long currentTime, int countTime)
        {
            Events.RemoveAll(e =>
            {
                return currentTime - e.Time >= countTime * 60000;
            });
            int sumDeductions = 0;
            foreach (var e in Events)
            {
                switch (e.Type)
                {
                    case SafetyEventType.Environment:
                        sumDeductions += envMulti;
                        break;
                    case SafetyEventType.Player:
                        sumDeductions += playerMulti;
                        break;
                    case SafetyEventType.Traffic:
                        sumDeductions += trafficMulti;
                        break;
                }
            }
            return _baseValue - sumDeductions;
        }
    }
}
