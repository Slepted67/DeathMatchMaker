using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class AINavGrid2D : MonoBehaviour
{
    public Tilemap obstacleTilemap;   // assign your Obstacles tilemap
    public float cellSize = 1f;       // usually tilemap.cellSize.y/x
    public int maxSearch = 2048;      // BFS node cap

    BoundsInt bounds;
    bool[,] blocked;

    public void Build()
    {
        bounds = obstacleTilemap.cellBounds;
        blocked = new bool[bounds.size.x, bounds.size.y];

        // mark blocked cells if they have a collider tile
        foreach (var pos in bounds.allPositionsWithin)
        {
            int ix = pos.x - bounds.xMin;
            int iy = pos.y - bounds.yMin;
            var tile = obstacleTilemap.GetTile(pos);
            blocked[ix, iy] = tile != null; // simple; refine if needed
        }
    }

    public bool TryPath(Vector2 fromWorld, Vector2 toWorld, List<Vector2> outPath)
    {
        outPath.Clear();
        var start = WorldToCellIndex(fromWorld, out int sx, out int sy);
        var goal  = WorldToCellIndex(toWorld,   out int gx, out int gy);
        if (!start || !InBounds(sx,sy) || !InBounds(gx,gy)) return false;

        var came = new Dictionary<(int,int),(int,int)>();
        var q = new Queue<(int,int)>();
        q.Enqueue((sx,sy));
        came[(sx,sy)] = (-999,-999);

        int nodes = 0;
        (int,int)[] dirs = { (1,0),(-1,0),(0,1),(0,-1) };

        while (q.Count>0 && nodes<maxSearch)
        {
            nodes++;
            var cur = q.Dequeue();
            if (cur == (gx,gy)) break;
            foreach (var d in dirs)
            {
                var nx = cur.Item1 + d.Item1;
                var ny = cur.Item2 + d.Item2;
                if (!InBounds(nx,ny) || blocked[nx,ny]) continue;
                if (came.ContainsKey((nx,ny))) continue;
                came[(nx,ny)] = cur;
                q.Enqueue((nx,ny));
            }
        }
        if (!came.ContainsKey((gx,gy))) return false;

        // reconstruct
        var p = (gx,gy);
        while (p != (-999,-999))
        {
            outPath.Add(CellCenter(p.Item1, p.Item2));
            p = came[p];
        }
        outPath.Reverse();
        return true;
    }

    bool InBounds(int x,int y) => x>=0 && y>=0 && x<blocked.GetLength(0) && y<blocked.GetLength(1);

    bool WorldToCellIndex(Vector2 w, out int ix, out int iy)
    {
        var cell = obstacleTilemap.WorldToCell(w);
        ix = cell.x - bounds.xMin;
        iy = cell.y - bounds.yMin;
        return true;
    }

    Vector2 CellCenter(int ix,int iy)
    {
        var cell = new Vector3Int(ix + bounds.xMin, iy + bounds.yMin, 0);
        return obstacleTilemap.GetCellCenterWorld(cell);
    }
}