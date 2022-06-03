using AssettoServer.Server.Plugin;
using Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyRatingPlugin
{
    public class SafetyRatingModule : AssettoServerModule<SafetyRatingConfiguration>
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<SafetyRating>().AsSelf().As<IAssettoServerAutostart>().SingleInstance();
        }
    }
}
