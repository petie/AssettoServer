using AssettoServer.Server;
using AssettoServer.Server.Plugin;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RolePlayPlugin
{
    public class RolePlayPlugin : IAssettoServerPlugin<RolePlayConfiguration>
    {
        private RolePlayConfiguration? _config;

        public void Initialize(ACServer server)
        {
            Log.Debug("Role Play Plugin Initialized!");
        }

        public void SetConfiguration(RolePlayConfiguration configuration)
        {
            _config = configuration;
        }
    }
}
