using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightWatch
{
    public class UIManager : MonoBehaviour
    {
        GameObject _menu, _hud, _end;
        TextMeshProUGUI _wave, _gold, _hp, _timer, _race, _msg, _endMsg;
        Button _waveBtn, _archer, _cannon;

        void Awake()
        {
            var canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = canvas.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _menu = Panel(canvas.transform, new Color(0, 0, 0, 0.4f));
            Text(_menu.transform, "Нічний Дозор", 48, new Vector2(0, 120));
            Text(_menu.transform, "Оберіть расу:", 24, new Vector2(0, 50));
            Btn(_menu.transform, "Ельфи\n+15% швидкість", new Vector2(-120, -30), new Color(0.3f, 0.7f, 0.4f),
                () => Game.I.StartWithRace(Race.Elf));
            Btn(_menu.transform, "Гноми\n+20% урон", new Vector2(120, -30), new Color(0.6f, 0.45f, 0.3f),
                () => Game.I.StartWithRace(Race.Dwarf));

            _hud = Panel(canvas.transform, Color.clear);
            _hud.GetComponent<Image>().raycastTarget = false;
            _hp = Text(_hud.transform, "HP: 100", 20, new Vector2(-500, 500), TextAlignmentOptions.TopLeft);
            _wave = Text(_hud.transform, "Хвиля 0", 22, new Vector2(-200, 500), TextAlignmentOptions.TopLeft);
            _timer = Text(_hud.transform, "Час: —", 20, new Vector2(0, 500));
            _race = Text(_hud.transform, "", 18, new Vector2(200, 500), TextAlignmentOptions.TopLeft);
            _gold = Text(_hud.transform, "Золото: 0", 24, new Vector2(500, 500), TextAlignmentOptions.TopRight);
            _msg = Text(_hud.transform, "", 18, new Vector2(0, -450));
            _archer = Btn(_hud.transform, "Лучник", new Vector2(-100, 420), new Color(0.35f, 0.75f, 0.45f),
                () => Game.I.SelectTower(TowerType.Archer));
            _cannon = Btn(_hud.transform, "Гармата", new Vector2(100, 420), new Color(0.75f, 0.45f, 0.3f),
                () => Game.I.SelectTower(TowerType.Cannon));
            _waveBtn = Btn(_hud.transform, "Хвиля", new Vector2(400, -450), Color.gray, () => Game.I.StartNextWave());

            _end = Panel(canvas.transform, new Color(0, 0, 0, 0.9f));
            _endMsg = Text(_end.transform, "", 40, Vector2.zero);
            Btn(_end.transform, "Знову", new Vector2(0, -80), Color.gray, () => Game.I.Restart());
            ShowMenu();
        }

        public void ShowMenu() => SetScreen(menu: true);
        public void ShowHud() => SetScreen(hud: true);
        public void ShowEnd(bool win, string reason = null)
        {
            _end.SetActive(true);
            _endMsg.text = win ? "Перемога!" : (reason ?? "Поразка");
        }

        void SetScreen(bool menu = false, bool hud = false)
        {
            _menu.SetActive(menu);
            _hud.SetActive(hud);
            _end.SetActive(false);
        }

        public void ShowMessage(string text) => _msg.text = text;

        public void HighlightTower(TowerType type)
        {
            bool archer = type == TowerType.Archer;
            _archer.GetComponent<Image>().color = archer ? new Color(0.35f, 0.95f, 0.45f) : new Color(0.2f, 0.45f, 0.25f);
            _cannon.GetComponent<Image>().color = archer ? new Color(0.45f, 0.28f, 0.18f) : new Color(0.95f, 0.55f, 0.25f);
        }

        public void RefreshHud()
        {
            var g = Game.I;
            if (g == null || !_hud.activeSelf) return;
            _wave.text = $"Хвиля {g.CurrentWave}/{GameConfig.WaveCount}";
            _gold.text = $"Золото: {g.Gold}";
            _hp.text = $"HP: {Mathf.CeilToInt(g.CrystalHp)}";
            _race.text = GameConfig.RaceNames[(int)g.PlayerRace];
            if (g.IsWaveActive && g.WaveTimeLeft > 0f)
            {
                int s = Mathf.CeilToInt(g.WaveTimeLeft);
                _timer.text = $"Час: {s / 60}:{s % 60:00}";
            }
            else _timer.text = "Час: —";
            _waveBtn.interactable = !g.IsWaveActive && !g.IsGameOver && g.CurrentWave < GameConfig.WaveCount;
        }

        static GameObject Panel(Transform parent, Color color)
        {
            var p = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            p.transform.SetParent(parent, false);
            var r = p.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            p.GetComponent<Image>().color = color;
            return p;
        }

        static TextMeshProUGUI Text(Transform parent, string text, float size, Vector2 pos,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var o = new GameObject("Text", typeof(RectTransform));
            o.transform.SetParent(parent, false);
            var r = o.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = new Vector2(600, 60);
            var t = o.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.raycastTarget = false;
            return t;
        }

        static Button Btn(Transform parent, string label, Vector2 pos, Color color, UnityEngine.Events.UnityAction click)
        {
            var o = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            o.transform.SetParent(parent, false);
            var r = o.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = new Vector2(180, 64);
            o.GetComponent<Image>().color = color;
            var b = o.GetComponent<Button>();
            b.onClick.AddListener(click);
            Text(o.transform, label, 16, Vector2.zero).richText = true;
            return b;
        }
    }
}
