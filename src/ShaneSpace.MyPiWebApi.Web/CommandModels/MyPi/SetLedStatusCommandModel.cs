namespace ShaneSpace.MyPiWebApi.Web.CommandModels.MyPi
{
    /// <summary>
    /// Update Led Command Model
    /// </summary>
    public class UpdateLedCommandModel
    {
        public bool IsOn { get; set; }
        public int Brightness { get; set; }
    }
}
