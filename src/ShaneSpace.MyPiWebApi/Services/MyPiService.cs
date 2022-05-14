using Iot.Device.Card.Mifare;
using Iot.Device.Card.Ultralight;
using Iot.Device.DHTxx;
using Iot.Device.Hcsr04;
using Iot.Device.Mfrc522;
using Iot.Device.Ndef;
using Iot.Device.Rfid;
using Newtonsoft.Json;
using Serilog;
using ShaneSpace.MyPiWebApi.Models.Buttons;
using ShaneSpace.MyPiWebApi.Models.Leds;
using ShaneSpace.MyPiWebApi.Models.Sensors;
using System;
using System.Collections.Generic;
using System.Device.Spi;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitsNet;

namespace ShaneSpace.MyPiWebApi.Services
{
    public class MyPiService : BaseRaspberryPiService, IMyPiService, IDisposable
    {
        private readonly Dht11 _temperatureSensor;
        private readonly GpioButton _button1;
        private readonly RgbLed _rgbLed;
        private readonly GpioLed _blueLed;
        private MfRc522 _mfrc522;
        private int _button1PushCount = 0;
        private int _tooClose = int.MaxValue;
        protected override string RaspberryPiName => "Shane's Pi";

        public MyPiService(IGpioService gpioService, ILogger logger)
            : base(gpioService, logger)
        {
            // build components
            _rgbLed = new RgbLed(GpioService.Controller, 17, 27, 22, 1, "RGB LED", Color.White, logger);
            _blueLed = new GpioLed(GpioService.Controller, 16, "Blue LED", Color.Blue, logger);
            _button1 = new GpioButton(GpioService.Controller, 12, "Button 1", logger);
            _button1.StatusUpdated += Button1OnStatusUpdated;
            _temperatureSensor = new(26);

            // register components
            Leds.Add(_rgbLed);
            Leds.Add(_blueLed);
            Buttons.Add(_button1);

            logger.Information("{ServiceName} initialized.", nameof(MyPiService));

            _=StartSonar(async distance =>
            {
                if (_tooClose != int.MinValue && distance.Inches < 1)
                {
                    if (_tooClose != 1)
                    {
                        Display.ClearScreen();
                        Display.SendMessage($"{Math.Round(distance.Centimeters, 1)} cm. is way too close!", 20);
                        _tooClose = 1;
                    }
                }
                if (_tooClose != int.MinValue && distance.Inches < 2)
                {
                    if (_tooClose != 2)
                    {
                        Display.ClearScreen();
                        Display.SendMessage($"{Math.Round(distance.Inches, 1)} in. is too close!", 20);
                        _tooClose = 2;
                    }
                }
                else if (_tooClose != int.MinValue && distance.Inches < 6)
                {
                    if (_tooClose != 3)
                    {
                        Display.ClearScreen();
                        Display.SendMessage($"{Math.Round(distance.Inches, 1)} in. is a bit close!", 20);
                        _tooClose = 3;
                    }
                }
                else if (_tooClose != int.MinValue && _tooClose <= 3)
                {
                    _tooClose = int.MinValue;
                    Display.ClearScreen();
                    Display.SendMessage($"Thanks for the space...", 20);
                    await Task.Delay(1000);
                    Display.ClearScreen();
                    _tooClose = int.MaxValue;
                }
            });

            StartCardReader();
        }

        public TemperatureAndHumidity GetTemperatureAndHumidityData()
        {
            var maxRetries = 5;

            for (int i = 0; i < maxRetries; i++)
            {
                var temperature = _temperatureSensor.Temperature;
                var humidity = _temperatureSensor.Humidity;
                // You can only display temperature and humidity if the read is successful otherwise, this will raise an exception as
                // both temperature and humidity are NAN
                if (_temperatureSensor.IsLastReadSuccessful)
                {
                    return new TemperatureAndHumidity
                    {
                        Temperature = temperature,
                        Humidity = humidity
                    };
                }
            }

            throw new Exception($"Error reading DHT sensor after {maxRetries} attempts.");
        }

