// Головний скрипт: золото, хвилі, будівництво, перемога/поразка.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NightWatch
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public RaceType SelectedRace;
        public Difficulty SelectedDifficulty = Difficulty.Medium;
        public TowerType SelectedTowerType = TowerType.Archer;
        public int CurrentWave;
        public int Gold = GameConfig.StartingGold;
        public bool WaveActive;
        public bool GameStarted;
        public bool GameOver;
        public float WaveTimeRemaining { get; private set; }
        public bool WaveOvertime { get; private set; }

        public float CrystalMaxHp { get; private set; }
        public float CrystalHp { get; private set; }
        public Vector3[][] SpawnPaths { get; private set; }
        public List<BuildZone> BuildZones { get; } = new();
        public List<Tower> Towers { get; } = new();
        public List<Enemy> ActiveEnemies { get; } = new();
        public Tower SelectedTower { get; private set; }
        public bool BuildModeActive { get; private set; } = true;
        public bool RewardChoicePending { get; private set; }
        public WaveRewardBonuses ActiveRewards { get; } = new();

        public DifficultyParams Diff => DifficultyConfig.Get(SelectedDifficulty);

        WaveRewardType[] _rewardOffers;
        Transform _levelRoot;
        Transform _enemyRoot;
        UIManager _ui;
        bool _levelBuilt;
        bool _spawnDone;
        int _spawnIndex;
        Coroutine _spawnRoutine;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            ModelSpawner.WarmUp();
            _ui = FindFirstObjectByType<UIManager>();
            _ui?.ShowMainMenu();
        }

        void Update()
        {
            if (!GameStarted || GameOver || RewardChoicePending) return;

            if (WaveActive)
            {
                TickWaveTimer();
                ActiveEnemies.RemoveAll(e => e == null);
                if (_spawnDone && ActiveEnemies.Count == 0)
                    OnWaveComplete();
            }

            _ui?.RefreshHud();
        }

        void TickWaveTimer()
        {
            if (WaveTimeRemaining > 0f)
            {
                WaveTimeRemaining -= Time.deltaTime;
                if (WaveTimeRemaining <= 0f)
                {
                    WaveTimeRemaining = 0f;
                    WaveOvertime = true;
                    _ui?.SetMessage("Час вийшов! Кристал отримує 2 урон/с — знищте ворогів!");
                }
                return;
            }

            if (WaveOvertime)
                DamageCrystal(GameConfig.OvertimeCrystalDamagePerSecond * Time.deltaTime);
        }

        public void SetCrystalHp(float max) { CrystalMaxHp = max; CrystalHp = max; }
        public void AddCrystalHp(float amount) { CrystalMaxHp += amount; CrystalHp += amount; }
        public void ResetCrystalHp() => CrystalHp = CrystalMaxHp;

        public void DamageCrystal(float amount)
        {
            CrystalHp = Mathf.Max(0f, CrystalHp - amount);
            if (CrystalHp <= 0f) OnObjectiveDestroyed();
        }

        public void SetDifficulty(Difficulty difficulty) => SelectedDifficulty = difficulty;

        public void SelectRace(RaceType race)
        {
            SelectedRace = race;
            Gold = GameConfig.StartingGold;
            CurrentWave = 0;
            GameOver = false;
            GameStarted = true;
            WaveActive = false;
            ActiveRewards.Reset();

            if (!_levelBuilt) BuildLevel();
            else ResetLevelState();

            SetCrystalHp(Diff.CrystalHp);

            _ui?.ShowGameHud();
            BuildModeActive = true;
            _ui?.HighlightTowerButton((int)SelectedTowerType);
            var diffName = DifficultyConfig.Names[(int)SelectedDifficulty];
            _ui?.SetMessage($"{diffName} · {GameConfig.RaceNames[(int)race]}. Оберіть башню зверху.");
        }

        public void ToggleTowerType(TowerType type)
        {
            if (BuildModeActive && SelectedTowerType == type)
            {
                BuildModeActive = false;
                DeselectTower();
                _ui?.HighlightTowerButton(-1);
                _ui?.SetMessage("Вільний режим");
                return;
            }

            SelectedTowerType = type;
            BuildModeActive = true;
            DeselectTower();
            _ui?.HighlightTowerButton((int)type);
            _ui?.SetMessage($"Башня: {GameConfig.TowerNames[(int)type]}. Наведіть на зелену клітинку.");
        }

        public void SelectTower(Tower tower)
        {
            if (tower != null && tower == SelectedTower)
            {
                DeselectTower();
                _ui?.SetMessage("Вільний режим");
                return;
            }

            if (SelectedTower != null)
                SelectedTower.SetSelected(false);
            SelectedTower = tower;
            if (tower != null)
            {
                tower.SetSelected(true);
                _ui?.ShowTowerPanel(tower);
                _ui?.SetMessage($"{GameConfig.TowerNames[(int)tower.Type]} — рівень {tower.Level}/3");
            }
        }

        public void DeselectTower()
        {
            if (SelectedTower != null)
                SelectedTower.SetSelected(false);
            SelectedTower = null;
            _ui?.HideTowerPanel();
        }

        public void TryBuildAtZone(BuildZone zone)
        {
            if (!BuildModeActive || zone == null || zone.Occupied || !GameStarted || GameOver) return;

            var stats = GameConfig.GetTowerStats(SelectedTowerType);
            if (Gold < stats.Cost)
            {
                _ui?.SetMessage("Недостатньо золота!");
                return;
            }

            Gold -= stats.Cost;

            var go = new GameObject($"Tower_{SelectedTowerType}");
            var tower = go.AddComponent<Tower>();

            if (!tower.Init(SelectedRace, SelectedTowerType, zone))
            {
                Gold += stats.Cost;
                Destroy(go);
                _ui?.SetMessage("Не вдалося поставити башню!");
                return;
            }

            DeselectTower();
            _ui?.SetMessage($"{GameConfig.TowerNames[(int)SelectedTowerType]} побудовано!");
        }

        public BuildZone FindBuildZoneFromScreen(Camera cam, Vector2 screenPos)
        {
            if (cam == null) return null;
            var ray = cam.ScreenPointToRay(screenPos);

            BuildZone zone = null;
            float best = float.MaxValue;
            foreach (var hit in Physics.RaycastAll(ray, 400f))
            {
                var z = hit.collider.GetComponent<BuildZone>() ?? hit.collider.GetComponentInParent<BuildZone>();
                if (z != null && hit.distance < best) { best = hit.distance; zone = z; }
            }
            if (zone != null && !zone.Occupied) return zone;

            if (!RayToGround(ray, out Vector3 ground)) return null;
            return FindNearestBuildZone(ground);
        }

        public Tower FindTowerFromScreen(Camera cam, Vector2 screenPos)
        {
            if (cam == null) return null;
            var ray = cam.ScreenPointToRay(screenPos);

            Tower tower = null;
            float best = float.MaxValue;
            foreach (var hit in Physics.RaycastAll(ray, 400f))
            {
                var t = hit.collider.GetComponentInParent<Tower>();
                if (t != null && hit.distance < best) { best = hit.distance; tower = t; }
            }
            if (tower != null) return tower;

            if (!RayToGround(ray, out Vector3 ground)) return null;

            Tower nearest = null;
            float bestDist = float.MaxValue;
            foreach (var t in Towers)
            {
                if (t == null) continue;
                var p = t.transform.position;
                float d = Vector2.Distance(new Vector2(ground.x, ground.z), new Vector2(p.x, p.z));
                if (d <= 2f && d < bestDist) { bestDist = d; nearest = t; }
            }
            return nearest;
        }

        static bool RayToGround(Ray ray, out Vector3 point)
        {
            if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float enter))
            {
                point = ray.GetPoint(enter);
                return true;
            }
            point = default;
            return false;
        }

        BuildZone FindNearestBuildZone(Vector3 worldPos)
        {
            BuildZone closest = null;
            float best = float.MaxValue;
            float maxDist = LevelMap.CellSize * 0.65f;
            foreach (var zone in BuildZones)
            {
                if (zone == null || zone.Occupied) continue;
                var zp = zone.transform.position;
                float d = Vector2.Distance(new Vector2(worldPos.x, worldPos.z), new Vector2(zp.x, zp.z));
                if (d <= maxDist && d < best) { best = d; closest = zone; }
            }
            return closest;
        }

        public void TryUpgradeTower()
        {
            if (SelectedTower == null) return;
            int cost = SelectedTower.GetUpgradeCost();
            if (cost < 0) { _ui?.SetMessage("Макс. рівень (3)!"); return; }
            if (Gold < cost) { _ui?.SetMessage("Недостатньо золота!"); return; }
            Gold -= cost;
            SelectedTower.Upgrade();
            _ui?.ShowTowerPanel(SelectedTower);
            _ui?.SetMessage($"Апгрейд! Рівень {SelectedTower.Level}/3");
        }

        public void TryRepairTower()
        {
            if (SelectedTower == null) return;
            if (!Diff.TowerRepairEnabled)
            {
                _ui?.SetMessage("Ремонт тільки в режимі АД!");
                return;
            }

            if (!SelectedTower.NeedsRepair())
            {
                _ui?.SetMessage("Башня не потребує ремонту.");
                return;
            }

            int cost = SelectedTower.GetRepairCost();
            if (Gold < cost)
            {
                _ui?.SetMessage($"Недостатньо золота! Ремонт: {cost}g");
                return;
            }

            Gold -= cost;
            SelectedTower.Repair();
            _ui?.ShowTowerPanel(SelectedTower);
            _ui?.SetMessage($"Башню відремонтовано! -{cost} золота");
        }

        public void TrySellTower()
        {
            if (SelectedTower == null) return;
            int refund = SelectedTower.GetSellRefund();
            var zone = SelectedTower.Zone;
            Towers.Remove(SelectedTower);
            if (zone != null) zone.Clear();
            Destroy(SelectedTower.gameObject);
            Gold += refund;
            DeselectTower();
            _ui?.SetMessage($"Башню продано! +{refund} золота (70% витрат)");
        }

        public void StartNextWave()
        {
            if (WaveActive || GameOver || RewardChoicePending) return;

            if (CurrentWave >= GameConfig.WavesPerLevel)
            {
                _ui?.SetMessage("Всі 10 хвиль пройдено!");
                return;
            }

            CurrentWave++;
            WaveActive = true;
            WaveTimeRemaining = GameConfig.GetWaveTimeLimit(CurrentWave - 1, SelectedDifficulty);
            WaveOvertime = false;
            float limit = WaveTimeRemaining;

            if (GameConfig.IsBossWave(CurrentWave - 1))
                _ui?.SetMessage($"ХВИЛЯ 10 — БОС! Час: {limit:0}с ({DifficultyConfig.Names[(int)SelectedDifficulty]})");
            else
                _ui?.SetMessage($"Хвиля {CurrentWave}/{GameConfig.WavesPerLevel} — {limit:0}с на перемогу!");

            _spawnDone = false;
            _spawnIndex = 0;
            if (_spawnRoutine != null) StopCoroutine(_spawnRoutine);
            _spawnRoutine = StartCoroutine(SpawnWave(CurrentWave - 1));
        }

        IEnumerator SpawnWave(int waveIndex)
        {
            var paths = SpawnPaths;

            if (GameConfig.IsBossWave(waveIndex))
            {
                SetMessage("БОС! Викликає швидких міньонів!");
                int scouts = Mathf.CeilToInt(3 * Diff.EnemyCountMult);
                for (int i = 0; i < scouts; i++)
                {
                    SpawnEnemy(EnemyType.Scout, waveIndex, paths[i % paths.Length]);
                    yield return new WaitForSeconds(1.2f);
                }
                yield return new WaitForSeconds(2f);
                if (paths.Length > 1) SpawnEnemy(EnemyType.Tank, waveIndex, paths[1], true);
                _spawnDone = true;
                yield break;
            }

            var composition = GameConfig.GetWaveComposition(waveIndex, SelectedDifficulty);
            float pause = GameConfig.MiniWavePause * Diff.MiniWavePauseMult;
            int miniCount = 0;

            for (int t = 0; t < GameConfig.EnemyTypesCount; t++)
            {
                for (int i = 0; i < composition[t]; i++)
                {
                    SpawnEnemy((EnemyType)t, waveIndex, paths[_spawnIndex++ % paths.Length]);
                    miniCount++;
                    yield return new WaitForSeconds(GameConfig.EnemySpawnInterval);
                    if (miniCount >= GameConfig.MiniWaveSize)
                    {
                        miniCount = 0;
                        SetMessage("Міні-хвиля... наступна група");
                        yield return new WaitForSeconds(pause);
                    }
                }
                if (composition[t] > 0) yield return new WaitForSeconds(1.2f);
            }

            _spawnDone = true;
        }

        void SpawnEnemy(EnemyType type, int waveIndex, Vector3[] path, bool boss = false)
        {
            if (path == null || path.Length == 0) return;
            var go = new GameObject(boss ? "Boss" : $"Enemy_{type}");
            go.transform.SetParent(_enemyRoot);
            go.transform.position = path[0];
            go.AddComponent<Enemy>().Initialize(type, waveIndex, path, boss);
        }

        void OnWaveComplete()
        {
            WaveActive = false;
            WaveOvertime = false;
            WaveTimeRemaining = 0f;
            int waveGold = Mathf.RoundToInt(GameConfig.GoldPerWave * Diff.WaveGoldMult) + ActiveRewards.BonusGoldPerWave;
            Gold += waveGold;

            if (CurrentWave >= GameConfig.WavesPerLevel)
            {
                _ui?.SetMessage("Перемога! Бос переможений!");
                EndGame(true);
                return;
            }

            _ui?.SetMessage($"Хвилю {CurrentWave} відбито! +{waveGold} золота. Готуйтеся до хвилі {CurrentWave + 1}...");

            if (CurrentWave == WaveRewardConfig.MilestoneWave && !ActiveRewards.MilestoneClaimed)
                OfferMilestoneReward();
        }

        void OfferMilestoneReward()
        {
            _rewardOffers = WaveRewardConfig.PickRandomOffers();
            RewardChoicePending = true;
            DeselectTower();
            _ui?.ShowWaveRewardPanel(_rewardOffers);
            _ui?.SetMessage("4-у хвилю відбито! Оберіть нагороду перед 5-ю хвилею.");
        }

        public void ChooseWaveReward(WaveRewardType type)
        {
            if (!RewardChoicePending) return;

            ActiveRewards.MilestoneClaimed = true;
            RewardChoicePending = false;
            WaveRewardConfig.Apply(type, ActiveRewards, this);
            var def = WaveRewardConfig.Get(type);

            _ui?.HideWaveRewardPanel();
            _ui?.RefreshHud();
            _ui?.SetMessage($"Обрано: {def.Name}! {def.Description}");
        }

        public void SetMessage(string msg) => _ui?.SetMessage(msg);

        public void SpawnBossMinion(Vector3 nearBoss, Vector3[] path)
        {
            if (path == null || path.Length == 0 || !WaveActive) return;

            var offset = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
            var pos = nearBoss + offset;
            pos.y = path[0].y;

            var go = new GameObject("BossMinion");
            go.transform.SetParent(_enemyRoot);
            go.transform.position = pos;
            var enemy = go.AddComponent<Enemy>();
            enemy.Initialize(EnemyType.Scout, CurrentWave - 1, path);
        }

        public void OnEnemyKilled(Enemy e, int gold)
        {
            ActiveEnemies.Remove(e);
            Gold += GameConfig.KillGold(gold, SelectedRace, ActiveRewards);
        }

        public void OnObjectiveDestroyed() => EndGame(false);

        void EndGame(bool win)
        {
            GameOver = true;
            WaveActive = false;
            WaveOvertime = false;
            WaveTimeRemaining = 0f;
            _ui?.ShowEndScreen(win);
        }

        public void RestartGame()
        {
            GameStarted = false;
            GameOver = false;
            WaveActive = false;
            BuildModeActive = true;
            ActiveRewards.Reset();
            _ui?.HideWaveRewardPanel();
            RewardChoicePending = false;
            DestroyLevel();
            DeselectTower();
            _ui?.ShowMainMenu();
        }

        void BuildLevel()
        {
            if (_levelBuilt) return;

            _levelRoot = new GameObject("Level").transform;
            _enemyRoot = new GameObject("Enemies").transform;
            _enemyRoot.SetParent(_levelRoot);

            BuildTerrain();
            SpawnPaths = BuildSpawnPaths();
            BuildCrystal();
            BuildBuildZones();
            _levelBuilt = true;
        }

        void BuildTerrain()
        {
            var root = new GameObject("Ground").transform;
            root.SetParent(_levelRoot);

            for (int x = 0; x < LevelMap.Width; x++)
            {
                for (int z = 0; z < LevelMap.Height; z++)
                {
                    var cell = new Vector2Int(x, z);
                    var pos = LevelMap.CellToWorld(cell);

                    if (LevelMap.IsTreeCell(cell))
                    {
                        string treeModel = (x + z) % 3 == 0 ? "tile-tree-double" : "tile-tree";
                        var tree = ModelSpawner.Spawn(treeModel, pos, root, LevelMap.CellSize * 0.9f);
                        ModelSpawner.TintRenderers(tree, new Color(0.28f, 0.55f, 0.3f));
                        continue;
                    }

                    if (LevelMap.IsPathCell(cell))
                    {
                        var path = ModelSpawner.Spawn("tile-straight", pos, root, LevelMap.CellSize);
                        ModelSpawner.TintRenderers(path, new Color(0.62f, 0.48f, 0.32f));
                        continue;
                    }

                    if (cell == LevelMap.CrystalCell) continue;
                    if (LevelMap.IsBuildable(cell)) continue;

                    var grass = ModelSpawner.Spawn("tile", pos, root, LevelMap.CellSize);
                    ModelSpawner.TintRenderers(grass, new Color(0.3f, 0.62f, 0.34f));
                }
            }

            foreach (var spawn in LevelMap.SpawnCells)
            {
                var pos = LevelMap.CellToWorld(spawn);
                ModelSpawner.Spawn("spawn-round", pos + Vector3.up * 0.05f, root, 1.4f);
                ModelSpawner.Spawn("tile-spawn", pos, root, LevelMap.CellSize);
            }
        }

        Vector3[][] BuildSpawnPaths()
        {
            var paths = new Vector3[LevelMap.SpawnCells.Length][];
            for (int i = 0; i < LevelMap.SpawnCells.Length; i++)
                paths[i] = LevelMap.BuildWorldWaypoints(LevelMap.SpawnCells[i]);
            return paths;
        }

        void BuildCrystal()
        {
            var pos = LevelMap.CellToWorld(LevelMap.CrystalCell);
            var go = new GameObject("Crystal");
            go.transform.SetParent(_levelRoot);
            go.transform.position = pos;
            SetCrystalHp(Diff.CrystalHp);
            var model = ModelSpawner.Spawn("detail-crystal-large", pos, go.transform, 1.5f);
            model.transform.localPosition = Vector3.zero;
        }

        void BuildBuildZones()
        {
            var root = new GameObject("BuildZones").transform;
            root.SetParent(_levelRoot);

            for (int x = 0; x < LevelMap.Width; x++)
            {
                for (int z = 0; z < LevelMap.Height; z++)
                {
                    var cell = new Vector2Int(x, z);
                    if (!LevelMap.IsBuildable(cell)) continue;

                    var go = new GameObject($"Build_{x}_{z}");
                    go.transform.SetParent(root);
                    var zone = go.AddComponent<BuildZone>();
                    zone.Setup(cell, LevelMap.CellToWorld(cell));
                    BuildZones.Add(zone);
                }
            }
        }

        void ResetLevelState()
        {
            ClearTowersAndEnemies();
            ResetCrystalHp();
            SetCrystalHp(Diff.CrystalHp);
            ActiveRewards.Reset();
            RewardChoicePending = false;
            _ui?.HideWaveRewardPanel();
            foreach (var z in BuildZones)
                z.Clear();
        }

        void ClearTowersAndEnemies()
        {
            foreach (var t in Towers)
                if (t != null) Destroy(t.gameObject);
            Towers.Clear();

            if (_enemyRoot != null)
                Destroy(_enemyRoot.gameObject);
            _enemyRoot = new GameObject("Enemies").transform;
            _enemyRoot.SetParent(_levelRoot);

            ActiveEnemies.Clear();
            DeselectTower();
        }

        void DestroyLevel()
        {
            if (_levelRoot != null)
                Destroy(_levelRoot.gameObject);
            _levelRoot = null;
            _levelBuilt = false;
            BuildZones.Clear();
            Towers.Clear();
            CrystalHp = 0f;
            CrystalMaxHp = 0f;
            SpawnPaths = null;
            ActiveEnemies.Clear();
        }
    }
}
