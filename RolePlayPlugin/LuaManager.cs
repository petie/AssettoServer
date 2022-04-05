using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RolePlayPlugin
{
    internal class LuaManager
    {
        public static string GetClientScript(RolePlayConfiguration config)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("function script.update(dt)");
            foreach (var item in config.JobDestinations)
            {
                sb.AppendLine($"\trender.debugSphere(vec3({item.X},{item.Y},{item.Z}), {config.JobDistance}, rgbm.colors.green)");
            }
            sb.AppendLine("end");
            return sb.ToString();
        }
    }
}
