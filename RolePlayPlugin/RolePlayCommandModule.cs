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
        //JobsRepository jr;
        public RolePlayCommandModule()
        {
            br = new BankRepository();
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
            List<JobOffer> result = await RolePlayPlugin.JobManager.GetJobsList();
            foreach (JobOffer offer in result)
            {
                Reply(offer.ToString());
            }
        }

        [Command("startJob")]
        public async Task StartJob(string number)
        {
            bool result = await RolePlayPlugin.JobManager.StartJob(Context.Client.EntryCar, number);
            if (result)
            {
                Reply("Job started. GO!");
            }
            else
            {
                Reply("Job not assigned");
            }
        }

        [Command("deliver")]
        public async Task Deliver()
        {
            int? result = await RolePlayPlugin.JobManager?.EndJob(Context.Client.EntryCar) ?? null;
            if (result != null)
            {
                Reply($"Job Succeded! ¥{result} has been deposited to your account.");
            } else
            {
                Reply($"Error, job could not be delivered");
            }
        }
    }
}
