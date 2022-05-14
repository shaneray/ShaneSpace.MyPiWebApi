using System;
using System.Device.I2c;
using System.Linq;
using System.Timers;
using System.IO;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using Iot.Device.Ssd13xx;
using Iot.Device.Ssd13xx.Commands;
using System.Numerics;
using Serilog;
using Iot.Device.Ssd13xx.Commands.Ssd1306Commands;
using System.Threading;
using System.Collections.Generic;

namespace ShaneSpace.MyPiWebApi.Models.Displays
{
    // https://cdn-shop.adafruit.com/datasheets/SSD1306.pdf
    public partial class Ssd1306Display : IDisposable
    {
        private Ssd1306 _display;
        private readonly ILogger _logger;
        private readonly int _rows;
        private readonly int _columns;
        private readonly PageAddress _maxPageAddress;
        private byte[] _buffer;
        private int _contrast = 255;
        private int _preDimContrast;
        private int dimSeconds = 45;
        private int sleepSeconds = 60;
        private DisplayState _state;
        private System.Timers.Timer activityTimer;
        private FontFamily _defaultFontFamily;
        private IEnumerable<FontFamily> _fonts;

        public Ssd1306Display(ILogger logger, int columns = 128, int rows = 64)
        {
            _logger=logger;
            _rows = rows;
            _columns = columns;
            _maxPageAddress=rows>32 ? PageAddress.Page7 : PageAddress.Page3;

            var size = _columns * ((_rows + 7) / 8);
            _buffer = new byte[size];

            if (dimSeconds > 0 || sleepSeconds > 0)
            {
                activityTimer = new System.Timers.Timer(TimeSpan.FromSeconds(dimSeconds).TotalMilliseconds);
                activityTimer.Elapsed += ActivityTimer_Elapsed;
            }

            InitializeDisplay();
            InitializeFonts();
            ClearScreen();
            SendMessage("Online and waiting for command...");
        }

        private void InitializeFonts()
        {
            _defaultFontFamily = SystemFonts.Families.FirstOrDefault(x => x.Name == "DejaVu Sans Mono") ?? SystemFonts.Families.First();
            var customFonts = new FontCollection();
            foreach (var item in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Fonts"), "*.ttf"))
            {
                customFonts.Install(item);
            }

            _fonts = SystemFonts.Families.Union(customFonts.Families);

            Console.WriteLine("Font List:");
            Console.WriteLine(string.Join(Environment.NewLine, _fonts.Select(x => x.Name)));
        }

        private void ActivityReset()
        {
            if (_state == DisplayState.Dim)
            {
                UnDim();
            }

            if (_state == DisplayState.Sleep)
            {
                Wake();
            }

            if (dimSeconds > 0 || sleepSeconds > 0)
            {
                activityTimer.Stop();
                activityTimer.Interval = TimeSpan.FromSeconds(dimSeconds).TotalMilliseconds;
                activityTimer.Start();
            }
        }

        private void ActivityTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_state != DisplayState.Dim)
            {
                _logger.Information("ActivityTimer_Elapsed: Dim...");
                Dim();
                if (sleepSeconds > 0)
                {
                    activityTimer.Interval = TimeSpan.FromSeconds(sleepSeconds).TotalMilliseconds;
                }
                else
                {
                    activityTimer.Stop();
                }
            }
            else if (_state != DisplayState.Sleep)
            {
                _logger.Information("ActivityTimer_Elapsed: Sleep...");
                Sleep();
                activityTimer.Stop();
            }
            else
            {
                _logger.Information($"ActivityTimer_Elapsed: DisplayState - {_state}");
            }
        }

        public void HelloWorld()
        {
            SendMessage("Hello .NET IoT!!!");
            //DisplayClock();
        }

        I2cDevice GetI2CDevice()
        {
            I2cConnectionSettings connectionSettings = new(1, 0x3C);
            return I2cDevice.Create(connectionSettings);
        }

        Ssd1306 GetSsd1306WithI2c()
        {
            return new Ssd1306(GetI2CDevice());
        }

