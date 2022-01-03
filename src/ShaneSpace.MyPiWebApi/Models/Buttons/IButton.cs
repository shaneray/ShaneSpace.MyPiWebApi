using ShaneSpace.MyPiWebApi.Models.Components;

namespace ShaneSpace.MyPiWebApi.Models.Buttons
{
    public interface IButton : IGpioComponent
    {
        bool IsPushed { get; }
    }
}