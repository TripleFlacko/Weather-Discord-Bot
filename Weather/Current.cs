using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Weather_Discord_Bot.Weather
{
    internal class Current
    {
        public double Temp_C { get; set; }
        public Condition Condition { get; set; }
        public double Wind_Kph { get; set; }
        public int Humidity { get; set; }
        public int Cloud { get; set; }
    }
}
