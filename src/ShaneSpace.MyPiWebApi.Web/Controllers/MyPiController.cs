using AutoMapper;
using Iot.Device.Hcsr04;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ShaneSpace.MyPiWebApi.Models.Leds;
using ShaneSpace.MyPiWebApi.Services;
using ShaneSpace.MyPiWebApi.Web.CommandModels.MyPi;
using ShaneSpace.MyPiWebApi.Web.ViewModels.MyPi;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnitsNet;

namespace ShaneSpace.MyPiWebApi.Web.Controllers
{
    /// <summary>
    /// The Debug Controller
    /// </summary>
    [ApiController]
    [Route("api/my-pi")]
    public class MyPiController : ControllerBase
    {
        private readonly ILogger<MyPiController> _logger;
        private readonly IMapper _objectMapper;
        private readonly IMyPiService _myPi;

        /// <summary>
        /// The Debug Controller Constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="objectMapper"></param>
        /// <param name="myPi"></param>
        public MyPiController(
            ILogger<MyPiController> logger,
            IMapper objectMapper,
            IMyPiService myPi)
        {
            _logger = logger;
            _objectMapper = objectMapper;
            _myPi = myPi;
        }

        /// <summary>
        /// Gets Raspberry PI Information
        /// </summary>
        /// <returns>RaspberryPiViewInfoModel</returns>
        [HttpGet]
        [ProducesResponseType(typeof(RaspberryPiInfoViewModel), (int)HttpStatusCode.OK)]
        public RaspberryPiInfoViewModel GetInfo()
        {
            var output = _myPi.GetInfo();
            return _objectMapper.Map<RaspberryPiInfoViewModel>(output);
        }

        /// <summary>
        /// Power Controller
        /// </summary>
        /// <returns></returns>
        [HttpPost("power")]
        [ProducesResponseType(typeof(ProcessResultViewModel), (int)HttpStatusCode.OK)]
        public ProcessResultViewModel Power(PowerOptionsCommandModel powerOptionsCommand)
        {
            var output = powerOptionsCommand.Restart
                ? _myPi.Restart()
                : _myPi.Shutdown();

            return _objectMapper.Map<ProcessResultViewModel>(output);
        }

        /// <summary>
        /// Gets LED Information
        /// </summary>
        /// <returns>LedViewModels</returns>
        [HttpGet("leds")]
        [ProducesResponseType(typeof(LedViewModel[]), (int)HttpStatusCode.OK)]
        public LedViewModel[] GetLeds()
        {
            var output = _myPi.GetLeds();
            return _objectMapper.Map<LedViewModel[]>(output);
        }

        /// <summary>
        /// Gets Button Information
        /// </summary>
        /// <returns>ButtonViewModel</returns>
        [HttpGet("buttons")]
        [ProducesResponseType(typeof(ButtonViewModel[]), (int)HttpStatusCode.OK)]
        public ButtonViewModel[] GetButtons()
        {
            var output = _myPi.GetButtons();
            return _objectMapper.Map<ButtonViewModel[]>(output);
        }

        /// <summary>
        /// Updates LED properties
        /// </summary>
        /// <returns>LedViewModel</returns>
        [HttpPost("leds/{ledIndex}")]
        [ProducesResponseType(typeof(LedViewModel), (int)HttpStatusCode.OK)]
        public LedViewModel UpdateLed([FromRoute] int ledIndex, [FromBody] UpdateLedCommandModel updateLedCommand)
        {
            var ledByIndex = _myPi.GetLedByIndex(ledIndex);
            ledByIndex.IsOn = updateLedCommand.IsOn;
            ledByIndex.Brightness = updateLedCommand.Brightness;
            return _objectMapper.Map<LedViewModel>(ledByIndex);
        }

        /// <summary>
        /// Sets LED color
        /// </summary>
        /// <returns>LedViewModel</returns>
        [HttpPost("leds/{ledIndex}/color/{color}")]
        [ProducesResponseType(typeof(LedViewModel), (int)HttpStatusCode.OK)]
        public LedViewModel UpdateLed([FromRoute] int ledIndex, [FromRoute] KnownColor color)
        {
            var ledByIndex = _myPi.GetLedByIndex(ledIndex);

            if (ledByIndex is RgbLed rgbLed)
            {
                rgbLed.SetColor(Color.FromKnownColor(color));
            }
            else
            {
                throw new NotImplementedException();
            }
            return _objectMapper.Map<LedViewModel>(ledByIndex);
        }

        /// <summary>
        /// Capture picture
        /// </summary>
        /// <returns></returns>
        [HttpPost("camera/picture/capture")]
        public async Task<IActionResult> TakePicture()
        {
            await _myPi.Camera.TakePictureAsync().ConfigureAwait(false);
            return Ok();
        }

        /// <summary>
        /// Start video capture
        /// </summary>
        /// <returns></returns>
        [HttpPost("camera/video/start")]
        public IActionResult StartVideo()
        {
#pragma warning disable 4014
            _myPi.Camera.StartVideo();
#pragma warning restore 4014
            return Ok();
        }

        /// <summary>
        /// Stop video capture
        /// </summary>
        /// <returns></returns>
        [HttpPost("camera/video/stop")]
        public IActionResult StopVideo()
        {
            _myPi.Camera.StopVideo();
            return Ok();
        }

        /// <summary>
        /// Set Display Text
        /// </summary>
        /// <returns></returns>
        [HttpPost("display")]
        public IActionResult SetDisplay(string text)
        {
            if (text == "hello")
            {
                _myPi.Display.HelloWorld();
            }
            else
            {
                _myPi.Display.SendMessage(text);
            }

            return Ok();
        }

        /// <summary>
        /// Display Alien
        /// </summary>
        /// <returns></returns>
        [HttpPost("display/alien")]
        public IActionResult DisplayAlien()
        {
            _myPi.Display.DisplayAlien();
            return Ok();
        }

        /// <summary>
        /// Display Clock
        /// </summary>
        /// <returns></returns>
        [HttpPost("display/clock")]
        public IActionResult DisplayClock()
        {
            _myPi.Display.DisplayClock();
            return Ok();
        }

        /// <summary>
        /// Display temp
        /// </summary>
        /// <returns></returns>
        [HttpPost("display/temp")]
        public IActionResult DisplayTemp()
        {
            try
            {
                var data = _myPi.GetTemperatureAndHumidityData();
                var temperature = data.Temperature;
                var humidity = data.Humidity;
                _myPi.Display.SendMessage($"Temp: {Math.Round(temperature.DegreesFahrenheit, 1)} \u00B0F{Environment.NewLine}Humidity: {Math.Round(humidity.Percent, 1)} %");
            }
            catch (Exception)
            {
                _myPi.Display.SendMessage("Error reading DHT sensor");
                throw;
            }

            return Ok();
        }

        /// <summary>
        /// Clear Display
        /// </summary>
        /// <returns></returns>
        [HttpPost("display/clear")]
        public IActionResult ClearDisplay()
        {
            _myPi.Display.ClearScreen();
            return Ok();
        }

        /// <summary>
        /// Set contrast percentage
        /// </summary>
        /// <returns></returns>
        [HttpPost("display/contrast")]
        public IActionResult DisplayContrast(double percentage)
        {
            _myPi.Display.SetContrast(percentage);
            return Ok();
        }

        /// <summary>
        /// Display Demo
        /// </summary>
        /// <returns></returns>
        [HttpPost("display/demo")]
        public IActionResult DisplayDemo()
        {
            _myPi.Display.Demo();
            return Ok();
        }
    }
}
