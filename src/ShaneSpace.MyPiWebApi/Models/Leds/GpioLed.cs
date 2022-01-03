using Serilog;
using ShaneSpace.MyPiWebApi.Models.Components;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.Pwm.Drivers;
using System.Drawing;

namespace ShaneSpace.MyPiWebApi.Models.Leds
{
    public class GpioLed : BaseGpioComponent, ILed
    {
        private readonly ILogger _logger;
        private SoftwarePwmChannel _softwarePwmChannel;

        public GpioLed(GpioController gpioController, int pinNumber, string ledName, Color color, ILogger logger = null)
            : base(gpioController, logger)
        {
            _logger = logger?.ForContext<GpioLed>();

            Name = string.IsNullOrWhiteSpace(ledName)
                ? $"GPIO{LedIndex}"
                : ledName;

            Pins.Add("Anode", pinNumber);

            InitializeAttribute("Color", color.ToString());
            InitializeAttribute("Status", "Off");

            SetBrightness(255);
            SetIsOn(false);
        }

        public override string Name { get; }
        public int LedIndex => Pins["Anode"] + 1000;
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

        protected override void LogAttributeChange(GpioAttributeUpdatedEventArgs eventArgs)
        {
            _logger?.Information(
                "Component {ComponentName} attribute {Attribute} changed value from {OriginalValue} to {NewValue} - Status: {Status}, Color: {Color}, Brightness: {Brightness} ({BrightnessPercent}%)",
                Name, eventArgs.AttributeKey, eventArgs.OriginalValue, eventArgs.NewValue,
                Attributes["Status"], Attributes["Color"], Attributes["Brightness"],
                (double.Parse(Attributes["Brightness"]) / 255.0) * 100);
        }

        private bool GetIsOn()
        {
            return Attributes["Status"] == "On";
        }

        private void SetIsOn(bool value)
        {
            switch (value)
            {
                case false:
                    {
                        UpdateAttribute("Status", "Off");

                        SetPinValue(PinValue.Low);
                        break;
                    }
                case true:
                    {
                        UpdateAttribute("Status", "On");

                        var brightness = int.Parse(Attributes["Brightness"]);
                        SetBrightness(brightness);

                        break;
                    }
            }
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

            if (!IsOn)
            {
                return;
            }

            switch (value)
            {
                case 0:
                    SetPinValue(PinValue.Low);
                    break;
                case 255:
                    SetPinValue(PinValue.High);
                    break;
                default:
                    {
                        var brightness = double.Parse(Attributes["Brightness"]) / 255.0;

                        if (_softwarePwmChannel == null)
                        {
                            if (GpioController.IsPinOpen(Pins["Anode"]))
                            {
                                GpioController.ClosePin(Pins["Anode"]);
                            }
                            _softwarePwmChannel = new SoftwarePwmChannel(Pins["Anode"], 500, brightness, false, GpioController, false);
                            _softwarePwmChannel.Start();
                        }

                        _softwarePwmChannel.DutyCycle = brightness;
                        break;
                    }
            }
        }

        private void SetPinValue(PinValue pinValue)
        {
            if (_softwarePwmChannel != null)
            {
                _softwarePwmChannel.Dispose();
                _softwarePwmChannel = null;
            }
            if (!GpioController.IsPinOpen(Pins["Anode"]))
            {
                GpioController.OpenPin(Pins["Anode"], PinMode.Output);
            }
            GpioController.Write(Pins["Anode"], pinValue);
        }
    }
}