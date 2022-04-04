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
        private Dictionary<EntryCar, int> balances;
        public BankManager(ACServer server, RolePlayConfiguration config)
        {
            this.server = server;
            this.config = config;
            balances= new Dictionary<EntryCar, int>();

        }

        internal async Task<int> GetBalance(EntryCar guid)
        {
            int result = 0;
            balances.TryGetValue(guid, out result);
            return result;
        }

        internal async Task<bool> AddBalance(EntryCar car, int prize)
        {
            if (balances.ContainsKey(car))
            {
                balances[car] += prize;

            } else
            {
                balances.Add(car, prize);
            }
            return true;
        }

        internal Task AddBalance(string userGuid, int value)
        {
            throw new NotImplementedException();
        }

        internal Task RemoveBalance(int value, string userGuid)
        {
            throw new NotImplementedException();
        }
    }
}
