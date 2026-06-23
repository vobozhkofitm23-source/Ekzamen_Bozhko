using UnityEngine;

namespace NightWatch
{
    public class TowerInput : MonoBehaviour
    {
        Camera _cam;
        LineRenderer _rangeLine;
        GameObject _ghostTower;
        string _ghostKey;

        void Start()
        {
            _cam = Camera.main;
            _rangeLine = RangeRingHelper.Create(null, new Color(0.1f, 1f, 0.25f, 1f), 0.38f);
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.GameStarted || gm.GameOver || gm.RewardChoicePending) return;
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            UpdatePlacementPreview(gm);

            if (!WasLeftClick()) return;
            if (IsBlockingUiClick()) return;

            var zone = gm.FindBuildZoneFromScreen(_cam, UiEventSetup.GetPointerPosition());
            if (zone != null && !zone.Occupied)
            {
                gm.DeselectTower();
                gm.TryBuildAtZoneSilent(zone);
                return;
            }

            var tower = gm.FindTowerFromScreen(_cam, UiEventSetup.GetPointerPosition());
            if (tower != null)
            {
                gm.SelectTower(tower);
                return;
            }

            gm.DeselectTower();
        }

        void UpdatePlacementPreview(GameManager gm)
        {
            if (gm.SelectedTower != null || !gm.BuildModeActive || IsBlockingUiClick())
            {
                HidePreview();
                return;
            }

            var zone = gm.FindBuildZoneFromScreen(_cam, UiEventSetup.GetPointerPosition());
            if (zone == null || zone.Occupied)
            {
                HidePreview();
                return;
            }

            float range = GameConfig.GetPreviewRange(gm.SelectedTowerType, gm.SelectedRace, 1);
            RangeRingHelper.Draw(_rangeLine, zone.transform.position, range, 0.75f);
            _rangeLine.gameObject.SetActive(true);

            string key = $"{gm.SelectedTowerType}_{gm.SelectedRace}";
            if (_ghostTower == null || _ghostKey != key)
            {
                if (_ghostTower != null) Destroy(_ghostTower);
                _ghostKey = key;
                _ghostTower = ModelSpawner.CreateTowerModel(gm.SelectedTowerType, gm.SelectedRace, null);
                _ghostTower.name = "GhostTower";
                foreach (var c in _ghostTower.GetComponentsInChildren<Collider>())
                    Destroy(c);
                foreach (var r in _ghostTower.GetComponentsInChildren<Renderer>())
                {
                    var c = r.material.color;
                    r.material.color = new Color(c.r, c.g, c.b, 0.55f);
                }
            }

            _ghostTower.transform.position = zone.transform.position + Vector3.up * 0.65f;
            _ghostTower.SetActive(true);
        }

        static bool IsBlockingUiClick() => UiEventSetup.IsPointerOverUi();

        void HidePreview()
        {
            if (_rangeLine != null) _rangeLine.gameObject.SetActive(false);
            if (_ghostTower != null) _ghostTower.SetActive(false);
        }

        void OnDestroy()
        {
            if (_rangeLine != null) Destroy(_rangeLine.gameObject);
            if (_ghostTower != null) Destroy(_ghostTower);
        }

        static bool WasLeftClick()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null &&
                UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                return true;
#endif
            return Input.GetMouseButtonDown(0);
        }
    }

    public static class RangeRingHelper
    {
        const int Segments = 96;

        public static LineRenderer Create(Transform parent, Color color, float width)
        {
            var go = new GameObject("RangeRing");
            if (parent != null)
                go.transform.SetParent(parent, false);

            var line = go.AddComponent<LineRenderer>();
            line.loop = true;
            line.useWorldSpace = true;
            line.widthMultiplier = width;
            line.positionCount = Segments;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.numCapVertices = 8;
            line.alignment = LineAlignment.View;

            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Color");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;
            mat.renderQueue = 5000;
            line.material = mat;

            go.SetActive(false);
            return line;
        }

        public static void Draw(LineRenderer line, Vector3 center, float radius, float height = 0.75f)
        {
            if (line == null) return;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i / (float)line.positionCount * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    center.y + height,
                    center.z + Mathf.Sin(angle) * radius));
            }
        }
    }
}
