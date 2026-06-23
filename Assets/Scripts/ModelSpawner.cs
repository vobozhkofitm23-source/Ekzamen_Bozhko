using System.Collections.Generic;
using UnityEngine;

namespace NightWatch
{
    public static class ModelSpawner
    {
        static readonly Dictionary<string, GameObject> _cache = new();
        static Material _kenneyMat;
        static bool _initialized;

        public static void WarmUp()
        {
            if (_initialized) return;
            _initialized = true;

            foreach (var prefab in Resources.LoadAll<GameObject>("Models"))
            {
                if (prefab != null && !_cache.ContainsKey(prefab.name))
                    _cache[prefab.name] = prefab;
            }

            EnsureKenneyMaterial();
        }

        public static GameObject Spawn(string modelName, Vector3 position, Transform parent, float scale = 1f)
        {
            WarmUp();

            GameObject go;
            if (_cache.TryGetValue(modelName, out var prefab) && prefab != null)
            {
                go = Object.Instantiate(prefab, position, Quaternion.identity, parent);
                go.name = modelName;
                go.transform.localScale = Vector3.one * scale;
                EnsureKenneyMaterialOnRenderers(go);
            }
            else
            {
                go = CreateGenericFallback(modelName, position, parent, scale);
            }

            foreach (var c in go.GetComponentsInChildren<Collider>())
                Object.Destroy(c);

            return go;
        }

        public static GameObject CreateTowerModel(TowerType type, RaceType race, Transform parent)
        {
            WarmUp();
            return BuildProceduralTower(type, race, parent);
        }

        public static GameObject CreateEnemyModel(EnemyType type, bool isBoss, Transform parent)
        {
            WarmUp();
            return BuildProceduralEnemy(type, isBoss, parent);
        }

        static GameObject BuildProceduralTower(TowerType type, RaceType race, Transform parent)
        {
            var root = new GameObject("TowerModel");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;

            var baseColor = GameConfig.RaceColors[(int)race] * 0.65f;
            var accent = GameConfig.GetTowerColor(type);

            AddPart(root, PrimitiveType.Cylinder, Vector3.zero, new Vector3(1.35f, 0.14f, 1.35f), baseColor * 0.85f);
            AddPart(root, PrimitiveType.Cylinder, new Vector3(0, 0.08f, 0), new Vector3(1.15f, 0.06f, 1.15f),
                Color.Lerp(baseColor, accent, 0.35f), true);

            switch (type)
            {
                case TowerType.Archer:
                    AddPart(root, PrimitiveType.Cylinder, new Vector3(0, 0.35f, 0), new Vector3(0.55f, 0.5f, 0.55f), accent, true);
                    AddPart(root, PrimitiveType.Cube, new Vector3(0, 0.65f, 0.35f), new Vector3(0.15f, 0.15f, 0.55f), accent * 1.15f, true);
                    break;
                case TowerType.Cannon:
                    AddPart(root, PrimitiveType.Cylinder, new Vector3(0, 0.4f, 0), new Vector3(0.75f, 0.55f, 0.75f), accent, true);
                    AddPart(root, PrimitiveType.Cylinder, new Vector3(0, 0.55f, 0.45f), new Vector3(0.22f, 0.22f, 0.7f),
                        new Color(0.32f, 0.32f, 0.36f), true);
                    break;
                case TowerType.Mortar:
                    AddPart(root, PrimitiveType.Cube, new Vector3(0, 0.35f, 0), new Vector3(0.9f, 0.45f, 0.9f), accent, true);
                    AddPart(root, PrimitiveType.Sphere, new Vector3(0, 0.75f, 0), new Vector3(0.5f, 0.35f, 0.5f),
                        new Color(0.95f, 0.55f, 0.18f), true);
                    break;
                case TowerType.Freeze:
                    AddPart(root, PrimitiveType.Cylinder, new Vector3(0, 0.45f, 0), new Vector3(0.6f, 0.65f, 0.6f), accent, true);
                    AddPart(root, PrimitiveType.Sphere, new Vector3(0, 0.95f, 0), new Vector3(0.35f, 0.35f, 0.35f),
                        new Color(0.75f, 0.98f, 1f), true);
                    break;
                case TowerType.Lightning:
                    AddPart(root, PrimitiveType.Cube, new Vector3(0, 0.5f, 0), new Vector3(0.65f, 0.75f, 0.65f), accent, true);
                    AddPart(root, PrimitiveType.Cylinder, new Vector3(0, 1.05f, 0), new Vector3(0.08f, 0.45f, 0.08f),
                        new Color(0.9f, 0.8f, 1f), true);
                    break;
                default:
                    AddPart(root, PrimitiveType.Cylinder, new Vector3(0, 0.4f, 0), new Vector3(0.5f, 0.7f, 0.5f), accent, true);
                    AddPart(root, PrimitiveType.Cube, new Vector3(0, 0.85f, 0.4f), new Vector3(0.12f, 0.12f, 0.9f),
                        new Color(0.42f, 0.42f, 0.48f), true);
                    break;
            }

            root.transform.localScale = Vector3.one * 1.4f;
            return root;
        }