        public async Task StartSonar(Action<Length> action)
        {
            var sampleCount = 3;
            using (var sonar = new Hcsr04(4, 17))
            {
                while (true)
                {
                    var samples = new List<Length>();
                    for (int i = 0; i < sampleCount; i++)
                    {
                        if (sonar.TryGetDistance(out Length distance))
                        {
                            samples.Add(distance);
                            await Task.Delay(10);
                        }
                    }
                    // Logger.Information($"Samples: {string.Join(",", samples.OrderBy(x => x.Millimeters).Select(x => Math.Round(x.Millimeters, 1)))}");

                    if (samples.Count == 0)
                    {
                        Logger.Error("Error reading sonar sensor");
                        action.Invoke(Length.MaxValue);
                        continue;
                    }

                    var meanDistance = samples.First();
                    if (samples.Count > 1)
                    {
                        meanDistance = samples.OrderBy(x => x.Millimeters).Skip(samples.Count/2).First();
                    }

                    action.Invoke(meanDistance);
                    //_myPi.Display.SendMessage($"{Math.Round(meanDistance.Inches, 1)} in{Environment.NewLine}{Math.Round(meanDistance.Millimeters, 1)} mm", 12);
                    await Task.Delay(75);
                }
            }

        }

        public async Task StartCardReader()
        {

            SpiConnectionSettings connection = new(0, 1);
            var spi = SpiDevice.Create(connection);
            _mfrc522 = new(spi, 24);
            Console.WriteLine($"Version: {_mfrc522.Version}, version should be 1 or 2. Some clones may appear with version 0");

            bool res;
            Data106kbpsTypeA card;
            do
            {
                res = _mfrc522.ListenToCardIso14443TypeA(out card, TimeSpan.FromSeconds(2));
                if (res)
                {
                    Console.WriteLine("Card detected...");
                    Console.WriteLine($"{JsonConvert.SerializeObject(card, Formatting.Indented)}");
                    if (UltralightCard.IsUltralightCard(card.Atqa, card.Sak))
                    {
                        Console.WriteLine("Ultralight card detected, running various tests.");
                        await ProcessUltralightAsync(card);
                    }
                    else
                    {
                        Console.WriteLine("Mifare card detected, dumping the memory.");
                        await ProcessMifareAsync(card);
                    }
                }
                else
                {
                    //Console.WriteLine("RFID read fail");
                }
                await Task.Delay(res ? 0 : 200);
            }
            while (true);
        }

