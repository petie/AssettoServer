using AssettoServer.Commands;
using Qmmands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RolePlayPlugin
{
    public class RolePlayCommandModule : ACModuleBase
    {
        BankRepository br;
        JobsRepository jr;
        public RolePlayCommandModule()
        {
            br = new BankRepository();
            jr = new JobsRepository();
        }

        [Command("balance")]
        public void GetBalance() {
            if (!string.IsNullOrEmpty(Context.Client.Guid)) {
                var balance = br.GetBalance(Context.Client.Guid);
                Reply($"Your balance is ¥{balance}");
            } else {
                Reply("Missing guid!");
            }
        }

        [Command("addBalance")]
        public async Task AddBalance(int value, string userGuid) {
            await br.AddBalance(value, userGuid);
            Reply($"Added {value} from {userGuid}");
        }

        [Command("removeBalance")]
        public async Task RemoveBalance(int value, string userGuid) {
            await br.RemoveBalance(value, userGuid);
            Reply($"Removed {value} from {userGuid}");
        }

        [Command("jobs")]
        public async Task GetJobs() {
            List<JobOffer> result = await jr.GetJobsList();
        }
    }
}