        void InitializeDisplay()
        {
            _state = DisplayState.Off;
            _display = GetSsd1306WithI2c();
            _display.SendCommand(new SetDisplayOff());
            _display.SendCommand(new SetDisplayClockDivideRatioOscillatorFrequency(0x00, 0x08));
            _display.SendCommand(new SetMultiplexRatio(63));
            _display.SendCommand(new SetDisplayOffset(0x00));
            _display.SendCommand(new SetDisplayStartLine(0x00));
            _display.SendCommand(new SetChargePump(true));
            _display.SendCommand(new SetMemoryAddressingMode(SetMemoryAddressingMode.AddressingMode.Horizontal));
            _display.SendCommand(new SetSegmentReMap(true));
            _display.SendCommand(new SetComOutputScanDirection(false));
            _display.SendCommand(new SetComPinsHardwareConfiguration(true, false));
            _display.SendCommand(new SetContrastControlForBank0((byte)_contrast));
            _display.SendCommand(new SetVcomhDeselectLevel(SetVcomhDeselectLevel.DeselectLevel.Vcc0_65));
            _display.SendCommand(new EntireDisplayOn(false));
            _display.SendCommand(new SetNormalDisplay());
            _display.SendCommand(new SetDisplayOn());
            _state = DisplayState.On;
            _logger.Information("Display initialized...");
        }

        public void ClearScreen()
        {
            ActivityReset();
            _buffer = new byte[_buffer.Length];
            SendBuffer();
        }

        public void SendMessage(string message, int fontSize = 12)
        {
            FancyText(message, fontSize);
        }

        private void FancyText(string message, int fontSize = 12)
        {
            var y = 0;
            var fontName = "Cascadia Code";
            var fontFamily = _defaultFontFamily;
            try
            {
                fontFamily = _fonts.FirstOrDefault(x => x.Name == fontName) ?? _defaultFontFamily;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Font error: {ex.Message}");
                Console.WriteLine($"Available Fonts: {string.Join(", ", SystemFonts.Families.Select(x => x.Name))}");
            }
            var font = fontFamily.CreateFont(fontSize, FontStyle.Regular);

            using (var image = new Image<Rgba32>(_columns, _rows))
            {
                if (image.TryGetSinglePixelSpan(out Span<Rgba32> imageSpan))
                {
                    imageSpan.Fill(Color.Black);
                }
                float padding = 0f;
                float textMaxWidth = image.Width - (padding * 2); // width of image indent left & right by padding

                var letterSize = TextMeasurer.Measure("W", new RendererOptions(font));
                _logger.Information($"TextMaxWidth: {textMaxWidth}");
                _logger.Information($"FontName: {font.Name}");
                _logger.Information($"FontSize: {fontSize}");
                _logger.Information($"Width: {letterSize.Width}");
                _logger.Information($"Height: {letterSize.Height}");
                _logger.Information($"Columns: {Math.Round(textMaxWidth/letterSize.Width, 0)}");
                _logger.Information($"Rows: {Math.Round(_rows/letterSize.Height+4, 0)}");

                Vector2 topLeftLocation = new Vector2(padding, padding);
                var options = new TextGraphicsOptions
                {
                    TextOptions = new TextOptions
                    {
                        WrapTextWidth = textMaxWidth,
                        HorizontalAlignment = HorizontalAlignment.Center
                    }
                };
                image.Mutate(ctx => ctx
                    .DrawText(options, message, font, Color.White, topLeftLocation));
                _logger.Information($"ImgHeight: {image.Height}");
                _logger.Information($"ImgWidth: {image.Width}");
                //image.SaveAsPng("/home/pi/Desktop/tmp");
                using (Image<L16> image_t = image.CloneAs<L16>())
                {
                    _display.SendCommand(new SetColumnAddress());
                    _display.SendCommand(new SetPageAddress(PageAddress.Page0, _maxPageAddress));
                    DisplayImage(image_t);
                }
            }
        }

