using Serilog;
using ShaneSpace.MyPiWebApi.Models.Components;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Drawing;

namespace ShaneSpace.MyPiWebApi.Models.Leds
{
    public class RgbLed : BaseGpioComponent, ILed
    {
        private readonly int _ledIndex;
        private readonly GpioLed _red;
        private readonly GpioLed _green;
        private readonly GpioLed _blue;
        private Color _color;
        private readonly ILogger _logger;

        public RgbLed(
            GpioController gpioController,
            int redPinNumber, int greenPinNumber, int bluePinNumber,
            int ledIndex, string ledName, Color color, ILogger logger)
                : base(gpioController, logger)
        {
            _ledIndex = ledIndex;
            _color = color;
            _logger = logger;

            Name = string.IsNullOrWhiteSpace(ledName)
                ? $"RGB{LedIndex}"
                : ledName;

            Pins.Add("Red", redPinNumber);
            Pins.Add("Green", redPinNumber);
            Pins.Add("Blue", redPinNumber);

            _red = new GpioLed(gpioController, redPinNumber, "RGB-Red", Color.Red);
            _green = new GpioLed(gpioController, greenPinNumber, "RGB-Green", Color.Green);
            _blue = new GpioLed(gpioController, bluePinNumber, "RGB-Blue", Color.Blue);

            InitializeAttribute("Status", "Off");
            InitializeAttribute("Brightness", "255");

            SetColor(color);
            SetBrightness(255);
        }

        public override string Name { get; }
        public int LedIndex => _ledIndex + 2000;
        public string Trigger => "GPIO";
        public IEnumerable<string> AvailableTriggers { get; } = new[] { "GPIO" };

        public bool IsOn
        {
            get => GetIsOn();
            set => SetIsOn(value);
        }

        public int Brightness
        {
            get => GetBrightness();
            set => SetBrightness(value);
        }

        public Color Color
        {
            get => _color;
            set => SetColor(value);
        }

        protected override void LogAttributeChange(GpioAttributeUpdatedEventArgs eventArgs)
        {
            _logger?.Information(
                "Component {ComponentName} attribute {Attribute} changed value from {OriginalValue} to {NewValue} - Status: {Status}, Color: {Color}, Brightness: {Brightness} ({BrightnessPercent}%)",
                Name, eventArgs.AttributeKey, eventArgs.OriginalValue, eventArgs.NewValue,
                Attributes["Status"], Attributes["Color"], Attributes["Brightness"],
                (double.Parse(Attributes["Brightness"]) / 255.0) * 100);
        }

        public void SetColor(Color color)
        {
            _color = color;
            UpdateAttribute("Color", color.ToString());

            var brightness = double.Parse(Attributes["Brightness"]) / 255.0;
            _red.Brightness = (int)(_color.R * brightness);
            _green.Brightness = (int)(_color.G * brightness);
            _blue.Brightness = (int)(_color.B * brightness);
        }

        private bool GetIsOn()
        {
            return Attributes["Status"] == "On";
        }

        private void SetIsOn(bool value)
        {
            var status = value
                ? "On"
                : "Off";
            UpdateAttribute("Status", status);

            _red.IsOn = value;
            _green.IsOn = value;
            _blue.IsOn = value;
        }

        private int GetBrightness()
        {
            return int.Parse(Attributes["Brightness"]);
        }

        private void SetBrightness(int value)
        {
            value = value switch
            {
                > 255 => 255,
                < 0 => 0,
                _ => value
            };

            UpdateAttribute("Brightness", value.ToString());

            var brightness = double.Parse(Attributes["Brightness"]) / 255.0;
            _red.Brightness = (int)(_color.R * brightness);
            _green.Brightness = (int)(_color.G * brightness);
            _blue.Brightness = (int)(_color.B * brightness);
        }
    }
}