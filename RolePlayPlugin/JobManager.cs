using AssettoServer.Network.Packets.Shared;
using AssettoServer.Server;
using Serilog;
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
        private Dictionary<JobDestination, List<JobOffer>> availableJobs;
        //private List<JobOffer> availableJobs;

        public JobManager(ACServer server, RolePlayConfiguration configuration)
        {
            this.server = server;
            this.server.ClientCollision += OnClientCollision;
            this.configuration = configuration;
            InitializeAvailableJobs();
            _jobs = new Dictionary<EntryCar, JobOffer?>();
            foreach (EntryCar car in server.EntryCars)
            {
                _jobs.Add(car, null);
            }
            GenerateJobs();
            Log.Debug("Job Manager initialized!");
        }

        private void InitializeAvailableJobs()
        {
            availableJobs = new Dictionary<JobDestination, List<JobOffer>>();
            foreach (JobDestination jobDestination in configuration.JobDestinations)
            {
                availableJobs.Add(jobDestination, new List<JobOffer>());
            }
        }

        private void GenerateJobs()
        {
            int count = 0;
            do
            {
                count = DictListCount();
                var newJob = JobGenerator.GenerateJob(configuration.JobDestinations);
                if (availableJobs.TryGetValue(newJob.Start, out var jobOffers))
                {
                    Log.Debug($"Generated job: {newJob}");
                    jobOffers.Add(newJob);
                }
            } while (count < configuration.MaxAvailableJobs);
        }

        internal Task<bool> CancelJob(EntryCar entryCar)
        {
            if (_jobs[entryCar] != null)
            {
                _jobs[entryCar] = null;
                return Task.FromResult(true);
            } else
            {
                return Task.FromResult(false);
            }
        }

        private int DictListCount()
        {
            int count = 0;
            foreach (var item in availableJobs)
            {
                count += item.Value?.Count ?? 0;
            }
            return count;
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
                var distance = Vector3.Distance(job.Finish.Position, car.Status.Position);
                if (distance <= configuration.JobDistance)
                {
                    var prize = job.Complete();
                    bool bankSucceded = await RolePlayPlugin.BankManager.AddBalance(car, prize);
                    if (bankSucceded)
                        return prize;
                }
            }
            return null;
        }

        private JobDestination? GetClosestDestination(EntryCar car)
        {
            JobDestination? current = null;
            foreach (JobDestination destination in configuration.JobDestinations)
            {
                if (Vector3.Distance(destination.Position, car.Status.Position) < configuration.JobDistance)
                {
                    current = destination;
                    break;
                }
            }
            return current;
        }

        internal Task<bool> StartJob(EntryCar entryCar, string number)
        {
            
            if (_jobs.TryGetValue(entryCar, out var job) && job == null)
            {
                JobDestination? current = GetClosestDestination(entryCar);
                if (current!= null)
                {
                    List<JobOffer> offers = availableJobs[current];
                    JobOffer? offer = offers.FirstOrDefault(j => j.Id.ToString() == number);
                    if (offer != null)
                    {
                        offers.Remove(offer);
                        _jobs[entryCar] = offer;
                        offer.StartJob(entryCar);
                        GenerateJobs();
                        return Task.FromResult(true);
                    }
                }
            }
            return Task.FromResult(false);
        }

        internal Task<List<JobOffer>?> GetJobsList(EntryCar player)
        {
            if (_jobs[player] != null) return Task.FromResult(new List<JobOffer> { _jobs[player] }); 
            JobDestination? current = GetClosestDestination(player);
            if (current == null)
                return Task.FromResult<List<JobOffer>?>(null);
            else
            {
                return Task.FromResult(availableJobs[current]);
            }
        }
    }
}
