using System.Collections.Generic;
using UnityEngine;

// Brid grafa: povezuje dvije sobe s tezinom = udaljenost centara.
public struct Edge
{
    public int A;
    public int B;
    public float Weight;

    public Edge(int a, int b, float weight)
    {
        A = a;
        B = b;
        Weight = weight;
    }
}

// Korak 2: potpuni graf nad centrima soba. Za n soba daje n(n-1)/2
// bridova i garantira povezanost, pa MST uvijek obuhvaca sve sobe.
public static class GraphBuilder
{
    public static List<Edge> BuildCompleteGraph(List<Room> rooms)
    {
        var edges = new List<Edge>();

        for (int i = 0; i < rooms.Count; i++)
        {
            for (int j = i + 1; j < rooms.Count; j++)
            {
                float weight = Vector2Int.Distance(rooms[i].Center, rooms[j].Center);
                edges.Add(new Edge(i, j, weight));
            }
        }

        return edges;
    }
}