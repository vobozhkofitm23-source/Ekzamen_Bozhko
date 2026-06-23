using System.Collections.Generic;
using UnityEngine;

namespace NightWatch
{
    /// <summary>
    /// Сітка карти: коричнева стежка для ворогів, зелена трава для башен, дерева по краях.
    /// </summary>
    public static class LevelMap
    {
        public const int Width = 18;
        public const int Height = 14;
        public const float CellSize = 2f;

        public static readonly Vector2Int CrystalCell = new(16, 7);
        public static readonly Vector2Int[] SpawnCells =
        {
            new(1, 3),
            new(1, 7),
            new(1, 11)
        };

        static HashSet<Vector2Int> _pathCells;
        static HashSet<Vector2Int> _treeCells;

        public static IReadOnlyCollection<Vector2Int> PathCells
        {
            get { EnsureInit(); return _pathCells; }
        }

        public static IReadOnlyCollection<Vector2Int> TreeCells
        {
            get { EnsureInit(); return _treeCells; }
        }

        public static bool IsPathCell(Vector2Int cell)
        {
            EnsureInit();
            return _pathCells.Contains(cell);
        }

        public static bool IsTreeCell(Vector2Int cell)
        {
            EnsureInit();
            return _treeCells.Contains(cell);
        }

        static void EnsureInit()
        {
            if (_pathCells != null) return;

            _pathCells = new HashSet<Vector2Int>();

            // Три входи зліва зливаються в один коридор
            AddLine(1, 3, 5, 3);
            AddLine(5, 3, 5, 6);
            AddLine(1, 7, 5, 7);
            AddLine(5, 7, 5, 6);
            AddLine(1, 11, 5, 11);
            AddLine(5, 11, 5, 8);

            // Довгий зигзаг до кристалу (праворуч, вгору, праворуч, вниз)
            AddLine(6, 6, 12, 6);
            AddLine(12, 6, 12, 9);
            AddLine(12, 9, 16, 9);
            AddLine(16, 9, 16, 7);

            _treeCells = new HashSet<Vector2Int>();
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Height; z++)
                {
                    var cell = new Vector2Int(x, z);
                    if (_pathCells.Contains(cell) || cell == CrystalCell) continue;

                    bool border = x == 0 || x == Width - 1 || z == 0 || z == Height - 1;
                    bool cluster = (x * 3 + z * 5) % 11 == 0 && x > 1 && x < Width - 2 && z > 1 && z < Height - 2;
                    if (border || cluster)
                        _treeCells.Add(cell);
                }
            }
        }

        static void AddLine(int x0, int z0, int x1, int z1)
        {
            int dx = x1 == x0 ? 0 : (x1 > x0 ? 1 : -1);
            int dz = z1 == z0 ? 0 : (z1 > z0 ? 1 : -1);
            int x = x0, z = z0;
            while (true)
            {
                _pathCells.Add(new Vector2Int(x, z));
                if (x == x1 && z == z1) break;
                if (x != x1) x += dx;
                if (z != z1) z += dz;
            }
        }

        public static Vector3 CellToWorld(Vector2Int cell)
        {
            float ox = (Width - 1) * 0.5f;
            float oz = (Height - 1) * 0.5f;
            return new Vector3((cell.x - ox) * CellSize, 0f, (cell.y - oz) * CellSize);
        }

        public static bool IsBuildable(Vector2Int cell)
        {
            EnsureInit();
            if (cell.x <= 0 || cell.x >= Width - 1 || cell.y <= 0 || cell.y >= Height - 1) return false;
            return !_pathCells.Contains(cell) && !_treeCells.Contains(cell) && cell != CrystalCell;
        }

        public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
        {
            EnsureInit();
            var prev = new Dictionary<Vector2Int, Vector2Int>();
            var q = new Queue<Vector2Int>();
            var seen = new HashSet<Vector2Int> { start };
            q.Enqueue(start);

            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                if (cur == goal) break;

                foreach (var n in Neighbors(cur))
                {
                    if (!_pathCells.Contains(n) && n != goal) continue;
                    if (seen.Contains(n)) continue;
                    seen.Add(n);
                    prev[n] = cur;
                    q.Enqueue(n);
                }
            }

            var result = new List<Vector2Int>();
            if (!seen.Contains(goal)) return result;

            for (var c = goal; c != start; c = prev[c])
                result.Add(c);
            result.Add(start);
            result.Reverse();
            return result;
        }

        static IEnumerable<Vector2Int> Neighbors(Vector2Int c)
        {
            yield return new Vector2Int(c.x + 1, c.y);
            yield return new Vector2Int(c.x - 1, c.y);
            yield return new Vector2Int(c.x, c.y + 1);
            yield return new Vector2Int(c.x, c.y - 1);
        }

        public static Vector3[] BuildWorldWaypoints(Vector2Int spawn)
        {
            var gridPath = FindPath(spawn, CrystalCell);
            if (gridPath.Count < 2)
            {
                return new[]
                {
                    CellToWorld(spawn) + Vector3.up * 0.55f,
                    CellToWorld(CrystalCell) + Vector3.up * 0.55f
                };
            }

            var pts = new Vector3[gridPath.Count];
            for (int i = 0; i < gridPath.Count; i++)
                pts[i] = CellToWorld(gridPath[i]) + Vector3.up * 0.5f;
            return pts;
        }
    }
}
