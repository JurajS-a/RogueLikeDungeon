using System.Collections.Generic;

// Korak 3: minimalno razapinjuce stablo Primovim algoritmom.
// Rezultat je n-1 bridova koji povezuju sve sobe bez ciklusa,
// uz minimalnu ukupnu duljinu hodnika.
public static class MstSolver
{
    public static List<Edge> Solve(int nodeCount, List<Edge> edges)
    {
        var result = new List<Edge>();
        if (nodeCount == 0) return result;

        var inTree = new bool[nodeCount];
        inTree[0] = true;

        while (result.Count < nodeCount - 1)
        {
            Edge best = default;
            float bestWeight = float.MaxValue;
            bool found = false;

            foreach (var e in edges)
            {
                // XOR: tocno jedan kraj u stablu - brid prosiruje stablo
                // bez stvaranja ciklusa
                if (inTree[e.A] ^ inTree[e.B] && e.Weight < bestWeight)
                {
                    best = e;
                    bestWeight = e.Weight;
                    found = true;
                }
            }

            if (!found) break;

            result.Add(best);
            inTree[best.A] = true;
            inTree[best.B] = true;
        }

        return result;
    }
}