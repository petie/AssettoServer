using AssettoServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RolePlayPlugin
{
    internal class LeaderboardsManager
    {
        private readonly ACServer server;
        private readonly RolePlayConfiguration configuration;
        private Dictionary<Tuple<JobDestination, JobDestination>, List<LeaderboardEntry>> leaderboards;

        public LeaderboardsManager(ACServer server, RolePlayConfiguration configuration)
        {
            this.server = server;
            this.configuration = configuration;
            leaderboards = new Dictionary<Tuple<JobDestination, JobDestination>, List<LeaderboardEntry>>();
        }

        public bool StoreResult(JobOffer job)
        {
            LeaderboardEntry? entry = null;
            if (job.IsFinished)
            {
                entry = new LeaderboardEntry(job);
                if (leaderboards.TryGetValue(new Tuple<JobDestination, JobDestination>(job.Start, job.Finish), out var entries))
                {
                    entries.Add(entry);
                    entries.Sort((a, b) =>
                    {
                        if (a.Duration > b.Duration) return -1;
                        if (a.Duration == b.Duration) return 0;
                        return 1;
                    });
                    if (entries[0] == entry)
                        return true;
                    else return false;
                }
                else
                {
                    leaderboards.Add(new Tuple<JobDestination, JobDestination>(job.Start, job.Finish), new List<LeaderboardEntry> { entry });
                    return true;
                }
            }
            return false;
        }

        public Dictionary<Tuple<JobDestination, JobDestination>, List<LeaderboardEntry>>? GetForLocation(JobDestination start)
        {
            var result = new Dictionary<Tuple<JobDestination, JobDestination>, List<LeaderboardEntry>>();
            foreach (var key in leaderboards.Keys)
            {
                if (key.Item1 == start)
                {
                    result.Add(key, leaderboards[key].Take(10).ToList());
                }
            }
            return result;
        }

        public Dictionary<Tuple<JobDestination, JobDestination>, List<LeaderboardEntry>>? GetForLocation(EntryCar car)
        {
            var destination = RolePlayPlugin.JobManager.GetClosestDestination(car);
            if (destination != null)
            {
                return GetForLocation(destination);
            }
            return null;
        }

    }
}
