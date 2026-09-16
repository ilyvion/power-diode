[assembly: InternalsVisibleTo("PowerDiode.Tests")]

namespace PowerDiode;

internal partial class PowerDiodeMod
{
    internal static Settings Settings { get; private set; } = null!;

    partial void Construct()
    {
        new Harmony(Constants.Id).PatchAll(Assembly.GetExecutingAssembly());
        Settings = GetSettings<Settings>();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        base.DoSettingsWindowContents(inRect);
        Settings.DoSettingsWindowContents(inRect);
    }

    protected override bool HasSettings => true;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
internal sealed class HotSwappableAttribute : Attribute { }
