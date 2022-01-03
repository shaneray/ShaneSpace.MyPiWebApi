using Serilog;
using ShaneSpace.MyPiWebApi.Models;
using ShaneSpace.MyPiWebApi.Models.Buttons;
using ShaneSpace.MyPiWebApi.Models.Leds;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ShaneSpace.MyPiWebApi.Services
{
    public class BaseRaspberryPiService : IRaspberryPiService
    {
        protected virtual string RaspberryPiName { get; }

        protected readonly IGpioService GpioService;
        protected readonly ILogger Logger;
        protected List<ILed> Leds { get; } = new();
        protected List<IButton> Buttons { get; } = new();
        public Camera Camera { get; } = new();

        public BaseRaspberryPiService(IGpioService gpioService, ILogger logger)
        {
            GpioService = gpioService;
            Logger = logger.ForContext(GetType());
            RaspberryPiName = GetType().Name;

            var onBoardLeds = new[]
            {
                new OnBoardLed(1, "Power LED", logger),
                new OnBoardLed(0, "Activity LED", logger)
            };
            Leds.AddRange(onBoardLeds);
        }

        public RaspberryPiInfo GetInfo()
        {
            return new()
            {
                Name = RaspberryPiName,
                Leds = GetLeds(),
                Buttons = GetButtons()
            };
        }

        public IReadOnlyCollection<ILed> GetLeds()
        {
            return Leds.AsReadOnly();
        }

        public IReadOnlyCollection<IButton> GetButtons()
        {
            return Buttons.AsReadOnly();
        }

        public ILed GetLedByIndex(int index)
        {
            var ledByIndex = Leds
                .SingleOrDefault(x => x.LedIndex == index);
            if (ledByIndex == null)
            {
                throw new Exception($"Could not locate LED at index {index}");
            }
            return ledByIndex;
        }

        public ProcessResult Shutdown()
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sudo",
                    ArgumentList = { "shutdown", "now" },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            var output = new ProcessResult();

            process.Exited += (sender, args) =>
            {
                output.StandardOutput.AddRange(process.StandardOutput.ReadToEnd().Split(Environment.NewLine));
                output.StandardError.AddRange(process.StandardError.ReadToEnd().Split(Environment.NewLine));
            };

            process.Start();
            process.WaitForExit();
            return output;
        }

        public ProcessResult Restart()
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sudo",
                    ArgumentList = { "reboot" },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            var output = new ProcessResult();

            process.Exited += (sender, args) =>
            {
                output.StandardOutput.AddRange(process.StandardOutput.ReadToEnd().Split(Environment.NewLine));
                output.StandardError.AddRange(process.StandardError.ReadToEnd().Split(Environment.NewLine));
            };

            process.Start();

            return output;
        }
    }
}