        public void Demo()
        {
            ClearScreen();
            _display.SendCommand(new SetColumnAddress());

            // rows of icons
            var page = 0;
            while (page < (int)_maxPageAddress+1)
            {
                _display.SendCommand(new SetPageAddress((PageAddress)page, _maxPageAddress));

                _display.SendData(skull);
                Thread.Sleep(30);
                _display.SendData(heart);
                Thread.Sleep(30);
                _display.SendData(clock);
                Thread.Sleep(30);
                _display.SendData(skull);
                Thread.Sleep(30);
                _display.SendData(heart);
                Thread.Sleep(30);
                _display.SendData(clock);
                Thread.Sleep(30);
                _display.SendData(skull);
                Thread.Sleep(30);
                _display.SendData(heart);
                Thread.Sleep(30);
                _display.SendData(skull);
                Thread.Sleep(30);
                _display.SendData(heart);
                Thread.Sleep(30);
                _display.SendData(clock);
                Thread.Sleep(30);
                _display.SendData(skull);
                Thread.Sleep(30);
                _display.SendData(heart);
                Thread.Sleep(30);
                _display.SendData(clock);
                Thread.Sleep(30);
                _display.SendData(skull);
                Thread.Sleep(30);
                _display.SendData(heart);
                Thread.Sleep(30);

                ClearScreen();
                Thread.Sleep(100);
                page++;
            }

            // test pattern
            ClearScreen();
            DrawTestPattern();
            Thread.Sleep(1000);

            // squares filling screen
            ClearScreen();
            DrawSquare(1, 1, 32);
            SendBuffer();
            Thread.Sleep(500);
            DrawSquare(32, 1, 32);
            SendBuffer();
            Thread.Sleep(500);
            DrawSquare(64, 1, 32);
            SendBuffer();
            Thread.Sleep(500);
            DrawSquare(96, 1, 32);
            SendBuffer();
            Thread.Sleep(500);
            DrawSquare(1, 32, 32);
            SendBuffer();
            Thread.Sleep(500);
            DrawSquare(32, 32, 32);
            SendBuffer();
            Thread.Sleep(500);
            DrawSquare(64, 32, 32);
            SendBuffer();
            Thread.Sleep(500);
            DrawSquare(96, 32, 32);
            SendBuffer();
            Thread.Sleep(1000);

            // fill squares
            DrawFilledSquare(1, 1, 32);
            SendBuffer();
            Thread.Sleep(500);
            DrawFilledSquare(32, 1, 32);
            SendBuffer();
            Thread.Sleep(500);
            DrawFilledSquare(64, 1, 32);
            SendBuffer();
            Thread.Sleep(500);
            DrawFilledSquare(96, 1, 32);
            SendBuffer();
            Thread.Sleep(500);
            DrawFilledSquare(1, 32, 32);
            SendBuffer();
            Thread.Sleep(500);
            DrawFilledSquare(32, 32, 32);
            SendBuffer();
            Thread.Sleep(500);
            DrawFilledSquare(64, 32, 32);
            SendBuffer();
            Thread.Sleep(500);
            DrawFilledSquare(96, 32, 32);
            SendBuffer();
            Thread.Sleep(1000);

            // rectangles filling screen
            ClearScreen();
            DrawRectangle(1, 1, 32, 64);
            SendBuffer();
            Thread.Sleep(500);
            DrawRectangle(32, 1, 32, 64);
            SendBuffer();
            Thread.Sleep(500);
            DrawRectangle(64, 1, 32, 64);
            SendBuffer();
            Thread.Sleep(500);
            DrawRectangle(96, 1, 32, 64);
            SendBuffer();
            Thread.Sleep(1000);

            // fill rectangles
            DrawFilledRectangle(1, 1, 32, 64);
            SendBuffer();
            Thread.Sleep(500);
            DrawFilledRectangle(32, 1, 32, 64);
            SendBuffer();
            Thread.Sleep(500);
            DrawFilledRectangle(64, 1, 32, 64);
            SendBuffer();
            Thread.Sleep(500);
            DrawFilledRectangle(96, 1, 32, 64);
            SendBuffer();
            Thread.Sleep(1000);

            // horizontal lines
            ClearScreen();
            DrawHorizontalLine(1, 1, 64);
            SendBuffer();
            DrawHorizontalLine(1, 32, 64);
            SendBuffer();
            DrawHorizontalLine(1, 64, 64);
            SendBuffer();
            Thread.Sleep(1000);

            ClearScreen();
            DrawHorizontalLine(64, 1, 64);
            SendBuffer();
            DrawHorizontalLine(64, 32, 64);
            SendBuffer();
            DrawHorizontalLine(64, 64, 64);
            SendBuffer();
            Thread.Sleep(1000);

            // vertical lines
            ClearScreen();
            DrawVerticalLine(1, 1, 32);
            SendBuffer();
            DrawVerticalLine(32, 1, 32);
            SendBuffer();
            DrawVerticalLine(64, 1, 32);
            SendBuffer();
            DrawVerticalLine(96, 1, 32);
            SendBuffer();
            DrawVerticalLine(128, 1, 32);
            SendBuffer();
            Thread.Sleep(1000);

            ClearScreen();
            DrawVerticalLine(1, 32, 32);
            SendBuffer();
            DrawVerticalLine(32, 32, 32);
            SendBuffer();
            DrawVerticalLine(64, 32, 32);
            SendBuffer();
            DrawVerticalLine(96, 32, 32);
            SendBuffer();
            DrawVerticalLine(128, 32, 32);
            SendBuffer();
            Thread.Sleep(1000);

            // vertical and horizontal lines
            ClearScreen();
            DrawHorizontalLine(1, 1, 64);
            DrawHorizontalLine(1, 32, 64);
            DrawHorizontalLine(1, 64, 64);

            DrawHorizontalLine(64, 1, 64);
            DrawHorizontalLine(64, 32, 64);
            DrawHorizontalLine(64, 64, 64);

            DrawVerticalLine(1, 1, 32);
            DrawVerticalLine(32, 1, 32);
            DrawVerticalLine(64, 1, 32);
            DrawVerticalLine(96, 1, 32);
            DrawVerticalLine(128, 1, 32);

            DrawVerticalLine(1, 32, 32);
            DrawVerticalLine(32, 32, 32);
            DrawVerticalLine(64, 32, 32);
            DrawVerticalLine(96, 32, 32);
            DrawVerticalLine(128, 32, 32);
            SendBuffer();
            Thread.Sleep(1000);

            // triangles
            ClearScreen();
            DrawTriangle(_columns/2, _rows/2, _rows/4);
            SendBuffer();
            Thread.Sleep(500);
            DrawTriangle(_columns/2-_rows/4/2, _rows/2-_rows/4/2, _columns/2+_rows/4/2, _rows/2-_rows/4/2, _columns/2, _rows/2+_rows/4/2);
            SendBuffer();
            Thread.Sleep(500);
            DrawTriangle(1, 1, _rows/2);
            SendBuffer();
            Thread.Sleep(500);
            DrawTriangle(1, _rows, _rows/2);
            SendBuffer();
            Thread.Sleep(500);
            DrawTriangle(_columns, 1, _rows/2);
            SendBuffer();
            Thread.Sleep(500);
            DrawTriangle(_columns, _rows, _rows/2);
            SendBuffer();
            Thread.Sleep(500);
            DrawTriangle(_columns/2, _rows/2, _rows);
            SendBuffer();
            Thread.Sleep(500);
            DrawTriangle(1, 1, _columns, 1, _columns/2, _rows);
            SendBuffer();
            Thread.Sleep(1000);

            // fill triangles
            DrawFilledTriangle(_columns/2, _rows/2, _rows/4);
            SendBuffer();
            Thread.Sleep(500);
            DrawFilledTriangle(_columns/2-_rows/4/2, _rows/2-_rows/4/2, _columns/2+_rows/4/2, _rows/2-_rows/4/2, _columns/2, _rows/2+_rows/4/2);
            SendBuffer();
            Thread.Sleep(500);
            DrawFilledTriangle(1, 1, _rows/2);
            SendBuffer();
            Thread.Sleep(500);
            DrawFilledTriangle(1, _rows, _rows/2);
            SendBuffer();
            Thread.Sleep(500);
            DrawFilledTriangle(_columns, 1, _rows/2);
            SendBuffer();
            Thread.Sleep(500);
            DrawFilledTriangle(_columns, _rows, _rows/2);
            SendBuffer();
            Thread.Sleep(500);
            DrawFilledTriangle(_columns/2, _rows/2, _rows);
            SendBuffer();
            Thread.Sleep(500);
            DrawFilledTriangle(1, 1, _columns, 1, _columns/2, _rows);
            SendBuffer();
            Thread.Sleep(1000);

            // circles
            ClearScreen();
            DrawCircle(_columns/2, _rows/2, _rows/4);
            SendBuffer();
            Thread.Sleep(500);
            DrawCircle(1, 1, _rows/2);
            SendBuffer();
            Thread.Sleep(500);
            DrawCircle(_columns, _rows, _rows/2);
            SendBuffer();
            Thread.Sleep(1000);

            // fill circles
            DrawFilledCircle(_columns/2, _rows/2, _rows/4);
            SendBuffer();
            Thread.Sleep(500);
            DrawFilledCircle(1, 1, _rows/2);
            SendBuffer();
            Thread.Sleep(500);
            DrawFilledCircle(_columns, _rows, _rows/2);
            SendBuffer();
            Thread.Sleep(1000);

            // a bit of everything...
            ClearScreen();
            DrawTriangle(_columns/2, _rows/2, _rows/4);
            SendBuffer();
            Thread.Sleep(500);
            DrawSquare(_columns/2-_rows/8, _rows/2-_rows/8, _rows/4);
            SendBuffer();
            Thread.Sleep(500);
            DrawCircle(1, 1, _rows/2);
            SendBuffer();
            Thread.Sleep(500);
            DrawCircle(_columns, _rows, _rows/2);
            SendBuffer();
            Thread.Sleep(500);
            DrawSquare(_columns-_rows/4, 1-_rows/4, _rows/2);
            SendBuffer();
            Thread.Sleep(500);
            DrawSquare(1-_rows/4, _rows-_rows/4, _rows/2);
            SendBuffer();
            DrawLine(1, 1, _columns, 1);
            SendBuffer();
            Thread.Sleep(500);
            DrawLine(_columns, 1, _columns, _rows);
            SendBuffer();
            Thread.Sleep(500);
            DrawLine(_columns, _rows, 1, _rows);
            SendBuffer();
            Thread.Sleep(500);
            DrawLine(1, _rows, 1, 1);
            SendBuffer();
            Thread.Sleep(500);
            DrawLine(1, 1, _columns, _rows);
            SendBuffer();
            Thread.Sleep(500);
            DrawLine(_columns, 1, 1, _rows);
            SendBuffer();
            Thread.Sleep(1000);

            DrawTestPattern();
            Thread.Sleep(1000);
        }

