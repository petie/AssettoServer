//using System.Numerics;

//namespace RolePlayPlugin
//{
//    public class JobsRepository
//    {
//        private List<JobOffer> AvailableJobs = new List<JobOffer>();
//        internal Task<List<JobOffer>> GetJobsList()
//        {
//            return Task.FromResult(AvailableJobs);
//        }

//        internal async Task StartJob(string? guid, string number)
//        {
//            JobOffer selectedJob = AvailableJobs.FirstOrDefault(j => j.Id == number);
//            if (selectedJob != null && selectedJob.UserInProgress == null)
//            {
//                selectedJob.UserInProgress = guid;
//            }
//        }

//        internal Task EndJob(string? guid, Vector3 position)
//        {
//            if (_jobs.)
//        }
//    }
//}