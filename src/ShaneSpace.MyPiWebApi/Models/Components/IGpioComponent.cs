using System.Collections.Generic;

namespace ShaneSpace.MyPiWebApi.Models.Components
{
    public interface IGpioComponent
    {
        string Name { get; }
        string Type { get; }
        Dictionary<string, int> Pins { get; set; }
        Dictionary<string, string> Attributes { get; set; }
        IReadOnlyCollection<GenericEvent> History { get; }
    }
}
