using ShaneSpace.MyPiWebApi.Models.Leds;
using System.Collections.Generic;

namespace ShaneSpace.MyPiWebApi.Web.ViewModels.MyPi
{
    /// <summary>
    /// LED View Model
    /// </summary>
    // ReSharper disable UnusedMember.Global
    public class LedViewModel : BaseViewModel<ILed>, IGpioComponentViewModel
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public Dictionary<string, int> Pins { get; set; }
        public Dictionary<string, string> Attributes { get; set; }
        public IReadOnlyCollection<GenericEventViewModel> History { get; set; }
        public int LedIndex { get; set; }
        public bool IsOn { get; set; }
        public int Brightness { get; set; }
        public string Trigger { get; set; }
    }
}
