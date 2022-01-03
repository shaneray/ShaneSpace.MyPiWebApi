using Serilog;
using System;
using System.Collections.Generic;
using System.Device.Gpio;

namespace ShaneSpace.MyPiWebApi.Models.Components
{
    public abstract class BaseGpioComponent : IGpioComponent
    {
        protected GpioController GpioController { get; }
        protected ILogger Logger { get; }
        protected virtual int MaxAttributeChangeHistory => 5;

        private readonly Queue<GenericEvent> _attributeChangeHistory;

        protected BaseGpioComponent(GpioController gpioController, ILogger logger)
        {
            GpioController = gpioController;
            Logger = logger?.ForContext(GetType());

            _attributeChangeHistory = new Queue<GenericEvent>(20);
            _attributeChangeHistory.Enqueue(new GenericEvent
            {
                EventStart = DateTimeOffset.Now,
                EventEnd = DateTimeOffset.Now,
                Message = $"{Type} initialized"
            });
        }

        public virtual string Name => GetType().ToString();
        public string Type => GetType().ToString();
        public Dictionary<string, int> Pins { get; set; } = new();
        public Dictionary<string, string> Attributes { get; set; } = new();
        public IReadOnlyCollection<GenericEvent> History => _attributeChangeHistory.ToArray();

        public event EventHandler<GpioAttributeUpdatedEventArgs> AttributeUpdated;

        protected void InitializeAttribute(string key, string value)
        {
            Attributes[key] = value;
        }

        protected void UpdateAttribute(string key, string value)
        {
            Attributes.TryGetValue(key, out var originalValue);
            originalValue ??= "null";
            if (originalValue == value)
            {
                return;
            }

            Attributes[key] = value;
            OnAttributeUpdated(new GpioAttributeUpdatedEventArgs
            {
                AttributeKey = key,
                OriginalValue = originalValue,
                NewValue = value
            });
        }

        protected virtual void OnAttributeUpdated(GpioAttributeUpdatedEventArgs eventArgs)
        {
            LogAttributeChange(eventArgs);
            AddAttributeChangeHistory(eventArgs);

            AttributeUpdated?.Invoke(this, eventArgs);
        }

        private void AddAttributeChangeHistory(GpioAttributeUpdatedEventArgs eventArgs)
        {
            if (_attributeChangeHistory.Count == MaxAttributeChangeHistory)
            {
                _attributeChangeHistory.Dequeue();
            }

            var now = DateTimeOffset.Now;
            _attributeChangeHistory.Enqueue(new GenericEvent
            {
                EventStart = now,
                EventEnd = now,
                Message = $"{eventArgs.AttributeKey} changed from {eventArgs.OriginalValue} to {eventArgs.NewValue}."
            });
        }

        protected virtual void LogAttributeChange(GpioAttributeUpdatedEventArgs eventArgs)
        {
            Logger?.Information(
                "Component {ComponentName} attribute {Attribute} changed value from {OriginalValue} to {NewValue}.",
                Name, eventArgs.AttributeKey, eventArgs.OriginalValue, eventArgs.NewValue);
        }
    }
}