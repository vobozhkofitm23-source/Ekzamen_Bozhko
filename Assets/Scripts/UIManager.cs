using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NightWatch
{
    public class UIManager : MonoBehaviour
    {
        Canvas _canvas;
        GameObject _menuPanel;
        GameObject _hudPanel;
        GameObject _endPanel;
        GameObject _towerPanel;
        GameObject _tooltipPanel;
        GameObject _rewardPanel;
        Button[] _rewardChoiceButtons = new Button[WaveRewardConfig.OfferCount];
        TextMeshProUGUI[] _rewardNameLabels = new TextMeshProUGUI[WaveRewardConfig.OfferCount];
        TextMeshProUGUI[] _rewardDescLabels = new TextMeshProUGUI[WaveRewardConfig.OfferCount];

        TextMeshProUGUI _waveText;
        TextMeshProUGUI _timerText;
        TextMeshProUGUI _goldText;
        TextMeshProUGUI _towerCountText;
        TextMeshProUGUI _objectiveText;
        TextMeshProUGUI _messageText;
        TextMeshProUGUI _endTitle;
        TextMeshProUGUI _endBody;
        TextMeshProUGUI _tooltipText;
        Image _hpFill;
        RectTransform _hpFillRt;
        RectTransform _hpDamageRt;

        Button[] _towerButtons = new Button[GameConfig.TowerTypesCount];
        Button _waveButton;
        Button _upgradeButton;
        Button _repairButton;
        Button _sellButton;
        TextMeshProUGUI _towerInfoText;
        TMP_FontAsset _font;
        int _selectedTowerIdx = -1;
        int _selectedDifficultyIdx = 1;
        Button[] _difficultyButtons = new Button[3];

        void Awake()
        {
            _font = LoadFont();
            UiEventSetup.Ensure();
            BuildUi();
        }

        static TMP_FontAsset LoadFont()
        {
            try
            {
                var def = TMP_Settings.defaultFontAsset;
                if (def != null) return def;
            }
            catch (System.Exception) { }

            var fromResources = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (fromResources != null) return fromResources;

            try
            {
                return TMP_FontAsset.CreateFontAsset("Arial", "Regular", 90);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[NightWatch] Font fallback: {e.Message}");
            }

            var osFont = Font.CreateDynamicFontFromOSFont("Arial", 36);
            return TMP_FontAsset.CreateFontAsset(osFont);
        }

        void BuildUi()
        {
            var canvasGo = new GameObject("Canvas");
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100;

            canvasGo.AddComponent<GraphicRaycaster>();

            _menuPanel = CreatePanel("MenuPanel", new Color(0.04f, 0.06f, 0.1f, 1f));
            BuildMenu(_menuPanel.transform);

            _hudPanel = CreatePanel("HudPanel", Color.clear);
            _hudPanel.GetComponent<Image>().raycastTarget = false;
            BuildHud(_hudPanel.transform);

            _towerPanel = CreateAnchoredPanel("TowerPanel", new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(20, 20), new Vector2(380, 260));
            BuildTowerPanel(_towerPanel.transform);

            _endPanel = CreatePanel("EndPanel", new Color(0.04f, 0.06f, 0.1f, 0.98f));
            BuildEndScreen(_endPanel.transform);

            BuildWaveRewardPanel();

            ShowMainMenu();
        }

        void BuildHud(Transform parent)
        {
            // Верхня смуга статистики
            var statsBar = CreateBar("StatsBar", parent, new Vector2(0, 1), new Vector2(1, 1), 64);

            // Кристал + HP — ліворуч
            var hpPanel = new GameObject("HpPanel", typeof(RectTransform));
            hpPanel.transform.SetParent(statsBar.transform, false);
            var hpRt = hpPanel.GetComponent<RectTransform>();
            hpRt.anchorMin = new Vector2(0, 0);
            hpRt.anchorMax = new Vector2(0, 1);
            hpRt.pivot = new Vector2(0, 0.5f);
            hpRt.anchoredPosition = new Vector2(16, 0);
            hpRt.sizeDelta = new Vector2(240, 0);

            AddAnchoredText(hpPanel.transform, "Кристал", 15, FontStyles.Bold,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(0, -4), new Vector2(0, 22),
                TextAlignmentOptions.MidlineLeft, new Color(0.55f, 0.95f, 1f));

            var barBg = new GameObject("HpBarBg", typeof(RectTransform));
            barBg.transform.SetParent(hpPanel.transform, false);
            var barRt = barBg.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0, 0.5f);
            barRt.anchorMax = new Vector2(1, 0.5f);
            barRt.pivot = new Vector2(0, 0.5f);
            barRt.anchoredPosition = new Vector2(0, -6);
            barRt.sizeDelta = new Vector2(0, 16);
            var barBgImg = barBg.AddComponent<Image>();
            barBgImg.color = new Color(0.12f, 0.14f, 0.18f);

            var damageGo = new GameObject("HpDamageFill", typeof(RectTransform));
            damageGo.transform.SetParent(barBg.transform, false);
            _hpDamageRt = damageGo.GetComponent<RectTransform>();
            _hpDamageRt.anchorMin = new Vector2(1f, 0f);
            _hpDamageRt.anchorMax = Vector2.one;
            _hpDamageRt.offsetMin = Vector2.zero;
            _hpDamageRt.offsetMax = Vector2.zero;
            var damageImg = damageGo.AddComponent<Image>();
            damageImg.color = new Color(0.82f, 0.18f, 0.2f);

            var fillGo = new GameObject("HpFill", typeof(RectTransform));
            fillGo.transform.SetParent(barBg.transform, false);
            _hpFillRt = fillGo.GetComponent<RectTransform>();
            _hpFillRt.anchorMin = Vector2.zero;
            _hpFillRt.anchorMax = Vector2.one;
            _hpFillRt.offsetMin = Vector2.zero;
            _hpFillRt.offsetMax = Vector2.zero;
            _hpFill = fillGo.AddComponent<Image>();
            _hpFill.color = new Color(0.25f, 0.85f, 0.45f);

            _objectiveText = AddAnchoredText(hpPanel.transform, "100/100", 14, FontStyles.Normal,
                new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -6), new Vector2(0, 20),
                TextAlignmentOptions.Center);

            // Хвиля — по центру лівої частини (не перекриває HP)
            _waveText = AddAnchoredText(statsBar.transform, "Хвиля 0/10", 22, FontStyles.Bold,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(270, 0), new Vector2(220, 0),
                TextAlignmentOptions.MidlineLeft);

            _towerCountText = AddAnchoredText(statsBar.transform, "Башні: 0", 18, FontStyles.Normal,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(270, -22), new Vector2(200, 0),
                TextAlignmentOptions.MidlineLeft, new Color(0.75f, 0.8f, 0.9f));

            _timerText = AddAnchoredText(statsBar.transform, "Час: —", 20, FontStyles.Bold,
                new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(220, 0),
                TextAlignmentOptions.Center, new Color(0.85f, 0.95f, 1f));

            // Золото — праворуч, великим шрифтом
            _goldText = AddAnchoredText(statsBar.transform, "Золото: 120", 24, FontStyles.Bold,
                new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(260, 0),
                TextAlignmentOptions.MidlineRight, new Color(1f, 0.88f, 0.35f));

            // Панель вибору башен
            var towerBar = CreateBar("TowerSelectBar", parent, new Vector2(0, 1), new Vector2(1, 1), 108);
            var towerBarRt = towerBar.GetComponent<RectTransform>();
            towerBarRt.offsetMax = new Vector2(0, -64);

            AddAnchoredText(towerBar.transform, "Оберіть башню:", 18, FontStyles.Bold,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(200, 0),
                TextAlignmentOptions.MidlineLeft, new Color(0.75f, 0.85f, 1f));

            float startX = -400f;
            float spacing = 148f;
            for (int i = 0; i < GameConfig.TowerTypesCount; i++)
            {
                int idx = i;
                var type = (TowerType)idx;
                var stats = GameConfig.GetTowerStats(type);
                var color = GameConfig.GetTowerColor(type);

                _towerButtons[i] = CreateTowerButton(towerBar.transform,
                    GameConfig.TowerNames[idx],
                    $"{stats.Cost}g",
                    new Vector2(startX + i * spacing, -8),
                    new Vector2(132, 72),
                    color,
                    () => GameManager.Instance?.ToggleTowerType(type),
                    idx);
            }

            // Підказка — під панеллю башен, не по центру екрана
            _tooltipPanel = new GameObject("TowerTooltip", typeof(RectTransform));
            _tooltipPanel.transform.SetParent(parent, false);
            var tipRt = _tooltipPanel.GetComponent<RectTransform>();
            tipRt.anchorMin = tipRt.anchorMax = new Vector2(0.5f, 1f);
            tipRt.pivot = new Vector2(0.5f, 1f);
            tipRt.anchoredPosition = new Vector2(0, -180);
            tipRt.sizeDelta = new Vector2(280, 185);
            var tipBg = _tooltipPanel.AddComponent<Image>();
            tipBg.color = new Color(0.06f, 0.1f, 0.16f, 0.96f);
            tipBg.raycastTarget = false;

            _tooltipText = AddAnchoredText(_tooltipPanel.transform, "", 16, FontStyles.Normal,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.TopLeft, new Color(0.92f, 0.95f, 1f));
            _tooltipText.margin = new Vector4(12, 10, 12, 10);
            _tooltipText.richText = true;
            _tooltipPanel.SetActive(false);

            // Низ — повідомлення + кнопка хвилі
            var bottom = CreateBar("BottomBar", parent, new Vector2(0, 0), new Vector2(1, 0), 88);

            _messageText = AddAnchoredText(bottom.transform, "", 18, FontStyles.Italic,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -8), new Vector2(-320, 28),
                TextAlignmentOptions.Top, new Color(0.85f, 0.9f, 1f));

            _waveButton = CreateButton(bottom.transform, "Почати хвилю",
                new Vector2(0, -18), new Vector2(280, 52), new Color(0.22f, 0.58f, 0.32f),
                () => GameManager.Instance?.StartNextWave());
        }

        Button CreateTowerButton(Transform parent, string name, string cost, Vector2 pos, Vector2 size,
            Color color, UnityEngine.Events.UnityAction onClick, int towerIndex)
        {
            var btn = CreateButton(parent, $"{name}\n<size=80%>{cost}</size>", pos, size, color, onClick);

            var hover = btn.gameObject.AddComponent<TowerButtonHover>();
            hover.Init(this, towerIndex);

            return btn;
        }

        public void ShowTowerTooltip(int towerIndex)
        {
            if (_tooltipPanel == null || _tooltipText == null) return;
            if (towerIndex < 0 || towerIndex >= GameConfig.TowerTypesCount) return;

            var type = (TowerType)towerIndex;
            var gm = GameManager.Instance;
            var race = gm != null ? gm.SelectedRace : RaceType.Elves;
            var diff = gm != null ? gm.SelectedDifficulty : Difficulty.Medium;
            _tooltipText.text = GameConfig.GetTowerTooltip(type, race, diff);
            _tooltipPanel.SetActive(true);

            var tipRt = _tooltipPanel.GetComponent<RectTransform>();
            float startX = -420f;
            float spacing = 148f;
            tipRt.anchoredPosition = new Vector2(startX + towerIndex * spacing, -182);
        }

        public void HideTowerTooltip()
        {
            if (_tooltipPanel != null)
                _tooltipPanel.SetActive(false);
        }

        GameObject CreatePanel(string name, Color bg)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_canvas.transform, false);
            Stretch(go.GetComponent<RectTransform>());
            var img = go.AddComponent<Image>();
            img.color = bg;
            img.raycastTarget = true;
            return go;
        }

        GameObject CreateAnchoredPanel(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_canvas.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = anchorMax;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.08f, 0.12f, 0.2f, 0.95f);
            img.raycastTarget = true;
            return go;
        }

        GameObject CreateBar(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(0, anchorMin.y < 0.5f ? 0 : -height);
            rt.offsetMax = new Vector2(0, anchorMin.y < 0.5f ? height : 0);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.06f, 0.09f, 0.14f, 0.92f);
            img.raycastTarget = true;
            return go;
        }

        void BuildMenu(Transform parent)
        {
            var card = CreateCard(parent, new Vector2(720, 580));

            AddText(card, "Нічний Дозор", 54, new Vector2(0, 240), FontStyles.Bold);
            AddText(card, "Оберіть складність:", 24, new Vector2(0, 185), FontStyles.Bold);

            var diffRow = new GameObject("DifficultyRow", typeof(RectTransform));
            diffRow.transform.SetParent(card, false);
            var diffRt = diffRow.GetComponent<RectTransform>();
            diffRt.anchorMin = diffRt.anchorMax = new Vector2(0.5f, 0.5f);
            diffRt.anchoredPosition = new Vector2(0, 130);
            diffRt.sizeDelta = new Vector2(540, 52);

            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var diff = (Difficulty)idx;
                _difficultyButtons[i] = CreateButton(diffRow.transform, DifficultyConfig.Names[i],
                    new Vector2(-180 + i * 180, 0), new Vector2(160, 52), DifficultyConfig.Colors[i],
                    () =>
                    {
                        _selectedDifficultyIdx = idx;
                        GameManager.Instance?.SetDifficulty(diff);
                        HighlightDifficultyButton(idx);
                    });
            }
            HighlightDifficultyButton(_selectedDifficultyIdx);

            AddText(card, "Оберіть расу:", 24, new Vector2(0, 70), FontStyles.Bold);

            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var race = (RaceType)idx;
                CreateButton(card, GameConfig.GetRaceMenuText(race),
                    new Vector2(0, -10 - i * 72), new Vector2(520, 58), GameConfig.RaceColors[i],
                    () => GameManager.Instance?.SelectRace(race));
            }
        }

        void HighlightDifficultyButton(int idx)
        {
            for (int i = 0; i < 3; i++)
            {
                if (_difficultyButtons[i] == null) continue;
                var baseColor = DifficultyConfig.Colors[i];
                _difficultyButtons[i].GetComponent<Image>().color = i == idx ? baseColor : baseColor * 0.55f;
            }
        }

        void BuildTowerPanel(Transform parent)
        {
            _towerInfoText = AddAnchoredText(parent, "Башня", 17, FontStyles.Normal,
                new Vector2(0, 0.35f), new Vector2(1, 1), new Vector2(0, 1), new Vector2(14, -10), new Vector2(-28, 0),
                TextAlignmentOptions.TopLeft, new Color(0.92f, 0.95f, 1f));
            _towerInfoText.richText = true;
            _towerInfoText.lineSpacing = -4f;

            _upgradeButton = CreateAnchoredButton(parent, "Апгрейд", new Vector2(0, 0), new Vector2(0.333f, 0),
                new Vector2(14, 14), new Vector2(-6, 52), new Color(0.28f, 0.48f, 0.78f),
                () => GameManager.Instance?.TryUpgradeTower());
            _repairButton = CreateAnchoredButton(parent, "Ремонт", new Vector2(0.333f, 0), new Vector2(0.666f, 0),
                new Vector2(6, 14), new Vector2(-6, 52), new Color(0.35f, 0.65f, 0.38f),
                () => GameManager.Instance?.TryRepairTower());
            _sellButton = CreateAnchoredButton(parent, "Продати", new Vector2(0.666f, 0), new Vector2(1, 0),
                new Vector2(6, 14), new Vector2(-14, 52), new Color(0.72f, 0.32f, 0.28f),
                () => GameManager.Instance?.TrySellTower());
            _repairButton.gameObject.SetActive(false);
            _towerPanel.SetActive(false);
        }

        void BuildWaveRewardPanel()
        {
            _rewardPanel = CreatePanel("WaveRewardPanel", new Color(0.02f, 0.04f, 0.08f, 0.88f));

            var card = new GameObject("RewardCard", typeof(RectTransform));
            card.transform.SetParent(_rewardPanel.transform, false);
            var rt = card.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(980, 460);
            var cardBg = card.AddComponent<Image>();
            cardBg.color = new Color(0.08f, 0.12f, 0.22f, 0.98f);
            cardBg.raycastTarget = true;

            var accent = new GameObject("Accent", typeof(RectTransform));
            accent.transform.SetParent(card.transform, false);
            Stretch(accent.GetComponent<RectTransform>());
            var accentImg = accent.AddComponent<Image>();
            accentImg.color = new Color(0.35f, 0.65f, 1f, 0.12f);
            accentImg.raycastTarget = false;

            AddAnchoredText(card.transform, "Нагорода за 4-у хвилю!", 34, FontStyles.Bold,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -28), new Vector2(0, 44),
                TextAlignmentOptions.Center, new Color(1f, 0.88f, 0.35f));

            AddAnchoredText(card.transform, "Оберіть один бонус — діє до кінця гри", 18, FontStyles.Normal,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -68), new Vector2(0, 28),
                TextAlignmentOptions.Center, new Color(0.75f, 0.82f, 0.95f));

            float[] xs = { -310f, 0f, 310f };
            for (int i = 0; i < WaveRewardConfig.OfferCount; i++)
            {
                int idx = i;
                var slot = new GameObject($"RewardSlot_{i}", typeof(RectTransform));
                slot.transform.SetParent(card.transform, false);
                var srt = slot.GetComponent<RectTransform>();
                srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
                srt.anchoredPosition = new Vector2(xs[i], -20);
                srt.sizeDelta = new Vector2(280, 300);
                var slotBg = slot.AddComponent<Image>();
                slotBg.color = new Color(0.12f, 0.16f, 0.26f, 1f);

                _rewardNameLabels[i] = AddAnchoredText(slot.transform, "Бонус", 22, FontStyles.Bold,
                    new Vector2(0, 0.55f), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -16), new Vector2(-20, 0),
                    TextAlignmentOptions.Center, Color.white);

                _rewardDescLabels[i] = AddAnchoredText(slot.transform, "Опис", 16, FontStyles.Normal,
                    new Vector2(0, 0.22f), new Vector2(1, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-24, 0),
                    TextAlignmentOptions.Center, new Color(0.82f, 0.88f, 0.98f));

                _rewardChoiceButtons[i] = CreateButton(slot.transform, "Обрати",
                    new Vector2(0, -95), new Vector2(200, 48), new Color(0.28f, 0.55f, 0.38f),
                    () => { });
            }

            _rewardPanel.SetActive(false);
        }

        public void ShowWaveRewardPanel(WaveRewardType[] offers)
        {
            if (_rewardPanel == null || offers == null) return;
            _rewardPanel.SetActive(true);
            HideTowerTooltip();
            HideTowerPanel();

            for (int i = 0; i < WaveRewardConfig.OfferCount; i++)
            {
                if (i >= offers.Length || _rewardChoiceButtons[i] == null) continue;
                var def = WaveRewardConfig.Get(offers[i]);
                _rewardNameLabels[i].text = def.Name;
                _rewardDescLabels[i].text = def.Description;

                var img = _rewardChoiceButtons[i].GetComponent<Image>();
                img.color = def.Color * 0.85f;

                _rewardChoiceButtons[i].onClick.RemoveAllListeners();
                var type = offers[i];
                _rewardChoiceButtons[i].onClick.AddListener(() => GameManager.Instance?.ChooseWaveReward(type));
            }
        }

        public void HideWaveRewardPanel()
        {
            if (_rewardPanel != null)
                _rewardPanel.SetActive(false);
        }

        void BuildEndScreen(Transform parent)
        {
            var card = CreateCard(parent, new Vector2(580, 340));
            _endTitle = AddText(card, "Перемога!", 60, new Vector2(0, 70), FontStyles.Bold);
            _endBody = AddText(card, "", 26, new Vector2(0, 0));
            CreateButton(card, "Грати знову", new Vector2(0, -90), new Vector2(280, 60),
                new Color(0.28f, 0.48f, 0.78f), () => GameManager.Instance?.RestartGame());
        }

        public void ShowMainMenu()
        {
            if (_menuPanel == null) return;
            _menuPanel.SetActive(true);
            if (_hudPanel != null) _hudPanel.SetActive(false);
            if (_endPanel != null) _endPanel.SetActive(false);
            if (_towerPanel != null) _towerPanel.SetActive(false);
            HideWaveRewardPanel();
            HideTowerTooltip();
            GameManager.Instance?.SetDifficulty((Difficulty)_selectedDifficultyIdx);
            HighlightDifficultyButton(_selectedDifficultyIdx);
        }

        public void ShowGameHud()
        {
            if (_hudPanel == null) return;
            if (_menuPanel != null) _menuPanel.SetActive(false);
            _hudPanel.SetActive(true);
            if (_endPanel != null) _endPanel.SetActive(false);
        }

        public void ShowEndScreen(bool victory)
        {
            if (_endPanel == null || _endTitle == null || _endBody == null) return;
            _endPanel.SetActive(true);
            _endTitle.text = victory ? "Перемога!" : "Поразка";
            _endTitle.color = victory ? new Color(0.45f, 1f, 0.55f) : new Color(1f, 0.45f, 0.45f);
            _endBody.text = victory
                ? "Ви перемогли боса на 10-й хвилі та захистили кристал!"
                : "Кристал знищено. Спробуйте іншу расу або розстановку башен.";
        }

        public void ShowTowerPanel(Tower tower)
        {
            if (tower == null) return;
            _towerPanel.SetActive(true);
            int cost = tower.GetUpgradeCost();
            int refund = tower.GetSellRefund();
            var gm = GameManager.Instance;
            bool hell = gm != null && DifficultyConfig.Get(gm.SelectedDifficulty).TowerRepairEnabled;

            _towerInfoText.text = tower.GetStatsText();
            _upgradeButton.interactable = cost >= 0 && gm != null && gm.Gold >= cost;
            _sellButton.interactable = true;

            if (_repairButton != null)
            {
                _repairButton.gameObject.SetActive(hell);
                if (hell)
                {
                    int repairCost = tower.GetRepairCost();
                    _repairButton.interactable = tower.NeedsRepair() && gm != null && gm.Gold >= repairCost;
                    var repLabel = _repairButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (repLabel != null)
                        repLabel.text = tower.IsBroken ? $"Ремонт ({repairCost}g)" :
                            tower.NeedsRepair() ? $"Ремонт ({repairCost}g)" : "OK";
                }
            }

            var upLabel = _upgradeButton.GetComponentInChildren<TextMeshProUGUI>();
            if (upLabel != null)
                upLabel.text = cost >= 0 ? $"Апгрейд ({cost}g)" : "MAX";

            var sellLabel = _sellButton.GetComponentInChildren<TextMeshProUGUI>();
            if (sellLabel != null)
                sellLabel.text = $"Продати +{refund}";
        }

        public void HideTowerPanel() => _towerPanel.SetActive(false);

        public void RefreshHud()
        {
            if (GameManager.Instance == null || _hudPanel == null || !_hudPanel.activeSelf) return;
            var gm = GameManager.Instance;
            if (_waveText != null) _waveText.text = $"Хвиля {gm.CurrentWave}/{GameConfig.WavesPerLevel}";
            if (_goldText != null) _goldText.text = $"Золото: {gm.Gold}";
            if (_towerCountText != null) _towerCountText.text = $"Башні: {gm.Towers.Count}";

            if (_timerText != null)
            {
                if (gm.WaveActive)
                {
                    if (gm.WaveOvertime)
                    {
                        _timerText.text = "Час: 0:00";
                        _timerText.color = new Color(1f, 0.35f, 0.35f);
                    }
                    else
                    {
                        int sec = Mathf.CeilToInt(gm.WaveTimeRemaining);
                        _timerText.text = $"Час: {sec / 60}:{sec % 60:00}";
                        _timerText.color = sec <= 10
                            ? new Color(1f, 0.65f, 0.35f)
                            : new Color(0.85f, 0.95f, 1f);
                    }
                }
                else
                {
                    _timerText.text = "Час: —";
                    _timerText.color = new Color(0.65f, 0.72f, 0.82f);
                }
            }

            if (gm.Crystal != null && _hpFillRt != null && _objectiveText != null)
            {
                float ratio = Mathf.Clamp01(gm.Crystal.CurrentHp / gm.Crystal.MaxHp);
                _hpFillRt.anchorMin = Vector2.zero;
                _hpFillRt.anchorMax = new Vector2(ratio, 1f);
                if (_hpDamageRt != null)
                {
                    _hpDamageRt.anchorMin = new Vector2(ratio, 0f);
                    _hpDamageRt.anchorMax = Vector2.one;
                }
                _objectiveText.text = $"{Mathf.CeilToInt(gm.Crystal.CurrentHp)}/{Mathf.CeilToInt(gm.Crystal.MaxHp)}";
            }

            UpdateTowerButtonAffordability(gm.Gold);

            if (_waveButton != null)
                _waveButton.interactable = !gm.RewardChoicePending && !gm.WaveActive && !gm.GameOver
                    && gm.CurrentWave < GameConfig.WavesPerLevel;

            if (gm.SelectedTower != null && _towerPanel != null && _towerPanel.activeSelf)
                ShowTowerPanel(gm.SelectedTower);
        }

        void UpdateTowerButtonAffordability(int gold)
        {
            for (int i = 0; i < GameConfig.TowerTypesCount; i++)
            {
                if (_towerButtons[i] == null) continue;
                var stats = GameConfig.GetTowerStats((TowerType)i);
                bool canAfford = gold >= stats.Cost;
                var img = _towerButtons[i].GetComponent<Image>();
                var baseColor = GameConfig.GetTowerColor((TowerType)i);
                if (_selectedTowerIdx >= 0 && _selectedTowerIdx == i)
                    img.color = baseColor;
                else
                    img.color = canAfford ? baseColor * 0.85f : baseColor * 0.4f;
                _towerButtons[i].interactable = canAfford || _selectedTowerIdx == i;
            }
        }

        public void SetMessage(string msg)
        {
            if (_messageText != null) _messageText.text = msg;
        }

        public void HighlightTowerButton(int idx)
        {
            _selectedTowerIdx = idx;
            if (GameManager.Instance == null) return;
            for (int i = 0; i < GameConfig.TowerTypesCount; i++)
            {
                if (_towerButtons[i] == null) continue;
                var baseColor = GameConfig.GetTowerColor((TowerType)i);
                if (idx < 0)
                    _towerButtons[i].GetComponent<Image>().color = baseColor * 0.65f;
                else
                    _towerButtons[i].GetComponent<Image>().color = i == idx ? baseColor : baseColor * 0.65f;
            }
            UpdateTowerButtonAffordability(GameManager.Instance.Gold);
        }

        Transform CreateCard(Transform parent, Vector2 size)
        {
            var card = new GameObject("Card", typeof(RectTransform));
            card.transform.SetParent(parent, false);
            var rt = card.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            var bg = card.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.14f, 0.22f, 1f);
            bg.raycastTarget = true;
            return card.transform;
        }

        TextMeshProUGUI AddAnchoredText(Transform parent, string text, float size, FontStyles style,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta,
            TextAlignmentOptions align, Color? color = null)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = color ?? Color.white;
            tmp.font = _font;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
            return tmp;
        }

        TextMeshProUGUI AddText(Transform parent, string text, float size, Vector2 pos,
            FontStyles style = FontStyles.Normal, TextAnchor anchor = TextAnchor.MiddleCenter, Color? color = null)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(800, 80);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = AnchorToAlignment(anchor);
            tmp.color = color ?? Color.white;
            tmp.font = _font;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
            return tmp;
        }

        Button CreateButton(Transform parent, string label, Vector2 pos, Vector2 size, Color color,
            UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Button", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var c = btn.colors;
            c.normalColor = color;
            c.highlightedColor = Color.Lerp(color, Color.white, 0.35f);
            c.pressedColor = color * 0.7f;
            c.disabledColor = color * 0.35f;
            btn.colors = c;
            btn.onClick.AddListener(onClick);

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            Stretch(textGo.GetComponent<RectTransform>());
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 17;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.font = _font;
            tmp.raycastTarget = false;
            tmp.richText = true;

            return btn;
        }

        Button CreateAnchoredButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Button", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var c = btn.colors;
            c.normalColor = color;
            c.highlightedColor = Color.Lerp(color, Color.white, 0.35f);
            c.pressedColor = color * 0.7f;
            c.disabledColor = color * 0.35f;
            btn.colors = c;
            btn.onClick.AddListener(onClick);

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            Stretch(textGo.GetComponent<RectTransform>());
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 17;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.font = _font;
            tmp.raycastTarget = false;
            tmp.richText = true;

            return btn;
        }

        static GameObject CreateImage(Transform parent, Color color)
        {
            var go = new GameObject("Fill", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return go;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static TextAlignmentOptions AnchorToAlignment(TextAnchor anchor) => anchor switch
        {
            TextAnchor.MiddleLeft => TextAlignmentOptions.MidlineLeft,
            TextAnchor.MiddleRight => TextAlignmentOptions.MidlineRight,
            TextAnchor.LowerCenter => TextAlignmentOptions.BottomGeoAligned,
            _ => TextAlignmentOptions.Center
        };
    }

    public class TowerButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        UIManager _ui;
        int _index;

        public void Init(UIManager ui, int index)
        {
            _ui = ui;
            _index = index;
        }

        public void OnPointerEnter(PointerEventData eventData) => _ui?.ShowTowerTooltip(_index);
        public void OnPointerExit(PointerEventData eventData) => _ui?.HideTowerTooltip();
    }
}