        private void DisplayImage(Image<L16> image)
        {
            ActivityReset();
            var width = _columns;
            var pages = (int)_maxPageAddress;
            List<byte> buffer = new();

            for (int page = 0; page <= pages; page++)
            {
                for (int x = 0; x < width; x++)
                {
                    int bits = 0;
                    for (byte bit = 0; bit < 8; bit++)
                    {
                        bits = bits << 1;
                        bits |= image[x, page * 8 + 7 - bit].PackedValue > 0 ? 1 : 0;
                    }

                    buffer.Add((byte)bits);
                }
            }

            int chunk_size = 16;
            for (int i = 0; i < buffer.Count; i += chunk_size)
            {
                _display.SendData(buffer.Skip(i).Take(chunk_size).ToArray());
            }
        }

        public void DisplayClock()
        {
            ClearScreen();
            var fontSize = 25;

            foreach (var i in Enumerable.Range(0, 100))
            {
                SendMessage(DateTime.Now.ToString("HH:mm:ss"), fontSize);
            }
        }

        // 8px by 8px, column major, little endian.
        byte[] skull = new byte[] { 0x00, 0x1c, 0x72, 0x5e, 0x5e, 0x72, 0x1c, 0x00 };
        byte[] heart = new byte[] { 0x00, 0x1c, 0x3e, 0x7c, 0x7c, 0x3e, 0x1c, 0x00 };
        byte[] clock = new byte[] { 0x00, 0x3c, 0x42, 0x5a, 0x52, 0x42, 0x3c, 0x00 };

