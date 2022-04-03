using AssettoServer.Network.Packets.Shared;
using AssettoServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RolePlayPlugin
{
    internal class JobManager
    {
        private readonly ACServer server;
        private readonly RolePlayConfiguration configuration;
        private readonly Dictionary<EntryCar, JobOffer?> _jobs;
        private List<JobOffer> availableJobs;

        public JobManager(ACServer server, RolePlayConfiguration configuration)
        {
            this.server = server;
            this.server.ClientCollision += OnClientCollision;
            this.configuration = configuration;
            _jobs = new Dictionary<EntryCar, JobOffer?>();
            foreach (EntryCar car in server.EntryCars)
            {
                _jobs.Add(car, null);
            }
            GenerateJobs();
        }

        private void GenerateJobs()
        {
            while (availableJobs.Count < configuration.MaxAvailableJobs)
            {
                availableJobs.Add(JobGenerator.GenerateJob());
            }
        }

        private void OnClientCollision(AssettoServer.Network.Tcp.ACTcpClient sender, CollisionEventArgs args)
        {
            if (_jobs.TryGetValue(sender.EntryCar, out var carJob) && carJob != null)
            {
                var damage = carJob.OnCollision(args.Speed);
                sender.SendPacket(new ChatMessage { SessionId = 255, Message = $"Cargo damaged: {damage}" });

            }
        }

        internal async Task<int?> EndJob(EntryCar car)
        {
            if (_jobs.TryGetValue(car, out var job) && job != null)
            {
                var distance = Vector3.Distance(job.Destination, car.Status.Position);
                if (distance <= 5)
                {
                    var prize = job.Finish();
                    bool bankSucceded = await RolePlayPlugin.BankManager.AddBalance(car, prize);
                    if (bankSucceded)
                        return prize;
                }
            }
            return null;
        }

        internal Task<bool> StartJob(EntryCar entryCar, string number)
        {
            if (_jobs.TryGetValue(entryCar, out var job) && job == null)
            {
                JobOffer? offer = availableJobs.FirstOrDefault(j => j.Id == number);
                if (offer != null) 
                {
                    availableJobs.Remove(offer);
                    _jobs[entryCar] = offer;
                    offer.UserInProgress = entryCar;
                    return Task.FromResult(true);
                }
            }
            return Task.FromResult(false);
        }

        internal Task<List<JobOffer>> GetJobsList()
        {
            return Task.FromResult(availableJobs);
        }
    }
}
