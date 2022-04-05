using AssettoServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RolePlayPlugin
{
    internal class LeaderboardEntry
    {
        public JobDestination Start { get; set; }
        public JobDestination Finish { get; set; }
        public long Duration { get; set; }
        public EntryCar Player { get; set; }

        public LeaderboardEntry(JobOffer job)
        {
            Start = job.Start;
            Finish = job.Finish;
            Duration = job.JobFinishTime - job.JobStartTime;
            Player = job.UserInProgress;
        }

        public override string ToString()
        {
            return $"{Player.Client.Name}\t{TimeSpan.FromMilliseconds(Duration).ToString("mm:ss.fff")}";
        }
    }
}
