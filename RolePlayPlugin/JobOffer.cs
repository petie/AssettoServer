using AssettoServer.Server;
using System.Numerics;

namespace RolePlayPlugin
{
    public class JobOffer {

        public JobOffer(int jobCount, JobDestination start, JobDestination finish, CargoType cargoType)
        {
            Id = jobCount;
            Start = start;
            Finish = finish;
            Cargo = cargoType;
            Distance = Vector3.Distance(start.Position, finish.Position);
            StartPrize = (int)Math.Floor((int)Cargo * 10000 * Math.Floor(Distance/1000));
        }

        public int Id { get; private set; }
        public int StartPrize { get; private set; }

        public JobDestination Start { get; private set; }
        public JobDestination Finish { get; private set; }

        public long JobStartTime { get; private set; }
        public long JobFinishTime { get; private set; }

        public bool IsFinished { get; set; }

        public float Distance { get; private set; }
        public CargoType Cargo { get; private set; }
        public EntryCar UserInProgress { get; private set; }
        public int CargoDamage { get; private set; }

        public int CurrentPrize { get
            {
                return (StartPrize - CargoDamage);
            } }

        public int CurrentDamagePercent { get
            {
                return 100 - ((StartPrize - CargoDamage) * 100 / StartPrize);
            } }

        public override string ToString()
        {
            return $"{Id}: {StartPrize} {Start.Name} => {Finish.Name} {Cargo}";
        }

        public int OnCollision(float speed)
        {
            CargoDamage += (int)Math.Ceiling(speed * 100);
            if (CargoDamage > StartPrize) CargoDamage = StartPrize;
            return CurrentDamagePercent;
        }

        internal int Complete(long finishTime)
        {
            IsFinished = true;
            JobFinishTime = finishTime;
            return CurrentPrize;
        }

        internal void StartJob(EntryCar entryCar, long startTime)
        {
            UserInProgress = entryCar;
            JobStartTime = startTime;
        }
    }

    public enum CargoType
    {
        Tofu = 1,
        Caviar,
        Documents,
        Firearms,
        Laptops,
        Gold,
        VIPs
    }
}