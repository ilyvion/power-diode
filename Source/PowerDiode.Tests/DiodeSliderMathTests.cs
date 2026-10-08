using DevTools.Testing;

namespace PowerDiode.Tests;

[TestFixture(TestType.MainMenu)]
internal sealed class DiodeSliderMathTests
{
    [Test]
    public static void FractionRoundTripsToTheSameValue()
    {
        foreach (var value in new[] { 0f, 125f, 2500f, 4975f, 5000f })
        {
            Expect.AreApproximatelyEqual(
                value,
                DiodeSliderMath.FromFraction(
                    DiodeSliderMath.ToFraction(value, 0f, 5000f),
                    0f,
                    5000f
                )
            );
        }
        Expect.AreApproximatelyEqual(
            42f,
            DiodeSliderMath.FromFraction(DiodeSliderMath.ToFraction(42f, 10f, 500f), 10f, 500f)
        );
    }

    [Test]
    public static void EndpointsMapToZeroAndOne()
    {
        Expect.AreEqual(0f, DiodeSliderMath.ToFraction(10f, 10f, 500f));
        Expect.AreEqual(1f, DiodeSliderMath.ToFraction(500f, 10f, 500f));
        Expect.AreEqual(10f, DiodeSliderMath.FromFraction(0f, 10f, 500f));
        Expect.AreEqual(500f, DiodeSliderMath.FromFraction(1f, 10f, 500f));
    }

    [Test]
    public static void ValuesOutsideTheRangeMapOutsideZeroToOne()
    {
        Expect.LessThan(DiodeSliderMath.ToFraction(-100f, 0f, 5000f), 0f);
        Expect.GreaterThan(DiodeSliderMath.ToFraction(6000f, 0f, 5000f), 1f);
    }

    // Regression guard: min == max used to give a 0/0 = NaN slider position, which Gizmo_Slider
    // wrote back into the diode's setting, leaving it NaN.
    [Test]
    public static void EmptyRangeHasNoRangeAndNeverGivesNaN()
    {
        Expect.IsFalse(DiodeSliderMath.HasRange(300f, 300f));
        Expect.IsFalse(DiodeSliderMath.HasRange(300f, 200f));
        Expect.IsTrue(DiodeSliderMath.HasRange(200f, 300f));

        var fraction = DiodeSliderMath.ToFraction(300f, 300f, 300f);
        Expect.IsFalse(float.IsNaN(fraction));
        Expect.AreEqual(300f, DiodeSliderMath.FromFraction(fraction, 300f, 300f));
    }

    [Test]
    public static void IncrementsIsRangeOverStepAndAtLeastOne()
    {
        Expect.AreEqual(200, DiodeSliderMath.Increments(5000f, 25f));
        Expect.AreEqual(20, DiodeSliderMath.Increments(100f, 5f));
        Expect.AreEqual(1, DiodeSliderMath.Increments(10f, 50f));
        Expect.AreEqual(1, DiodeSliderMath.Increments(0f, 25f));
    }
}
