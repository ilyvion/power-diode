using DevTools.Testing;

namespace PowerDiode.Tests;

[TestFixture(TestType.MainMenu)]
internal sealed class FindPartnerToLinkToTests
{
    // A def with neither diode comp is never looked up on the map, so no map is needed.
    [Test]
    public static void NonDiodeDefHasNoPartner() =>
        Expect.IsNull(PowerDiodeLinking.FindPartnerToLinkTo(ThingDefOf.Wall, IntVec3.Zero, null!));
}
