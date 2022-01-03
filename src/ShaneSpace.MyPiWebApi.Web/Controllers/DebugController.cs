using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ShaneSpace.MyPiWebApi.Web.ViewModels.Debug;
using System;
using System.Runtime.InteropServices;

namespace ShaneSpace.MyPiWebApi.Web.Controllers
{
    /// <summary>
    /// The Debug Controller
    /// </summary>
    [ApiController]
    [Route("api/debug")]
    public class DebugController : ControllerBase
    {
        private readonly ILogger<DebugController> _logger;
        private readonly IHttpContextAccessor _httpContext;

        /// <summary>
        /// The Debug Controller Constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="httpContext"></param>
        public DebugController(ILogger<DebugController> logger, IHttpContextAccessor httpContext)
        {
            _logger = logger;
            _httpContext = httpContext;
        }

        /// <summary>
        /// Gets Debug Information
        /// </summary>
        /// <returns>DebugInfoViewModel</returns>
        [HttpGet]
        public DebugInfoViewModel GetDebugInfo()
        {
            const string unknownValue = "unknown";
            return new DebugInfoViewModel
            {
                Server = new ServerDebugInfoViewModel
                {
                    MachineName = Environment.MachineName,
                    OSDescription = RuntimeInformation.OSDescription,
                    OSArchitecture = RuntimeInformation.OSArchitecture.ToString(),
                    OSVersion = Environment.OSVersion.ToString(),
                    ProcessorCount = Environment.ProcessorCount,
                },
                Application = new ApplicationInfoViewModel
                {
                    CommandLine = Environment.CommandLine,
                    CurrentDirectory = Environment.CurrentDirectory,
                    UserName = Environment.UserName,
                    FrameworkDescription = RuntimeInformation.FrameworkDescription,
                    ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                    RuntimeIdentifier = RuntimeInformation.RuntimeIdentifier,
                    SystemVersion = RuntimeEnvironment.GetSystemVersion()
                },
                Request = new RequestInfoViewModel
                {
                    LocalIpAddress = _httpContext.HttpContext?.Connection.LocalIpAddress?.ToString() ?? unknownValue,
                    LocalPort = _httpContext.HttpContext?.Connection.LocalPort.ToString() ?? unknownValue,
                    RemoteIpAddress = _httpContext.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? unknownValue,
                    RemotePort = _httpContext.HttpContext?.Connection.RemotePort.ToString() ?? unknownValue,
                    IsHttps = _httpContext.HttpContext?.Request.IsHttps.ToString() ?? unknownValue
                }
            };
        }
    }
}
