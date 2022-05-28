using AssettoServer.Server;
using AssettoServer.Server.Configuration;
using AssettoServer.Server.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyRatingPlugin
{
    public class SafetyRatingPlugin : IAssettoServerPlugin<SafetyRatingConfiguration>
    {
        internal static SafetyRating? Instance { get; private set; }

        private SafetyRatingConfiguration? _configuration;
        public void Initialize(ACServer server)
        {
            if (_configuration == null)
                throw new ConfigurationException("No configuration found for SafetyRatingPlugin");
            Instance = new SafetyRating(server, _configuration);
        }

        public void SetConfiguration(SafetyRatingConfiguration configuration)
        {
            _configuration = configuration;
        }
    }
}

