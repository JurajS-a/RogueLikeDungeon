using System.Collections.Generic;

// Pretrazivanje grafa soba u sirinu (BFS).
// Sluzi za odredivanje sobe najudaljenije od pocetne, mjereno brojem
// soba na putu, a ne zracnom linijom.
public static class GraphSearch
{
    public static int[] BfsDistances(int startNode, int nodeCount, List<Edge> edges)
    {
        // Lista susjedstva iz liste bridova (graf je neusmjeren)
        var neighbors = new List<int>[nodeCount];
        for (int i = 0; i < nodeCount; i++)
            neighbors[i] = new List<int>();

        foreach (var e in edges)
        {
            neighbors[e.A].Add(e.B);
            neighbors[e.B].Add(e.A);
        }

        var distance = new int[nodeCount];
        for (int i = 0; i < nodeCount; i++)
            distance[i] = -1;              // -1 = neposjecen

        var queue = new Queue<int>();
        distance[startNode] = 0;
        queue.Enqueue(startNode);

        // Red osigurava obradu po rastucoj udaljenosti, pa je prvi
        // pronadeni put ujedno najkraci
        while (queue.Count > 0)
        {
            int current = queue.Dequeue();

            foreach (int next in neighbors[current])
            {
                if (distance[next] != -1) continue;

                distance[next] = distance[current] + 1;
                queue.Enqueue(next);
            }
        }

        return distance;
    }

    public static int FarthestNode(int[] distances)
    {
        int best = 0;
        for (int i = 1; i < distances.Length; i++)
            if (distances[i] > distances[best])
                best = i;
        return best;
    }
}