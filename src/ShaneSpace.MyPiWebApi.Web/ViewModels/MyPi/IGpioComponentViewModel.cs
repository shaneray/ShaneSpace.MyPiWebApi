using System.Collections.Generic;

namespace ShaneSpace.MyPiWebApi.Web.ViewModels.MyPi
{
    public interface IGpioComponentViewModel
    {
        string Name { get; set; }
        string Type { get; set; }
        Dictionary<string, int> Pins { get; set; }
        Dictionary<string, string> Attributes { get; set; }
        IReadOnlyCollection<GenericEventViewModel> History { get; set; }
    }
}