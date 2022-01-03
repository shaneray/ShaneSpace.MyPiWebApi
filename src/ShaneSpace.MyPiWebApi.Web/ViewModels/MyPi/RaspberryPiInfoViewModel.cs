using ShaneSpace.MyPiWebApi.Models;
using System.Collections.Generic;

namespace ShaneSpace.MyPiWebApi.Web.ViewModels.MyPi
{
    /// <summary>
    /// Raspberry Pi View Model
    /// </summary>
    // ReSharper disable UnusedMember.Global
    public class RaspberryPiInfoViewModel : BaseViewModel<RaspberryPiInfo>
    {
        public string Name { get; set; }
        public IReadOnlyCollection<LedViewModel> Leds { get; set; }
        public IReadOnlyCollection<ButtonViewModel> Buttons { get; set; }
    }
}