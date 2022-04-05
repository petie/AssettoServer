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
        public RolePlayCommandModule()
        {
        }

        [Command("balance")]
        public async Task GetBalance() {
            if (!string.IsNullOrEmpty(Context.Client.Guid)) {
                var balance = await RolePlayPlugin.BankManager.GetBalance(Context.Client.EntryCar);
                Reply($"Your balance is ¥{balance}");
            } else {
                Reply("Missing guid!");
            }
        }

        [Command("addBalance")]
        public async Task AddBalance(int value, string userGuid) {
            await RolePlayPlugin.BankManager.AddBalance(userGuid, value);
            Reply($"Added {value} from {userGuid}");
        }

        [Command("removeBalance")]
        public async Task RemoveBalance(int value, string userGuid) {
            await RolePlayPlugin.BankManager.RemoveBalance(value, userGuid);
            Reply($"Removed {value} from {userGuid}");
        }

        [Command("jobs")]
        public async Task GetJobs() {
            if (Context.Client.EntryCar != null)
            {
                List<JobOffer> result = await RolePlayPlugin.JobManager.GetJobsList(Context.Client.EntryCar);
                if (result == null) Reply("Too far away from PA");
                else
                {
                    foreach (JobOffer offer in result)
                    {
                        Reply(offer.ToString());
                    }
                }
            }
        }

        [Command("cancelJob")]
        public async Task CancelJob()
        {
            if (Context.Client.EntryCar != null)
            {
                bool result = await RolePlayPlugin.JobManager.CancelJob(Context.Client.EntryCar);
                if (result) Reply("Job Cancelled");
                else Reply("No job to cancel");
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
            var result = await RolePlayPlugin.JobManager?.EndJob(Context.Client.EntryCar) ?? null;
            if (result != null)
            {
                Reply(result.ToString());
            } else
            {
                Reply($"Error unknown");
            }
        }

        [Command("times")]
        public async Task GetTimes()
        {
            var result = RolePlayPlugin.LeaderboardsManager?.GetForLocation(Context.Client.EntryCar);
            if (result == null)
            {
                Reply("Too far away from destination!");
            }
            else
            {
                Reply(result.Convert());
            }
        }
    }
}
