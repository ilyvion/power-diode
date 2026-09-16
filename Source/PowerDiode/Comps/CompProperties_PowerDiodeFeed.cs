namespace PowerDiode;

internal class CompProperties_PowerDiodeFeed : CompProperties
{
    public float wattageStepSize = 25f;
    public float reserveWattDaysStepSize = 10f;

    public CompProperties_PowerDiodeFeed()
    {
        compClass = typeof(CompPowerDiodeFeed);
    }
}
