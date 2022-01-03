using System;
using System.Device.Gpio;

namespace ShaneSpace.MyPiWebApi.Services
{
    public interface IGpioService : IDisposable
    {
        GpioController Controller { get; }
    }
}