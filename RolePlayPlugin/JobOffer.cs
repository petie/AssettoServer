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
        public float Distance { get; private set; }
        public CargoType Cargo { get; private set; }
        public EntryCar UserInProgress { get; private set; }
        public int CargoDamage { get; private set; }

        public bool IsFinished { get; private set; }

        public int CurrentPrize { get
            {
                return (StartPrize - CargoDamage) / StartPrize;
            } }

        public override string ToString()
        {
            return $"{Id}: {StartPrize} {Start.Name} => {Finish.Name} {Cargo}";
        }

        public int OnCollision(float speed)
        {
            CargoDamage += (int)Math.Ceiling(speed);
            return CurrentPrize;
        }

        internal int Complete()
        {
            bool IsFinished = true;
            return CurrentPrize;
        }

        internal void StartJob(EntryCar entryCar)
        {
            UserInProgress = entryCar;
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