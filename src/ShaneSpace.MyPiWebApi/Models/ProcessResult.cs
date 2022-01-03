using System.Collections.Generic;

namespace ShaneSpace.MyPiWebApi.Models
{
    public class ProcessResult
    {
        public List<string> StandardOutput { get; set; }
        public List<string> StandardError { get; set; }
    }
}