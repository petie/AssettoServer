using AssettoServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyRatingPlugin
{
    public class SafetyRating
    {
        private readonly ACServer _server;
        private readonly SafetyRatingConfiguration _configuration;
        private static Dictionary<string, PlayerSafetyRating> _ratings = new Dictionary<string, PlayerSafetyRating>();

        internal string GetRatings(string userName, int currentTime)
        {
            if (userName == null)
            {
                StringBuilder sb = new StringBuilder();
                _ratings.Values.OrderBy(x => x.Name).ToList().ForEach(x =>
                {
                    sb.Append($"{x.Calculate(_configuration.EnvironmentMultiplier, _configuration.EnvironmentMultiplier, _configuration.PlayerMultiplier, currentTime, _configuration.Duration)}\t{x.Name}");
                });
                return sb.ToString();
            } else
            {
                var rating = _ratings[userName];
                return $"{rating.Calculate(_configuration.EnvironmentMultiplier, _configuration.EnvironmentMultiplier, _configuration.PlayerMultiplier, currentTime, _configuration.Duration)}\t{rating.Name}";
            }
        }

        public SafetyRating(ACServer server, SafetyRatingConfiguration configuration)
        {
            _server = server;
            _configuration = configuration;
            _server.ClientCollision += ClientCollided;
            _server.ClientDisconnected += ClientDisconnected;
            _server.ClientChecksumPassed += ClientConnected;
        }

        private void ClientConnected(AssettoServer.Network.Tcp.ACTcpClient sender, EventArgs args)
        {
            if (sender.Name != null && !sender.EntryCar.AiControlled)
            {
                var rating = _ratings[sender.Name];
                if (rating == null)
                {
                    rating = new PlayerSafetyRating(sender.Name, _configuration.BaseValue);
                    _ratings[sender.Name] = rating;
                }
            }
        }

        private void ClientDisconnected(AssettoServer.Network.Tcp.ACTcpClient sender, EventArgs args)
        {
            if (sender != null && sender.Name != null)
            {
                if (_ratings.TryGetValue(sender.Name, out var rating))
                {
                    _ratings.Remove(sender.Name);
                }
            }
        }

        private void ClientCollided(AssettoServer.Network.Tcp.ACTcpClient sender, CollisionEventArgs args)
        {
            if (sender != null && sender.Name != null)
            {
                var rating = _ratings[sender.Name];

                if (args.TargetCar == null)
                {
                    rating.AddEvent(SafetyEventType.Environment, sender.Server.CurrentTime);
                }
                else if (args.TargetCar != null && args.TargetCar.AiControlled)
                {
                    rating.AddEvent(SafetyEventType.Traffic, sender.Server.CurrentTime);
                }
                else
                {
                    rating.AddEvent(SafetyEventType.Player, sender.Server.CurrentTime);
                }

            }
        }
    }
}
