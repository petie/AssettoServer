using System.Numerics;

namespace RolePlayPlugin
{
    public class JobOffer {
        public string Id { get; set; }
        public int Prize { get; set; }
        public Vector3 Destination { get; set; }
        public string Cargo { get; set; }
    }
}