using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RolePlayPlugin
{
    internal static class Extensions
    {
        internal static string Convert(this Dictionary<Tuple<JobDestination, JobDestination>, List<LeaderboardEntry>> a)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var kvp in a)
            {
                sb.AppendLine($"{kvp.Key.Item1.Name} => {kvp.Key.Item2.Name}");
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    var entry = kvp.Value[i];
                    sb.AppendLine($"{i+1}:\t{entry.ToString()}");
                }
                sb.AppendLine();

            }
            return sb.ToString();
        }
    }
}
