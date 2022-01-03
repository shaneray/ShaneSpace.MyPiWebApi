using ShaneSpace.MyPiWebApi.Models;
using System.Collections.Generic;

namespace ShaneSpace.MyPiWebApi.Web.ViewModels.MyPi
{
    /// <summary>
    /// Records the results of a process that was ran
    /// </summary>
    public class ProcessResultViewModel : BaseViewModel<ProcessResult>
    {
        public List<string> StandardOutput { get; set; }
        public List<string> StandardError { get; set; }
    }
}