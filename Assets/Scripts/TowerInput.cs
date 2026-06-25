// Кліки миші: поставити башню або вибрати існуючу.
using UnityEngine;

namespace NightWatch
{
    public class TowerInput : MonoBehaviour
    {
        Camera _cam;
        LineRenderer _rangeLine;

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

            var pointer = UiEventSetup.GetPointerPosition();
            UpdateRangePreview(gm, pointer);

            if (!WasLeftClick() || UiEventSetup.IsPointerOverUi()) return;

            var zone = gm.FindBuildZoneFromScreen(_cam, pointer);
            if (zone != null && !zone.Occupied)
            {
                gm.DeselectTower();
                gm.TryBuildAtZone(zone);
                return;
            }

            var tower = gm.FindTowerFromScreen(_cam, pointer);
            if (tower != null) gm.SelectTower(tower);
            else gm.DeselectTower();
        }

        void UpdateRangePreview(GameManager gm, Vector2 pointer)
        {
            if (gm.SelectedTower != null || !gm.BuildModeActive || UiEventSetup.IsPointerOverUi())
            {
                _rangeLine.gameObject.SetActive(false);
                return;
            }

            var zone = gm.FindBuildZoneFromScreen(_cam, pointer);
            if (zone == null || zone.Occupied)
            {
                _rangeLine.gameObject.SetActive(false);
                return;
            }

            var stats = GameConfig.GetTowerStats(gm.SelectedTowerType);
            float range = GameConfig.GetTowerCombat(stats, gm.SelectedTowerType, 1, gm.SelectedRace, gm.ActiveRewards).Range;
            RangeRingHelper.Draw(_rangeLine, zone.transform.position, range, 0.75f);
            _rangeLine.gameObject.SetActive(true);
        }

        void OnDestroy()
        {
            if (_rangeLine != null) Destroy(_rangeLine.gameObject);
        }

        static bool WasLeftClick()
        {
#if ENABLE_INPUT_SYSTEM
            return UnityEngine.InputSystem.Mouse.current != null &&
                   UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }
    }

    public static class RangeRingHelper
    {
        public static LineRenderer Create(Transform parent, Color color, float width)
        {
            var go = new GameObject("RangeRing");
            if (parent != null) go.transform.SetParent(parent, false);

            var line = go.AddComponent<LineRenderer>();
            line.loop = true;
            line.useWorldSpace = true;
            line.widthMultiplier = width;
            line.positionCount = 48;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.alignment = LineAlignment.View;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;
            line.material = mat;
            go.SetActive(false);
            return line;
        }

        public static void Draw(LineRenderer line, Vector3 center, float radius, float height = 0.75f)
        {
            if (line == null) return;
            for (int i = 0; i < line.positionCount; i++)
            {
                float a = i / (float)line.positionCount * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(center.x + Mathf.Cos(a) * radius, center.y + height, center.z + Mathf.Sin(a) * radius));
            }
        }
    }
}
