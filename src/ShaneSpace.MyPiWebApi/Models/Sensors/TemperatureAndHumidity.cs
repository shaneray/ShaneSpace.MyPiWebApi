using UnitsNet;

namespace ShaneSpace.MyPiWebApi.Models.Sensors
{
    public class TemperatureAndHumidity
    {
        public Temperature Temperature { get; set; }
        public RelativeHumidity Humidity { get; set; }
    }
}
