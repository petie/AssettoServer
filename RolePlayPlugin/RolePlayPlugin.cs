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
        internal static JobManager JobManager { get; private set; }
        internal static CarManager CarManager { get; private set; }
        internal static BankManager BankManager { get; private set; }
        private RolePlayConfiguration? _config;
        Dictionary<EntryCar, JobOffer?> _jobs;

        public void Initialize(ACServer server)
        {
            JobManager = new JobManager(server, _config);
            BankManager = new BankManager(server, _config);
            CarManager = new CarManager(server, _config);
            Log.Debug("Role Play Plugin Initialized!"); 
        }

        public void SetConfiguration(RolePlayConfiguration configuration)
        {
            _config = configuration;
        }
    }
}
