using DevTools.Testing;

namespace PowerDiode.Tests;

[TestFixture(TestType.MainMenu)]
internal sealed class OperatingModeTranslationTests
{
    [Test]
    public static void EveryModeHasALabelAndDescription()
    {
        foreach (PowerDiodeOperatingMode mode in Enum.GetValues(typeof(PowerDiodeOperatingMode)))
        {
            Expect.IsTrue($"PowerDiode.Mode.{mode}".CanTranslate(), $"{mode} label");
            Expect.IsTrue($"PowerDiode.Mode.{mode}.Desc".CanTranslate(), $"{mode} description");
        }
    }

    [Test]
    public static void UnknownModeThrows()
    {
        var unknown = (PowerDiodeOperatingMode)99;

        _ = Expect.Throws<ArgumentOutOfRangeException>(() => unknown.Label(), "Label");
        _ = Expect.Throws<ArgumentOutOfRangeException>(() => unknown.Description(), "Description");
        _ = Expect.Throws<ArgumentOutOfRangeException>(() => unknown.Icon(), "Icon");
    }
}
