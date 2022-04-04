using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RolePlayPlugin
{
    public class RolePlayConfiguration
    {
        public string? DbConnectionString { get; set; }

        public int MaxAvailableJobs { get; set; }

        public List<JobDestination>? JobDestinations { get; set; }

        public int JobDistance { get; set; }

        public override string ToString()
        {
            return $"MaxAvailableJobs = {MaxAvailableJobs}, JobDestinations.Count = {JobDestinations.Count}";
        }
    }
}
