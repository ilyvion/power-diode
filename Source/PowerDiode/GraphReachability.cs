namespace PowerDiode;

// Pure BFS, kept free of any Verse/RimWorld types so it's unit-testable without spawning
// anything on a live Map. PowerNetMaker_ContiguousPowerBuildings supplies Building-specific
// adjacency/blocked-edge delegates over live game state.
internal static class GraphReachability
{
    internal static HashSet<T> ReachableWithoutCrossingBlockedEdges<T>(
        T root,
        IReadOnlyCollection<T> allowed,
        Func<T, IEnumerable<T>> adjacentCandidates,
        Func<T, T, bool> isBlockedEdge
    )
        where T : notnull
    {
        var allowedSet = allowed as HashSet<T> ?? [.. allowed];
        var visited = new HashSet<T> { root };
        var queue = new Queue<T>();
        queue.Enqueue(root);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var candidate in adjacentCandidates(current))
            {
                if (
                    allowedSet.Contains(candidate)
                    && !visited.Contains(candidate)
                    && !isBlockedEdge(current, candidate)
                )
                {
                    _ = visited.Add(candidate);
                    queue.Enqueue(candidate);
                }
            }
        }
        return visited;
    }
}