        static GameObject BuildProceduralEnemy(EnemyType type, bool isBoss, Transform parent)
        {
            var root = new GameObject("EnemyModel");
            root.transform.SetParent(parent, false);

            if (isBoss)
            {
                AddPart(root, PrimitiveType.Cube, Vector3.zero, new Vector3(2.2f, 1.8f, 2.2f), new Color(0.75f, 0.2f, 0.95f));
                AddPart(root, PrimitiveType.Sphere, new Vector3(0, 1.4f, 0), new Vector3(1.2f, 1.2f, 1.2f), new Color(1f, 0.35f, 1f));
                return root;
            }

            switch (type)
            {
                case EnemyType.Scout:
                    AddPart(root, PrimitiveType.Sphere, new Vector3(0, 0.55f, 0), new Vector3(0.9f, 0.9f, 0.9f), new Color(1f, 0.25f, 0.25f));
                    break;
                case EnemyType.Fighter:
                    AddPart(root, PrimitiveType.Cube, new Vector3(0, 0.6f, 0), new Vector3(0.85f, 1.1f, 0.85f), new Color(1f, 0.7f, 0.15f));
                    break;
                default:
                    AddPart(root, PrimitiveType.Cube, new Vector3(0, 0.7f, 0), new Vector3(1.2f, 1.3f, 1.2f), new Color(0.35f, 0.5f, 1f));
                    break;
            }

            return root;
        }

        static void AddPart(GameObject root, PrimitiveType shape, Vector3 localPos, Vector3 scale, Color color,
            bool accent = false)
        {
            var part = GameObject.CreatePrimitive(shape);
            part.transform.SetParent(root.transform, false);
            part.transform.localPosition = localPos;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().material = CreateTowerMaterial(color, accent);
            Object.Destroy(part.GetComponent<Collider>());
        }

        static Material CreateTowerMaterial(Color color, bool accent)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(shader);
            var lit = Color.Lerp(color, Color.white, accent ? 0.28f : 0.14f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", lit);
            else mat.color = lit;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", accent ? 0.72f : 0.48f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", accent ? 0.38f : 0.22f);
            return mat;
        }

        static GameObject CreateGenericFallback(string name, Vector3 pos, Transform parent, float scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name + "_fallback";
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            var r = go.GetComponent<Renderer>();
            r.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            r.material.color = name.Contains("enemy") ? Color.red :
                name.Contains("crystal") ? Color.cyan :
                name.Contains("selection") ? new Color(0.3f, 0.8f, 0.3f) :
                new Color(0.35f, 0.55f, 0.35f);
            return go;
        }

        static void StripColliders(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>())
                Object.Destroy(c);
        }

        static void EnsureKenneyMaterialOnRenderers(GameObject root)
        {
            EnsureKenneyMaterial();
            if (_kenneyMat == null) return;

            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || mats[i].mainTexture == null)
                        mats[i] = _kenneyMat;
                }
                r.sharedMaterials = mats;
            }
        }

        static void EnsureKenneyMaterial()
        {
            if (_kenneyMat != null) return;

            var tex = Resources.Load<Texture2D>("Models/Textures/colormap");
            if (tex == null) return;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            _kenneyMat = new Material(shader);
            _kenneyMat.mainTexture = tex;
            _kenneyMat.SetTexture("_BaseMap", tex);
            _kenneyMat.SetColor("_BaseColor", Color.white);
            _kenneyMat.SetFloat("_Smoothness", 0.08f);
        }

        public static void TintRenderers(GameObject root, Color color)
        {
            EnsureKenneyMaterial();
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                var baseMat = r.sharedMaterial != null ? r.sharedMaterial :
                    (_kenneyMat != null ? _kenneyMat : new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")));
                var mat = new Material(baseMat);
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);
                else
                    mat.color = color;
                r.material = mat;
            }
        }
    }
}
