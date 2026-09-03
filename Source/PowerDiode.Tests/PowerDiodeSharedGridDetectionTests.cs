using RimTestRedux;

namespace PowerDiode.Tests;

[TestSuite]
internal static class PowerDiodeSharedGridDetectionTests
{
    [Test]
    public static void SameInstanceIsSharedGrid()
    {
        var net = new object();

        Assert.That(PowerDiodeSharedGridDetection.IsSharedGrid(net, net)).Is.True();
    }

    [Test]
    public static void DifferentInstancesAreNotSharedGrid()
    {
        var sinkNet = new object();
        var sourceNet = new object();

        Assert.That(PowerDiodeSharedGridDetection.IsSharedGrid(sinkNet, sourceNet)).Is.False();
    }

    [Test]
    public static void NullSinkIsNotSharedGrid()
    {
        var sourceNet = new object();

        Assert.That(PowerDiodeSharedGridDetection.IsSharedGrid(null, sourceNet)).Is.False();
    }

    [Test]
    public static void BothNullIsNotSharedGrid() =>
        Assert.That(PowerDiodeSharedGridDetection.IsSharedGrid<object>(null, null)).Is.False();
}