        public void DisplayAlien()
        {
            DisplayImage(Path.Combine(AppContext.BaseDirectory, "Images", "alien-black-on-white.png"));
            Thread.Sleep(1000);
            DisplayImage(Path.Combine(AppContext.BaseDirectory, "Images", "alien-white-on-black.png"));
        }

        public void DisplayImage(string path)
        {
            using var image = Image.Load<L16>(path);
            DisplayImage(image);
        }

        private void DrawTestPattern()
        {
            _logger.Information("Display: test pattern...");
            SetPixel(1, 1, true);
            SetPixel(_columns, 1, true);
            SetPixel(1, _rows, true);
            SetPixel(_columns, _rows, true);
            SetPixel(_columns/2, _rows/2, true);
            SendBuffer();
        }

        public void DrawRectangle(int x, int y, int w, int h)
        {
            _logger.Information("DrawRectangle: x({x}), y({y}), w({w}), h({h})", x, y, w, h);
            DrawHorizontalLine(x, y, w);
            DrawHorizontalLine(x, y + h - 1, w);
            DrawVerticalLine(x, y, h);
            DrawVerticalLine(x + w - 1, y, h);
        }

        public void DrawFilledRectangle(int x, int y, int w, int h)
        {
            _logger.Information("DrawFilledRectangle: x({x}), y({y}), w({w}), h({h})", x, y, w, h);

            for (int i = x; i < x + w; i++)
            {
                DrawVerticalLine(i, y, h);
            }
        }

