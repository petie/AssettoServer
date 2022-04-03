using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RolePlayPlugin
{
    internal class JobGenerator {


        private static List<Vector3> Destinations = new List<Vector3>();

        internal static JobOffer GenerateJob()
        {
            Random r = new Random();
            Vector3 start, end;
            int random = r.Next(1, 7);
            do
            {
                start = Destinations[r.Next(0, Destinations.Count)];
                end = Destinations[r.Next(0, Destinations.Count)];
            } while (start == end);
            var distance = Vector3.Distance(start, end);
            JobOffer offer = new JobOffer(start, end, (CargoType)random);
            offer.Cargo = (CargoType)random;
            offer.StartPrize = (int)Math.Floor(random * 10000 * distance);
            offer.Start = start;
            offer.Destination = end;
            
            return offer;
        }
    }
}
