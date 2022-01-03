using ShaneSpace.MyPiWebApi.Models;
using System;

namespace ShaneSpace.MyPiWebApi.Web.ViewModels.MyPi
{
    public class GenericEventViewModel : BaseViewModel<GenericEvent>
    {
        public DateTimeOffset EventStart { get; set; }
        public DateTimeOffset? EventEnd { get; set; }
        public TimeSpan EventDuration { get; set; }
        public string Message { get; set; }
    }
}