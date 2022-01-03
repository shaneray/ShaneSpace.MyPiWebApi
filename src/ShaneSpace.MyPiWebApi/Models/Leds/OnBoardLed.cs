using Serilog;
using ShaneSpace.MyPiWebApi.Models.Components;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ShaneSpace.MyPiWebApi.Models.Leds
{
    public class OnBoardLed : BaseGpioComponent, ILed
    {
        private readonly string _ledDirectory;

        public OnBoardLed(int ledIndex, string ledName, ILogger logger) : base(null, logger)
        {
            LedIndex = ledIndex;
            _ledDirectory = $"/sys/class/leds/led{LedIndex}";
            Name = string.IsNullOrWhiteSpace(ledName) ? $"OnBoard{LedIndex}" : ledName;
            AvailableTriggers = GetAvailableTriggers();

            InitializeAttribute("Status", IsOn ? "On" : "Off");
        }
        public override string Name { get; }

        public int LedIndex { get; }
        public string Trigger => GetCurrentTrigger();
        public IEnumerable<string> AvailableTriggers { get; }
        public int Brightness
        {
            get => 255;
            set { }
        }

        public bool IsOn
        {
            get => int.Parse(File.ReadAllText($"{_ledDirectory}/brightness")) > 0;
            set => SetLedStatus(value);
        }

        private void SetLedStatus(bool isOn)
        {
            UpdateAttribute("Status", isOn ? "On" : "Off");
            File.WriteAllText($"{_ledDirectory}/brightness", isOn ? "1" : "0");
        }

        private string GetCurrentTrigger()
        {
            var currentTriggerString = File.ReadAllText($"{_ledDirectory}/trigger").Trim();
            var currentTrigger = Regex.Match(currentTriggerString, @"\[.+?\]");
            return string.IsNullOrWhiteSpace(currentTrigger.Value)
                ? currentTriggerString
                : currentTrigger.Value
                    .Replace("[", string.Empty)
                    .Replace("]", string.Empty);
        }

        private IEnumerable<string> GetAvailableTriggers()
        {
            return File.ReadAllText($"{_ledDirectory}/trigger")
                .Replace("[", string.Empty)
                .Replace("]", string.Empty)
                .Trim()
                .Split(' ');
        }
    }
}