using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Weather_Discord_Bot.Bot.Weather_Data
{
    internal class WeatherData
    {
        public Location Location { get; set; }
        public Current Current { get; set; }
    }
}