        private async Task ProcessMifareAsync(Data106kbpsTypeA card)
        {
            var mifare = new MifareCard(_mfrc522!, 0);
            mifare.SerialNumber = card.NfcId;
            mifare.Capacity = MifareCardCapacity.Mifare1K;
            mifare.KeyA = MifareCard.DefaultKeyA.ToArray();
            mifare.KeyB = MifareCard.DefaultKeyB.ToArray();
            int ret;

            for (byte block = 0; block < 64; block++)
            {
                mifare.BlockNumber = block;
                mifare.Command = MifareCardCommand.AuthenticationB;
                ret = mifare.RunMifareCardCommand();
                if (ret < 0)
                {
                    // If you have an authentication error, you have to deselect and reselect the card again and retry
                    // Those next lines shows how to try to authenticate with other known default keys
                    mifare.ReselectCard();
                    // Try the other key
                    mifare.KeyA = MifareCard.DefaultKeyA.ToArray();
                    mifare.Command = MifareCardCommand.AuthenticationA;
                    ret = mifare.RunMifareCardCommand();
                    if (ret < 0)
                    {
                        mifare.ReselectCard();
                        mifare.KeyA = MifareCard.DefaultBlocksNdefKeyA.ToArray();
                        mifare.Command = MifareCardCommand.AuthenticationA;
                        ret = mifare.RunMifareCardCommand();
                        if (ret < 0)
                        {
                            mifare.ReselectCard();
                            mifare.KeyA = MifareCard.DefaultFirstBlockNdefKeyA.ToArray();
                            mifare.Command = MifareCardCommand.AuthenticationA;
                            ret = mifare.RunMifareCardCommand();
                            if (ret < 0)
                            {
                                mifare.ReselectCard();
                                Console.WriteLine($"Error reading bloc: {block}");
                            }
                        }
                    }
                }

                if (ret >= 0)
                {
                    mifare.BlockNumber = block;
                    mifare.Command = MifareCardCommand.Read16Bytes;
                    ret = mifare.RunMifareCardCommand();
                    if (ret >= 0)
                    {
                        if (mifare.Data is object)
                        {
                            Console.WriteLine($"Bloc: {block}, Data: {BitConverter.ToString(mifare.Data)}");
                        }
                    }
                    else
                    {
                        mifare.ReselectCard();
                        Console.WriteLine($"Error reading bloc: {block}");
                    }

                    if (block % 4 == 3)
                    {
                        if (mifare.Data != null)
                        {
                            // Check what are the permissions
                            for (byte j = 3; j > 0; j--)
                            {
                                var access = mifare.BlockAccess((byte)(block - j), mifare.Data);
                                Console.WriteLine($"Bloc: {block - j}, Access: {access}");
                            }

                            var sector = mifare.SectorTailerAccess(block, mifare.Data);
                            Console.WriteLine($"Bloc: {block}, Access: {sector}");
                        }
                        else
                        {
                            Console.WriteLine("Can't check any sector bloc");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Authentication error");
                }
            }

            NdefMessage message;
            var conf = mifare.TryReadNdefMessage(out message);
            if (conf && message.Length != 0)
            {
                foreach (var record in message.Records)
                {
                    Console.WriteLine($"Record length: {record.Length}");
                    if (TextRecord.IsTextRecord(record))
                    {
                        var text = new TextRecord(record);
                        Console.WriteLine(text.Text);
                    }
                }
            }
            else
            {
                Console.WriteLine("No NDEF message in this ");
            }

            conf = mifare.IsFormattedNdef();
            if (!conf)
            {
                Console.WriteLine("Card is not NDEF formated, we will try to format it");
                conf = mifare.FormatNdef();
                if (!conf)
                {
                    Console.WriteLine("Impossible to format in NDEF, we will still try to write NDEF content.");
                }
                else
                {
                    conf = mifare.IsFormattedNdef();
                    if (conf)
                    {
                        Console.WriteLine("Formating successful");
                    }
                    else
                    {
                        Console.WriteLine("Card is not NDEF formated.");
                    }
                }
            }

            NdefMessage newMessage = new NdefMessage();
            newMessage.Records.Add(new TextRecord("I ❤ .NET IoT", "en", Encoding.UTF8));
            conf = mifare.WriteNdefMessage(newMessage);
            if (conf)
            {
                Console.WriteLine("NDEF data successfully written on the card.");
            }
            else
            {
                Console.WriteLine("Error writing NDEF data on card");
            }

            await Task.Delay(1000);
        }

        private async Task ProcessUltralightAsync(Data106kbpsTypeA card)
        {
            var ultralight = new UltralightCard(_mfrc522!, 0);
            ultralight.SerialNumber = card.NfcId;
            Console.WriteLine($"Type: {ultralight.UltralightCardType}, Ndef capacity: {ultralight.NdefCapacity}");

            var version = ultralight.GetVersion();
            if ((version != null) && (version.Length > 0))
            {
                Console.WriteLine("Get Version details: ");
                for (int i = 0; i < version.Length; i++)
                {
                    Console.Write($"{version[i]:X2} ");
                }

                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("Can't read the version.");
            }

            var sign = ultralight.GetSignature();
            if ((sign != null) && (sign.Length > 0))
            {
                Console.WriteLine("Signature: ");
                for (int i = 0; i < sign.Length; i++)
                {
                    Console.Write($"{sign[i]:X2} ");
                }

                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("Can't read the signature.");
            }

            // The ReadFast feature can be used as well, note that the MFRC522 has a very limited FIFO
            // So maximum 9 pages can be read as once.
            Console.WriteLine("Fast read example:");
            var buff = ultralight.ReadFast(0, 8);
            if (buff != null)
            {
                for (int i = 0; i < buff.Length / 4; i++)
                {
                    Console.WriteLine($"  Block {i} - {buff[i * 4]:X2} {buff[i * 4 + 1]:X2} {buff[i * 4 + 2]:X2} {buff[i * 4 + 3]:X2}");
                }
            }

            Console.WriteLine("Dump of all the card:");
            for (int block = 0; block < ultralight.NumberBlocks; block++)
            {
                ultralight.BlockNumber = (byte)block; // Safe cast, can't be more than 255
                ultralight.Command = UltralightCommand.Read16Bytes;
                var res = ultralight.RunUltralightCommand();
                if (res > 0)
                {
                    Console.Write($"  Block: {ultralight.BlockNumber:X2} - ");
                    for (int i = 0; i < 4; i++)
                    {
                        Console.Write($"{ultralight.Data![i]:X2} ");
                    }

                    var isReadOnly = ultralight.IsPageReadOnly(ultralight.BlockNumber);
                    Console.Write($"- Read only: {isReadOnly} ");

                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("Can't read card");
                    break;
                }
            }

            Console.WriteLine("Configuration of the card");
            // Get the Configuration
            var conf = ultralight.TryGetConfiguration(out Configuration configuration);
            if (conf)
            {
                Console.WriteLine("  Mirror:");
                Console.WriteLine($"    {configuration.Mirror.MirrorType}, page: {configuration.Mirror.Page}, position: {configuration.Mirror.Position}");
                Console.WriteLine("  Authentication:");
                Console.WriteLine($"    Page req auth: {configuration.Authentication.AuthenticationPageRequirement}, Is auth req for read and write: {configuration.Authentication.IsReadWriteAuthenticationRequired}");
                Console.WriteLine($"    Is write lock: {configuration.Authentication.IsWritingLocked}, Max num tries: {configuration.Authentication.MaximumNumberOfPossibleTries}");
                Console.WriteLine("  NFC Counter:");
                Console.WriteLine($"    Enabled: {configuration.NfcCounter.IsEnabled}, Password protected: {configuration.NfcCounter.IsPasswordProtected}");
                Console.WriteLine($"  Is strong modulation: {configuration.IsStrongModulation}");
            }
            else
            {
                Console.WriteLine("Error getting the configuration");
            }

            NdefMessage message;
            conf = ultralight.TryReadNdefMessage(out message);
            if (conf && message.Length != 0)
            {
                foreach (var record in message.Records)
                {
                    Console.WriteLine($"Record length: {record.Length}");
                    if (TextRecord.IsTextRecord(record))
                    {
                        var text = new TextRecord(record);
                        Console.WriteLine(text.Text);
                    }
                }
            }
            else
            {
                Console.WriteLine("No NDEF message in this ");
            }

            conf = ultralight.IsFormattedNdef();
            if (!conf)
            {
                Console.WriteLine("Card is not NDEF formated, we will try to format it");
                conf = ultralight.FormatNdef();
                if (!conf)
                {
                    Console.WriteLine("Impossible to format in NDEF, we will still try to write NDEF content.");
                }
                else
                {
                    conf = ultralight.IsFormattedNdef();
                    if (conf)
                    {
                        Console.WriteLine("Formating successful");
                    }
                    else
                    {
                        Console.WriteLine("Card is not NDEF formated.");
                    }
                }
            }

            NdefMessage newMessage = new NdefMessage();
            newMessage.Records.Add(new TextRecord("I ❤ .NET IoT", "en", Encoding.UTF8));
            conf = ultralight.WriteNdefMessage(newMessage);
            if (conf)
            {
                Console.WriteLine("NDEF data successfully written on the card.");
            }
            else
            {
                Console.WriteLine("Error writing NDEF data on card");
            }

            await Task.Delay(1000);
        }

        private void Button1OnStatusUpdated(object sender, ButtonStatusUpdatedEventArgs eventArgs)
        {
            if (eventArgs.IsPushed)
            {
                _rgbLed.Color = _button1PushCount switch
                {
                    1 => Color.Red,
                    2 => Color.Green,
                    3 => Color.Blue,
                    _ => Color.White
                };

                _button1PushCount++;
                if (_button1PushCount > 3)
                {
                    _button1PushCount = 0;
                }
            }

            _rgbLed.IsOn = eventArgs.IsPushed;
        }

        public void Dispose()
        {
            Display.Dispose();
        }
    }
}