using AssettoServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RolePlayPlugin
{
    /// <summary>
    /// Responsible for keeping track of who owns which car.
    /// </summary>
    internal class CarManager
    {
        private ACServer server;
        private RolePlayConfiguration config;
        private Dictionary<string, string> userCars;
        private CarRepository carRepository;

        public CarManager(ACServer server, RolePlayConfiguration config)
        {
            carRepository = new CarRepository();
            this.server = server;
            this.config = config;
            userCars = new Dictionary<string, string>();
            foreach (EntryCar car in this.server.EntryCars)
            {
                if (!string.IsNullOrEmpty(car.Client.Guid))
                    userCars.Add(car.Client.Guid, carRepository.GetCarForUser(car.Client.Guid));
            }
        }
    }
}