        public void DrawSquare(int x, int y, int w)
        {
            DrawRectangle(x, y, w, w);
        }

        public void DrawFilledSquare(int x, int y, int w)
        {
            DrawFilledRectangle(x, y, w, w);
        }

        public void DrawCircle(int x, int y, int r)
        {
            _logger.Information("DrawCircle: x({x}), y({y}), r({r})", x, y, r);

            int f = 1 - r;
            int ddF_x = 1;
            int ddF_y = -2 * r;
            int xt = 0;
            int yt = r;
            var color = true;
            SetPixel(x, y + r, color);
            SetPixel(x, y - r, color);
            SetPixel(x + r, y, color);
            SetPixel(x - r, y, color);

            while (xt < yt)
            {
                if (f >= 0)
                {
                    yt--;
                    ddF_y += 2;
                    f += ddF_y;
                }
                xt++;
                ddF_x += 2;
                f += ddF_x;

                SetPixel(x + xt, y + yt, color);
                SetPixel(x - xt, y + yt, color);
                SetPixel(x + xt, y - yt, color);
                SetPixel(x - xt, y - yt, color);
                SetPixel(x + yt, y + xt, color);
                SetPixel(x - yt, y + xt, color);
                SetPixel(x + yt, y - xt, color);
                SetPixel(x - yt, y - xt, color);
            }
        }

        public void DrawFilledCircle(int x, int y, int r)
        {
            _logger.Information("DrawFilledCircle: x({x}), y({y}), r({r})", x, y, r);

            DrawVerticalLine(x, y - r, 2 * r + 1);
            FillCircleHelper(x, y, r, 3, 0);
        }

        public void DrawTriangle(int x, int y, int size)
        {
            DrawTriangle(x, y, size, size);
        }

        public void DrawTriangle(int x, int y, int w, int h)
        {
            var x0 = x;
            var y0 = y - h/2;
            var x1 = x - w/2;
            var y1 = y + h/2;
            var x2 = x + w/2;
            var y2 = y1;
            DrawTriangle(x0, y0, x1, y1, x2, y2);
        }

        public void DrawTriangle(int x0, int y0, int x1, int y1, int x2, int y2)
        {
            _logger.Information("DrawTriangle: x0({x0}), y0({y0}), x1({x1}), y1({y1}), x2({x2}), y2({y2})", x0, y0, x1, y1, x2, y2);
            DrawLine(x0, y0, x1, y1);
            DrawLine(x1, y1, x2, y2);
            DrawLine(x2, y2, x0, y0);
        }

        public void DrawFilledTriangle(int x, int y, int size)
        {
            DrawFilledTriangle(x, y, size, size);
        }

        public void DrawFilledTriangle(int x, int y, int w, int h)
        {
            var x0 = x;
            var y0 = y - h/2;
            var x1 = x - w/2;
            var y1 = y + h/2;
            var x2 = x + w/2;
            var y2 = y1;
            DrawFilledTriangle(x0, y0, x1, y1, x2, y2);
        }

