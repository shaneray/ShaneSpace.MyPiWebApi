using ShaneSpace.MyPiWebApi.Models.Components;
using System.Collections.Generic;

namespace ShaneSpace.MyPiWebApi.Models.Leds
{
    public interface ILed : IGpioComponent
    {
        int LedIndex { get; }
        bool IsOn { get; set; }
        string Trigger { get; }
        int Brightness { get; set; }
        IEnumerable<string> AvailableTriggers { get; }
    }
}