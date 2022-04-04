using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace RolePlayPlugin
{
    public class JobDestination
    {
        public string Name { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        [YamlIgnore] public Vector3 Position { get
            {
                if (_position == null)
                {
                    _position = new Vector3(X, Y, Z);
                    return _position.Value;
                }
                return _position.Value;
            }
        }

        private Vector3? _position;

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj.GetType() != typeof(JobDestination)) return false;
            JobDestination other = (JobDestination)obj;
            if (other.X == X && other.Y == Y && other.Z == Z) return true;
            return base.Equals(obj);
        }
    }
}
