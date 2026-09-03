namespace PowerDiode;

internal class CompProperties_PowerDiodeFeed : CompProperties
{
    public float maxAllowedWattage = 500f;
    public float wattageStepSize = 25f;
    public float minReserveWattDays = 10f;
    public float maxReserveWattDays = 500f;
    public float reserveWattDaysStepSize = 10f;

    public CompProperties_PowerDiodeFeed()
    {
        compClass = typeof(CompPowerDiodeFeed);
    }
}
