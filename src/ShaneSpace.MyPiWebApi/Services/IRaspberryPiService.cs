using ShaneSpace.MyPiWebApi.Models;
using ShaneSpace.MyPiWebApi.Models.Buttons;
using ShaneSpace.MyPiWebApi.Models.Displays;
using ShaneSpace.MyPiWebApi.Models.Leds;
using System.Collections.Generic;

namespace ShaneSpace.MyPiWebApi.Services
{
    public interface IRaspberryPiService
    {
        Camera Camera { get; }
        Ssd1306Display Display { get; }

        RaspberryPiInfo GetInfo();
        ProcessResult Shutdown();
        ProcessResult Restart();

        IReadOnlyCollection<ILed> GetLeds();
        ILed GetLedByIndex(int index);

        IReadOnlyCollection<IButton> GetButtons();
    }
}