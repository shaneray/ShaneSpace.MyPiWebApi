using Serilog;
using ShaneSpace.MyPiWebApi.Models.Components;
using System;
using System.Device.Gpio;

namespace ShaneSpace.MyPiWebApi.Models.Buttons
{
    public class GpioButton : BaseGpioComponent, IButton
    {
        protected override int MaxAttributeChangeHistory => 10;
        public event EventHandler<ButtonStatusUpdatedEventArgs> StatusUpdated;
        private PinMode _pinMode;

        public GpioButton(GpioController gpioController, int pinNumber, string buttonName, ILogger logger)
            : base(gpioController, logger)
        {
            Name = string.IsNullOrWhiteSpace(buttonName)
                ? $"Button {pinNumber}"
                : buttonName;
            Pins.Add("Anode", pinNumber);

            InitializeGpio(gpioController, pinNumber);
        }

        public override string Name { get; }

        public bool IsPushed => Attributes["Status"] == "Pushed";

        protected override void OnAttributeUpdated(GpioAttributeUpdatedEventArgs eventArgs)
        {
            base.OnAttributeUpdated(eventArgs);

            if (eventArgs.AttributeKey != "Status")
            {
                return;
            }

            StatusUpdated?.Invoke(this, new ButtonStatusUpdatedEventArgs
            {
                IsPushed = eventArgs.NewValue == "Pushed"
            });
        }

        private void InitializeGpio(GpioController gpioController, int pinNumber)
        {
            if (gpioController.IsPinOpen(pinNumber))
            {
                return;
            }

            _pinMode = PinMode.InputPullUp;
            gpioController.OpenPin(pinNumber, _pinMode);
            gpioController.RegisterCallbackForPinValueChangedEvent(pinNumber, PinEventTypes.Rising, PinChangeEventHandler);
            gpioController.RegisterCallbackForPinValueChangedEvent(pinNumber, PinEventTypes.Falling, PinChangeEventHandler);
            var status = gpioController.Read(pinNumber);
            SetPinStatus(status);
        }

        private void SetPinStatus(PinValue status)
        {
            var isPushed = _pinMode switch
            {
                PinMode.InputPullUp => status == PinValue.Low,
                PinMode.InputPullDown => status == PinValue.High,
                _ => false
            };

            var newStatus = isPushed
                ? "Pushed"
                : "Released";

            UpdateAttribute("Status", newStatus);
        }

        private void PinChangeEventHandler(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            switch (pinValueChangedEventArgs.ChangeType)
            {
                case PinEventTypes.Rising:
                    SetPinStatus(PinValue.High);
                    break;
                case PinEventTypes.Falling:
                    SetPinStatus(PinValue.Low);
                    break;
            }
        }
    }
}