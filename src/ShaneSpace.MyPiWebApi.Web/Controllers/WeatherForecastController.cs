using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ShaneSpace.MyPiWebApi.Web.ViewModels.WeatherForecast;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShaneSpace.MyPiWebApi.Web.Controllers
{
    /// <summary>
    /// The WeatherForecast Controller
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        /// <summary>
        /// The WeatherForecast Controller Constructor
        /// </summary>
        /// <param name="logger"></param>
        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets fake weather information
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IEnumerable<WeatherForecastViewModel> Get()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5)
                .Select(index => new WeatherForecastViewModel
                {
                    Date = DateTime.Now.AddDays(index),
                    TemperatureC = rng.Next(-20, 55),
                    Summary = Summaries[rng.Next(Summaries.Length)]
                })
            .ToArray();
        }
    }
}