        public void DrawFilledTriangle(int x0, int y0, int x1, int y1, int x2, int y2)
        {
            _logger.Information("DrawTriangle: x0({x0}), y0({y0}), x1({x1}), y1({y1}), x2({x2}), y2({y2})", x0, y0, x1, y1, x2, y2);

            int a, b, y, last;

            // Sort coordinates by Y order (y2 >= y1 >= y0)
            if (y0 > y1)
            {
                SwapInt(ref y0, ref y1);
                SwapInt(ref x0, ref x1);
            }
            if (y1 > y2)
            {
                SwapInt(ref y2, ref y1);
                SwapInt(ref x2, ref x1);
            }
            if (y0 > y1)
            {
                SwapInt(ref y0, ref y1);
                SwapInt(ref x0, ref x1);
            }

            if (y0 == y2)
            { // Handle awkward all-on-same-line case as its own thing
                a = b = x0;
                if (x1 < a)
                    a = x1;
                else if (x1 > b)
                    b = x1;
                if (x2 < a)
                    a = x2;
                else if (x2 > b)
                    b = x2;
                DrawHorizontalLine(a, y0, b - a + 1);
                return;
            }

            int dx01 = x1 - x0, dy01 = y1 - y0, dx02 = x2 - x0, dy02 = y2 - y0,
                    dx12 = x2 - x1, dy12 = y2 - y1;
            int sa = 0, sb = 0;

            // For upper part of triangle, find scanline crossings for segments
            // 0-1 and 0-2.  If y1=y2 (flat-bottomed triangle), the scanline y1
            // is included here (and second loop will be skipped, avoiding a /0
            // error there), otherwise scanline y1 is skipped here and handled
            // in the second loop...which also avoids a /0 error here if y0=y1
            // (flat-topped triangle).
            if (y1 == y2)
                last = y1; // Include y1 scanline
            else
                last = y1 - 1; // Skip it

            for (y = y0; y <= last; y++)
            {
                a = x0 + sa / dy01;
                b = x0 + sb / dy02;
                sa += dx01;
                sb += dx02;
                /* longhand:
                a = x0 + (x1 - x0) * (y - y0) / (y1 - y0);
                b = x0 + (x2 - x0) * (y - y0) / (y2 - y0);
                */
                if (a > b)
                {
                    SwapInt(ref a, ref b);
                }
                DrawHorizontalLine(a, y, b - a + 1);
            }

            // For lower part of triangle, find scanline crossings for segments
            // 0-2 and 1-2.  This loop is skipped if y1=y2.
            sa = dx12 * (y - y1);
            sb = dx02 * (y - y0);
            for (; y <= y2; y++)
            {
                a = x1 + sa / dy12;
                b = x0 + sb / dy02;
                sa += dx12;
                sb += dx02;
                /* longhand:
                a = x1 + (x2 - x1) * (y - y1) / (y2 - y1);
                b = x0 + (x2 - x0) * (y - y0) / (y2 - y0);
                */
                if (a > b)
                {
                    SwapInt(ref a, ref b);
                }
                DrawHorizontalLine(a, y, b - a + 1);
            }
        }

        public void DrawHorizontalLine(int x, int y, int w)
        {
            //_logger.Information($"Display: DrawHorizontalLine - x({x}), y({y}), w({w})...");
            for (int i = x; i < x+w; i++)
            {
                SetPixel(i, y, true);
            }
        }

        public void DrawVerticalLine(int x, int y, int w)
        {
            //_logger.Information($"Display: DrawVerticalLine - x({x}), y({y}), w({w})...");
            for (int i = y; i < y+w; i++)
            {
                SetPixel(x, i, true);
            }
        }

        public void DrawLine(int x0, int y0, int x1, int y1)
        {
            if (x0 == x1)
            {
                if (y0 > y1)
                {
                    SwapInt(ref y0, ref y1);
                }
                DrawVerticalLine(x0, y0, y1 - y0 + 1);
            }
            else if (y0 == y1)
            {
                if (x0 > x1)
                {
                    SwapInt(ref x0, ref x1);
                }
                DrawHorizontalLine(x0, y0, x1 - x0 + 1);
            }
            else
            {
                _logger.Information("DrawLine: x0({x0}), y0({y0}), x1({x1}), y1({y1})", x0, y0, x1, y1);

                var steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
                if (steep)
                {
                    SwapInt(ref x0, ref y0);
                    SwapInt(ref x1, ref y1);
                }

                if (x0 > x1)
                {
                    SwapInt(ref x0, ref x1);
                    SwapInt(ref y0, ref y1);
                }

                int dx, dy;
                dx = x1 - x0;
                dy = Math.Abs(y1 - y0);

                int err = dx / 2;
                int ystep;

                if (y0 < y1)
                {
                    ystep = 1;
                }
                else
                {
                    ystep = -1;
                }

                for (; x0 <= x1; x0++)
                {
                    if (steep)
                    {
                        SetPixel(y0, x0, true);
                    }
                    else
                    {
                        SetPixel(x0, y0, true);
                    }
                    err -= dy;
                    if (err < 0)
                    {
                        y0 += ystep;
                        err += dx;
                    }
                }
            }
        }

