using System.Device.Gpio;

namespace ShaneSpace.MyPiWebApi.Services
{
    /// <summary>
    /// The GPIO Service
    /// </summary>
    public class GpioService : IGpioService
    {
        public GpioController Controller { get; }
        private bool _disposedValue;

        public GpioService()
        {
            try
            {
                Controller = new GpioController();
            }
            catch
            {
                // swallow exception
            }
        }

        public void Dispose()
        {
            if (_disposedValue)
            {
                return;
            }

            Controller.Dispose();
            _disposedValue = true;
        }
    }
}
