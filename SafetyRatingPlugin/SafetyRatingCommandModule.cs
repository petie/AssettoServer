using Qmmands;
using AssettoServer.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyRatingPlugin
{
    public class SafetyRatingCommandModule : ACModuleBase
    {
        private SafetyRating _rating;

        public SafetyRatingCommandModule(SafetyRating rating)
        {
            _rating = rating;
        }
        [Command("rating")]
        public void Rating(string userName)
        {
            Reply(_rating.GetRatings(userName));
        }

        [Command("ratings")]
        public void Ratings()
        {
            Reply(_rating.GetRatings());
        }
    }
}
