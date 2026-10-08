namespace PowerDiode;

// Maps a diode setting between its [min, max] range and the 0-1 position of its gizmo slider.
internal static class DiodeSliderMath
{
    // A slider over an empty range can't be moved, so its gizmo is hidden.
    internal static bool HasRange(float min, float max) => max > min;

    // Returns 0 for an empty range rather than the NaN that 0/0 gives, since Gizmo_Slider writes
    // the fraction straight back through FromFraction and Mathf.Clamp lets NaN through.
    internal static float ToFraction(float value, float min, float max) =>
        HasRange(min, max) ? (value - min) / (max - min) : 0f;

    internal static float FromFraction(float fraction, float min, float max) =>
        min + (fraction * (max - min));

    internal static int Increments(float range, float stepSize) =>
        Mathf.Max(1, Mathf.RoundToInt(range / stepSize));
}
