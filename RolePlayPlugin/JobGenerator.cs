using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RolePlayPlugin
{
    internal class JobGenerator {

        internal static int jobCount = 0;
        internal static JobOffer GenerateJob(List<JobDestination>? destinations)
        {
            Random r = new Random();
            JobDestination start, end;
            int random = r.Next(1, 7);
            do
            {
                start = destinations[r.Next(0, destinations.Count)];
                end = destinations[r.Next(0, destinations.Count)];
            } while (start == end);
            JobOffer offer = new JobOffer(jobCount, start, end, (CargoType)random);        
            jobCount++;
            return offer;
        }
    }
}
