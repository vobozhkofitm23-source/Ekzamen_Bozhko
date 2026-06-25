using UnityEngine;

namespace NightWatch
{
    public class BuildZone : MonoBehaviour
    {
        public Vector2Int Cell { get; private set; }
        public bool Occupied { get; private set; }

        GameObject _marker;

        public void Setup(Vector2Int cell, Vector3 position)
        {
            Cell = cell;
            transform.position = position;

            var grass = ModelSpawner.Spawn("tile", position, transform, LevelMap.CellSize);
            ModelSpawner.TintRenderers(grass, new Color(0.32f, 0.68f, 0.36f));

            _marker = ModelSpawner.Spawn("selection-a", position + Vector3.up * 0.04f, transform, 1.05f);
            _marker.transform.localScale = new Vector3(1.15f, 0.06f, 1.15f);
            ModelSpawner.TintRenderers(_marker, new Color(0.45f, 1f, 0.5f, 0.45f));

            var col = gameObject.AddComponent<BoxCollider>();
            col.center = Vector3.up * 1.2f;
            col.size = new Vector3(1.85f, 2.8f, 1.85f);
        }

        public bool TryBuild(Tower tower)
        {
            if (Occupied || tower == null) return false;
            Occupied = true;
            if (_marker != null) _marker.SetActive(false);

            tower.transform.SetParent(transform, false);
            tower.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            tower.transform.localRotation = Quaternion.identity;
            tower.transform.localScale = Vector3.one;
            return true;
        }

        public void Clear()
        {
            Occupied = false;
            if (_marker != null) _marker.SetActive(true);
        }
    }
}
