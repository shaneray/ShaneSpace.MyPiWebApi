using ShaneSpace.MyPiWebApi.Models.Buttons;
using ShaneSpace.MyPiWebApi.Models.Leds;
using System.Collections.Generic;

namespace ShaneSpace.MyPiWebApi.Models
{
    public class RaspberryPiInfo
    {
        public string Name { get; set; }
        public IReadOnlyCollection<ILed> Leds { get; set; }
        public IReadOnlyCollection<IButton> Buttons { get; set; }
    }
}