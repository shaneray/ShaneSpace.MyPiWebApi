using System;

namespace ShaneSpace.MyPiWebApi.Models.Buttons
{
    public class ButtonStatusUpdatedEventArgs : EventArgs
    {
        public bool IsPushed { get; init; }
    }
}