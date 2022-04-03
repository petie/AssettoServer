using AssettoServer.Server;
using System.Numerics;

namespace RolePlayPlugin
{
    public class JobOffer {

        public JobOffer(Vector3 start, Vector3 end, CargoType cargoType)
        {
            Start = start;
            Destination = end;
            Cargo = cargoType;
        }

        public string Id { get; set; }
        public int StartPrize { get; set; }
        public Vector3 Destination { get; set; }
        public string DestinationName { get; set; }
        public CargoType Cargo { get; set; }
        public EntryCar UserInProgress { get; set; }
        public int CargoDamage { get; set; }

        public Vector3 Start { get; set; }

        public bool IsFinished { get; set; }

        public int Prize { get
            {
                return (StartPrize - CargoDamage) / StartPrize;
            } }

        public override string ToString()
        {
            return $"{Id}: ¥{StartPrize} {DestinationName} {Cargo}";
        }

        public int OnCollision(float speed)
        {
            CargoDamage += (int)Math.Ceiling(speed);
            return Prize;
        }

        internal int Finish()
        {
            bool IsFinished = true;
            return Prize;
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