using ShaneSpace.MyPiWebApi.Models.Sensors;

namespace ShaneSpace.MyPiWebApi.Services
{
    public interface IMyPiService : IRaspberryPiService
    {
        TemperatureAndHumidity GetTemperatureAndHumidityData();
    }
}