        public void SetContrast(double percentage)
        {
            if (percentage < 0)
            {
                percentage = 0;
            }
            if (percentage > 100)
            {
                percentage = 100;
            }

            var newContrast = Convert.ToByte(Math.Round(percentage*.01*255, 0));
            var fadeDurationMs = 500;
            var contrastDiff = Math.Abs(_contrast-newContrast);
            var interval = 0;
            if (contrastDiff > 0)
            {
                interval = fadeDurationMs / contrastDiff;
            }
            _logger.Information($"Set Display Contrast to: {newContrast} ({percentage}%)");
            while (_contrast != newContrast)
            {
                int tmp = _contrast;
                if (_contrast > newContrast)
                {
                    tmp--;
                }
                else
                {
                    tmp++;
                }

                _contrast = tmp;
                _display.SendCommand(new SetContrastControlForBank0((byte)tmp));
                Thread.Sleep(interval);
            }
        }

        public void SetPixel(int x, int y, bool isOn)
        {
            x--;
            y--;
            if (x < 0 || y < 0 || x > _columns-1 || y > _rows-1)
            {
                return;
            }
            //_logger.Information($"SetPixel: x({x}),y({y}),isOn({isOn})");
            try
            {
                if (isOn)
                {
                    _buffer[x + (y / 8) * _columns] |= Convert.ToByte(1 << (y & 7));
                }
                else
                {
                    _buffer[x + (y / 8) * _columns] &= Convert.ToByte(~(1 << (y & 7)));
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.Message}: x({x}),y({y}),isOn({isOn})");
                throw;
            }
        }

        public void SendBuffer()
        {
            ActivityReset();

            _display.SendCommand(new SetColumnAddress());
            _display.SendCommand(new SetPageAddress(PageAddress.Page0, _maxPageAddress));

            int chunk_size = 16;
            for (int i = 0; i < _buffer.Length; i += chunk_size)
            {
                _display.SendData(_buffer.Skip(i).Take(chunk_size).ToArray());
            }
        }

        public void ClearBuffer()
        {
            _buffer = new byte[_buffer.Length];
        }

        public void Dispose()
        {
            SendMessage("Offline...");
            _display.Dispose();
        }

        private void Dim()
        {
            _preDimContrast = _contrast/255*100;
            _logger.Information($"Setting preDimContrast: {_contrast}({_preDimContrast}%)");
            SetContrast(0);
            _state = DisplayState.Dim;
        }

        private void UnDim()
        {
            SetContrast(_preDimContrast);
            _state = DisplayState.On;
        }

        private void Sleep()
        {
            _display.SendCommand(new SetDisplayOff());
            _state = DisplayState.Sleep;
        }

        private void Wake()
        {
            _display.SendCommand(new SetDisplayOn());

            //InitializeDisplay();
            UnDim();
        }

        private void SwapInt(ref int a, ref int b)
        {
            var t = a;
            a = b;
            b = t;
        }

        private void FillCircleHelper(int x, int y, int r, int corners, int delta)
        {

            int f = 1 - r;
            int ddF_x = 1;
            int ddF_y = -2 * r;
            int xt = 0;
            int yt = r;
            int px = xt;
            int py = yt;

            delta++; // Avoid some +1's in the loop

            while (xt < yt)
            {
                if (f >= 0)
                {
                    yt--;
                    ddF_y += 2;
                    f += ddF_y;
                }
                xt++;
                ddF_x += 2;
                f += ddF_x;
                // These checks avoid double-drawing certain lines, important
                // for the SSD1306 library which has an INVERT drawing mode.
                if (xt < (yt + 1))
                {
                    if ((corners & 1) != 0)
                        DrawVerticalLine(x + xt, y - yt, 2 * yt + delta);
                    if ((corners & 2) != 0)
                        DrawVerticalLine(x - xt, y - yt, 2 * yt + delta);
                }
                if (yt != py)
                {
                    if ((corners & 1) != 0)
                        DrawVerticalLine(x + py, y - px, 2 * px + delta);
                    if ((corners & 2) != 0)
                        DrawVerticalLine(x - py, y - px, 2 * px + delta);
                    py = yt;
                }
                px = xt;
            }
        }
    }
}
