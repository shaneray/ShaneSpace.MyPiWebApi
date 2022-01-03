using ShaneSpace.MyPiWebApi.Models.Buttons;
using System.Collections.Generic;

namespace ShaneSpace.MyPiWebApi.Web.ViewModels.MyPi
{
    public class ButtonViewModel : BaseViewModel<IButton>, IGpioComponentViewModel
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public Dictionary<string, int> Pins { get; set; }
        public Dictionary<string, string> Attributes { get; set; }
        public IReadOnlyCollection<GenericEventViewModel> History { get; set; }
        public bool IsPushed { get; set; }
    }
}