using AssettoServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RolePlayPlugin
{
    /// <summary>
    /// Responsible for keeping track of everyone's bank account balance
    /// </summary>
    internal class BankManager
    {
        private ACServer server;
        private RolePlayConfiguration config;

        public BankManager(ACServer server, RolePlayConfiguration config)
        {
            this.server = server;
            this.config = config;
        }

        internal async Task<bool> AddBalance(EntryCar car, object prize)
        {
            throw new NotImplementedException();
        }
    }
}
