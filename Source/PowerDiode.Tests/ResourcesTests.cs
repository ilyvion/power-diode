using DevTools.Testing;

namespace PowerDiode.Tests;

[TestFixture(TestType.MainMenu)]
internal sealed class ResourcesTests
{
    [Test]
    public static void OperatingModeIconsAreLoaded()
    {
        foreach (PowerDiodeOperatingMode mode in Enum.GetValues(typeof(PowerDiodeOperatingMode)))
        {
            var icon = mode.Icon();
            Expect.IsNotNull(icon, mode.ToString());
            Expect.ReferencesAreNotEqual(BaseContent.BadTex, icon, mode.ToString());
        }
    }

    [Test]
    public static void LinkNeighbourIconIsLoaded()
    {
        Expect.IsNotNull(Resources.LinkNeighbourIcon);
        Expect.ReferencesAreNotEqual(BaseContent.BadTex, Resources.LinkNeighbourIcon);
    }

    [Test]
    public static void NoBatteriesOverlayIsLoaded()
    {
        var texture = PowerDiodeOverlay.NoBatteriesMat.mainTexture;
        Expect.IsNotNull(texture);
        Expect.ReferencesAreNotEqual(BaseContent.BadTex, texture);
    }
}
