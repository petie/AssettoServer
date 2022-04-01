namespace RolePlayPlugin
{
    public class JobsRepository
    {
        private List<JobOffer> AvailableJobs = new List<JobOffer>();
        internal Task<List<JobOffer>> GetJobsList()
        {
            return Task.FromResult(AvailableJobs);
        }
    }
}