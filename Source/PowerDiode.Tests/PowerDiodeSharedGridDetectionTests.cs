using DevTools.Testing;

namespace PowerDiode.Tests;

[TestFixture(TestType.MainMenu)]
internal sealed class PowerDiodeSharedGridDetectionTests
{
    [Test]
    public static void SameInstanceIsSharedGrid()
    {
        var net = new object();

        Expect.IsTrue(PowerDiodeSharedGridDetection.IsSharedGrid(net, net));
    }

    [Test]
    public static void DifferentInstancesAreNotSharedGrid()
    {
        var sinkNet = new object();
        var sourceNet = new object();

        Expect.IsFalse(PowerDiodeSharedGridDetection.IsSharedGrid(sinkNet, sourceNet));
    }

    [Test]
    public static void NullSinkIsNotSharedGrid()
    {
        var sourceNet = new object();

        Expect.IsFalse(PowerDiodeSharedGridDetection.IsSharedGrid(null, sourceNet));
    }

    [Test]
    public static void BothNullIsNotSharedGrid() =>
        Expect.IsFalse(PowerDiodeSharedGridDetection.IsSharedGrid<object>(null, null));
}
