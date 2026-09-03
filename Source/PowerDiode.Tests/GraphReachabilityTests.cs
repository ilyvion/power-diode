using RimTestRedux;

namespace PowerDiode.Tests;

[TestSuite]
internal static class GraphReachabilityTests
{
    private static Dictionary<char, char[]> LineGraph() =>
        new()
        {
            ['A'] = ['B'],
            ['B'] = ['A', 'C'],
            ['C'] = ['B', 'D'],
            ['D'] = ['C'],
        };

    private static char[] Adjacent(Dictionary<char, char[]> graph, char node) =>
        graph.TryGetValue(node, out var neighbors) ? neighbors : [];

    [Test]
    public static void ReturnsEntireComponentWhenNoEdgeIsBlocked()
    {
        var graph = LineGraph();

        var reachable = GraphReachability.ReachableWithoutCrossingBlockedEdges(
            'A',
            ['A', 'B', 'C', 'D'],
            node => Adjacent(graph, node),
            (_, _) => false
        );

        Assert.ThatCollection(reachable).Has.Count(4);
    }

    // Regression guard for the diode use case: blocking a single edge must split the
    // component into exactly the two halves either side of it.
    [Test]
    public static void StopsAtABlockedEdge()
    {
        var graph = LineGraph();

        var reachable = GraphReachability.ReachableWithoutCrossingBlockedEdges(
            'A',
            ['A', 'B', 'C', 'D'],
            node => Adjacent(graph, node),
            (from, to) => (from == 'B' && to == 'C') || (from == 'C' && to == 'B')
        );

        Assert.ThatCollection(reachable).Has.Count(2);
        Assert.ThatCollection(reachable).Does.Contain('A');
        Assert.ThatCollection(reachable).Does.Contain('B');
    }

    [Test]
    public static void NeverStepsOutsideTheAllowedSet()
    {
        var graph = new Dictionary<char, char[]>
        {
            ['A'] = ['B'],
            ['B'] = ['A', 'C'],
            ['C'] = ['B'],
        };

        var reachable = GraphReachability.ReachableWithoutCrossingBlockedEdges(
            'A',
            ['A', 'B'],
            node => Adjacent(graph, node),
            (_, _) => false
        );

        Assert.ThatCollection(reachable).Has.Count(2);
        Assert.ThatCollection(reachable).Does.Not.Contain('C');
    }
}
