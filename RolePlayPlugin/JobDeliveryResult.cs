using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RolePlayPlugin
{
    internal class JobDeliveryResult
    {
        public bool IsCompleted { get; set; }
        public int FinalPrize { get; set; }
        public int BasePrize { get; set; }
        public List<Tuple<int, string>> Modifications { get; set; }
        public string ErrorMessage { get; set; }

        public long TotalTime { get; set; }

        public static JobDeliveryResult NoActiveJob()
        {
            return new JobDeliveryResult
            {
                IsCompleted = false,
                ErrorMessage = Constants.NO_ACTIVE_JOB
            };
        }

        internal static JobDeliveryResult TooFarAwayFromDestination()
        {
            return new JobDeliveryResult
            {
                IsCompleted = false,
                ErrorMessage = Constants.TOO_FAR_AWAY_FROM_DESTINATION
            };
        }

        internal void AddMod(int modification, string message)
        {
            Modifications.Add(new Tuple<int, string>(modification, message));
        }

        public override string ToString()
        {
            if (!IsCompleted)
            {
                return ErrorMessage;
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Job Completed!");
                sb.AppendLine($"Base prize: ¥{BasePrize}");
                foreach (var tuple in Modifications)
                {
                    sb.AppendLine($"¥{tuple.Item1}\t{tuple.Item2}");
                }
                sb.AppendLine($"TOTAL: ¥{FinalPrize} has been added to your account");
                return sb.ToString();
            }
        }

        public JobDeliveryResult()
        {
            Modifications = new List<Tuple<int, string>>();
        }
    }
}
