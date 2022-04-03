using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RolePlayPlugin
{
    public class CarOffer
    {
        public string Car { get; set; }
        public int Price { get; set; }

        public CarOffer(string car, int price)
        {
            Car = car;
            Price = price;
        }
    }
}
