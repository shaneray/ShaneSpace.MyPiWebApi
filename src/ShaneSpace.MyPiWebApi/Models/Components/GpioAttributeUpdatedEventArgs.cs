using System;

namespace ShaneSpace.MyPiWebApi.Models.Components
{
    public class GpioAttributeUpdatedEventArgs : EventArgs
    {
        public string AttributeKey { get; init; }
        public string OriginalValue { get; init; }
        public string NewValue { get; init; }
    }
}