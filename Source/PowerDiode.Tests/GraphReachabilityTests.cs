using DevTools.Testing;

namespace PowerDiode.Tests;

[TestFixture(TestType.MainMenu)]
internal sealed class GraphReachabilityTests
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

        Expect.AreEqual(4, reachable.Count);
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

        Expect.AreEqual(2, reachable.Count);
        Expect.IsTrue(reachable.Contains('A'));
        Expect.IsTrue(reachable.Contains('B'));
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

        Expect.AreEqual(2, reachable.Count);
        Expect.IsFalse(reachable.Contains('C'));
    }

    [Test]
    public static void RootOutsideTheAllowedSetIsStillReturned()
    {
        var graph = LineGraph();

        var reachable = GraphReachability.ReachableWithoutCrossingBlockedEdges(
            'A',
            [],
            node => Adjacent(graph, node),
            (_, _) => false
        );

        Expect.AreEqual(1, reachable.Count);
        Expect.IsTrue(reachable.Contains('A'));
    }

    [Test]
    public static void TerminatesOnACyclicGraph()
    {
        var graph = new Dictionary<char, char[]>
        {
            ['A'] = ['B', 'C'],
            ['B'] = ['A', 'C'],
            ['C'] = ['A', 'B'],
        };

        var reachable = GraphReachability.ReachableWithoutCrossingBlockedEdges(
            'A',
            ['A', 'B', 'C'],
            node => Adjacent(graph, node),
            (_, _) => false
        );

        Expect.AreEqual(3, reachable.Count);
    }
}
