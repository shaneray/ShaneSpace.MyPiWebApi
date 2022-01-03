using Iot.Device.Graphics;
using Iot.Device.Ws28xx;
using System.Device.Spi;
using System.Drawing;

namespace ShaneSpace.MyPiWebApi.Models.Leds
{
    public static class GpioRainbowLed
    {
        public static SpiConnectionSettings settings = new(0, 0)
        {
            ClockFrequency = 2_400_000,
            Mode = SpiMode.Mode0,
            DataBitLength = 8
        };
        public static SpiDevice spi = SpiDevice.Create(settings);
        public static Ws28xx neo = new Ws2808(spi, 15);
        public static void DoIt()
        {
            BitmapImage img = neo.Image;
            img.SetPixel(1, 0, Color.Blue);
            neo.Update();
        }
    }
}