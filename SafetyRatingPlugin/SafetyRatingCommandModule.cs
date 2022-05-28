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
        [Command("rating")]
        public void Rating(string userName)
        {
            SafetyRatingPlugin.Instance?.GetRatings(userName, Context.Server.CurrentTime);
        }
    }
}
