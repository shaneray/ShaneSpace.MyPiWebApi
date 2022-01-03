using Serilog;
using ShaneSpace.MyPiWebApi.Models.Buttons;
using ShaneSpace.MyPiWebApi.Models.Leds;
using System.Drawing;

namespace ShaneSpace.MyPiWebApi.Services
{
    public class MyPiService : BaseRaspberryPiService, IMyPiService
    {
        private readonly GpioButton _button1;
        private readonly RgbLed _rgbLed;
        private readonly GpioLed _blueLed;
        private int _button1PushCount = 0;
        protected override string RaspberryPiName => "Shane's Pi";

        public MyPiService(IGpioService gpioService, ILogger logger)
            : base(gpioService, logger)
        {
            // build components
            _rgbLed = new RgbLed(GpioService.Controller, 17, 27, 22, 1, "RGB LED", Color.White, logger);
            _blueLed = new GpioLed(GpioService.Controller, 16, "Blue LED", Color.Blue, logger);
            _button1 = new GpioButton(GpioService.Controller, 12, "Button 1", logger);
            _button1.StatusUpdated += Button1OnStatusUpdated;

            // register components
            Leds.Add(_rgbLed);
            Leds.Add(_blueLed);
            Buttons.Add(_button1);

            Logger.Information("{ServiceName} initialized.", nameof(MyPiService));
        }

        private void Button1OnStatusUpdated(object sender, ButtonStatusUpdatedEventArgs eventArgs)
        {
            if (eventArgs.IsPushed)
            {
                _rgbLed.Color = _button1PushCount switch
                {
                    1 => Color.Red,
                    2 => Color.Green,
                    3 => Color.Blue,
                    _ => Color.White
                };

                _button1PushCount++;
                if (_button1PushCount > 3)
                {
                    _button1PushCount = 0;
                }
            }

            _rgbLed.IsOn = eventArgs.IsPushed;
        }
    }
}