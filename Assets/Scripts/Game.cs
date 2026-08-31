using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace NightWatch
{
    public class Game : MonoBehaviour
    {
        public static Game I { get; private set; }

        public Race PlayerRace;
        public TowerType SelectedTower = TowerType.Archer;
        public int CurrentWave;
        public int Gold = GameConfig.StartGold;
        public float CrystalHp = GameConfig.CrystalMaxHp;
        public float WaveTimeLeft;
        public bool IsPlaying, IsGameOver, IsWaveActive;

        public readonly List<BuildZone> BuildZones = new();
        public readonly List<Enemy> Enemies = new();

        UIManager _ui;
        Transform _enemyFolder;
        Camera _camera;
        bool _waveSpawnDone;

        void Awake() => I = this;

        void Start()
        {
            _ui = GetComponent<UIManager>();
            _camera = Camera.main;
            var level = GameObject.Find("Level");
            if (level)
            {
                BuildZones.AddRange(level.GetComponentsInChildren<BuildZone>());
                _enemyFolder = level.transform.Find("Enemies");
                if (!_enemyFolder)
                {
                    var folder = new GameObject("Enemies");
                    folder.transform.SetParent(level.transform, false);
                    _enemyFolder = folder.transform;
                }
            }
            if (!FindFirstObjectByType<EventSystem>()) CreateEventSystem();
            _ui.ShowMenu();
        }

        void Update()
        {
            if (IsPlaying) _ui.RefreshHud();
            if (!IsWaveActive) { HandleClick(); return; }

            WaveTimeLeft -= Time.deltaTime;
            if (WaveTimeLeft <= 0f) { EndGame(false, "Час вийшов!"); return; }

            Enemies.RemoveAll(e => e == null);
            if (_waveSpawnDone && Enemies.Count == 0) FinishWave();
            HandleClick();
        }

        public void StartWithRace(Race race)
        {
            PlayerRace = race;
            Gold = GameConfig.StartGold;
            CrystalHp = GameConfig.CrystalMaxHp;
            CurrentWave = 0;
            IsGameOver = IsWaveActive = false;
            IsPlaying = true;
            WaveTimeLeft = 0f;
            _waveSpawnDone = false;
            ResetZones();
            _ui.ShowHud();
            _ui.HighlightTower(SelectedTower);
            _ui.ShowMessage(race == Race.Elf ? "Ельфи: +15% швидкість" : "Гноми: +20% урон");
        }

        public void SelectTower(TowerType type)
        {
            SelectedTower = type;
            _ui.HighlightTower(type);
            _ui.ShowMessage(GameConfig.TowerNames[(int)type]);
        }

        public void StartNextWave()
        {
            if (IsWaveActive || IsGameOver || CurrentWave >= GameConfig.WaveCount) return;
            CurrentWave++;
            IsWaveActive = true;
            WaveTimeLeft = GameConfig.WaveSeconds(CurrentWave - 1);
            _waveSpawnDone = false;
            _ui.ShowMessage($"Хвиля {CurrentWave}");
            StartCoroutine(SpawnWave(CurrentWave - 1));
        }

        public void DamageCrystal(int damage)
        {
            CrystalHp -= damage;
            if (CrystalHp <= 0f) EndGame(false, "Кристал знищено!");
        }

        public void OnEnemyKilled(Enemy enemy, int gold)
        {
            Enemies.Remove(enemy);
            Gold += gold;
        }

        public void Restart()
        {
            IsPlaying = IsGameOver = IsWaveActive = false;
            WaveTimeLeft = 0f;
            _waveSpawnDone = false;
            CrystalHp = GameConfig.CrystalMaxHp;
            ResetZones();
            _ui.ShowMenu();
        }

        void FinishWave()
        {
            IsWaveActive = false;
            Gold += GameConfig.GoldPerWave;
            if (CurrentWave >= GameConfig.WaveCount) EndGame(true);
            else _ui.ShowMessage($"+{GameConfig.GoldPerWave} золота");
        }

        void EndGame(bool win, string reason = null)
        {
            IsGameOver = true;
            IsWaveActive = false;
            _ui.ShowEnd(win, reason);
        }

        void HandleClick()
        {
            if (!IsPlaying || IsGameOver || !Clicked() || OverUi()) return;
            var zone = ZoneUnderMouse();
            if (zone == null || zone.HasTower) return;

            int cost = GameConfig.TowerCost(SelectedTower);
            if (Gold < cost) { _ui.ShowMessage("Мало золота!"); return; }
            Gold -= cost;
            Tower.Create(SelectedTower, zone);
            _ui.ShowMessage("Побудовано!");
        }

        BuildZone ZoneUnderMouse()
        {
            if (!_camera) return null;
            var ray = _camera.ScreenPointToRay(MousePos());
            foreach (var hit in Physics.RaycastAll(ray, 200f))
            {
                var z = hit.collider.GetComponent<BuildZone>() ?? hit.collider.GetComponentInParent<BuildZone>();
                if (z && !z.HasTower) return z;
            }

            if (!new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float d)) return null;
            var p = ray.GetPoint(d);
            BuildZone best = null;
            float dist = 2f;
            foreach (var z in BuildZones)
            {
                if (!z || z.HasTower) continue;
                float dx = p.x - z.transform.position.x, dz = p.z - z.transform.position.z;
                float d2 = dx * dx + dz * dz;
                if (d2 < dist * dist) { dist = Mathf.Sqrt(d2); best = z; }
            }
            return best;
        }

        IEnumerator SpawnWave(int wave)
        {
            var w = GameConfig.Waves[wave];
            foreach (var pair in new[] { (EnemyType.Scout, w.scout), (EnemyType.Fighter, w.fighter), (EnemyType.Tank, w.tank) })
                for (int i = 0; i < pair.Item2; i++)
                {
                    Enemy.Create(pair.Item1, wave, _enemyFolder);
                    yield return new WaitForSeconds(GameConfig.SpawnInterval);
                }
            _waveSpawnDone = true;
        }

        void ResetZones()
        {
            Enemies.Clear();
            if (_enemyFolder)
                foreach (Transform c in _enemyFolder) Destroy(c.gameObject);
            foreach (var z in BuildZones)
            {
                foreach (Transform c in z.transform)
                    if (c.GetComponent<Tower>()) Destroy(c.gameObject);
                z.Free();
            }
        }

        static bool Clicked()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        static Vector2 MousePos()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current?.position.ReadValue() ?? Vector2.zero;
#else
            return Input.mousePosition;
#endif
        }

        static bool OverUi()
        {
            if (!EventSystem.current) return false;
            var data = new PointerEventData(EventSystem.current) { position = MousePos() };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);
            foreach (var h in hits)
                if (h.gameObject.GetComponentInParent<Canvas>()) return true;
            return false;
        }

        static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }
    }

    public class Tower : MonoBehaviour
    {
        public TowerType Type;
        float _cd;

        public static void Create(TowerType type, BuildZone zone)
        {
            var go = new GameObject("Tower");
            var t = go.AddComponent<Tower>();
            t.Type = type;
            zone.PutTowerHere(t);

            var col = type == TowerType.Archer ? Color.green : new Color(0.75f, 0.45f, 0.3f);
            Shape.Add(go.transform, PrimitiveType.Cylinder, Vector3.zero, new Vector3(1.3f, 0.15f, 1.3f), col);
            Shape.Add(go.transform, PrimitiveType.Cylinder, Vector3.up * 0.35f, Vector3.one * 0.5f, col);
        }

        void Update()
        {
            if (Game.I == null || !Game.I.IsPlaying) return;
            _cd -= Time.deltaTime;
            if (_cd > 0f) return;

            float dmg = GameConfig.TowerDamage(Type), rate = GameConfig.TowerFireRate(Type);
            GameConfig.ApplyRace(Game.I.PlayerRace, ref dmg, ref rate);

            Enemy target = null;
            float best = GameConfig.TowerRange(Type);
            foreach (var e in Game.I.Enemies)
            {
                if (!e || e.Health <= 0f) continue;
                float d = Vector3.Distance(transform.position, e.transform.position);
                if (d < best) { best = d; target = e; }
            }
            if (!target) return;

            target.Hit(dmg);
            _cd = 1f / rate;
        }
    }

    public class Enemy : MonoBehaviour
    {
        public float Health;
        float _speed;
        int _gold, _dmg, _i;
        Vector3[] _path;

        public static void Create(EnemyType type, int wave, Transform parent)
        {
            var go = new GameObject("Enemy");
            go.transform.SetParent(parent);
            go.transform.position = GameConfig.EnemyPath[0];

            var e = go.AddComponent<Enemy>();
            e._path = GameConfig.EnemyPath;
            e.Health = GameConfig.EnemyHp(type, wave);
            e._speed = GameConfig.EnemySpeed(type);
            e._dmg = GameConfig.EnemyCrystalDamage(type);
            e._gold = GameConfig.EnemyGold(type);

            Color c = type == EnemyType.Scout ? Color.red : type == EnemyType.Fighter ? Color.yellow : Color.blue;
            Shape.Add(go.transform, type == EnemyType.Scout ? PrimitiveType.Sphere : PrimitiveType.Cube,
                Vector3.up * 0.6f, Vector3.one, c);
            Game.I.Enemies.Add(e);
        }

        void Update()
        {
            if (Health <= 0f || _path == null) return;
            if (_i >= _path.Length)
            {
                Game.I.Enemies.Remove(this);
                Game.I.DamageCrystal(_dmg);
                Destroy(gameObject);
                return;
            }

            var next = _path[_i];
            transform.position = Vector3.MoveTowards(transform.position, next, _speed * Time.deltaTime);
            if (Vector3.Distance(transform.position, next) < 0.25f) _i++;
        }

        public void Hit(float amount)
        {
            if (Health <= 0f) return;
            Health -= amount;
            if (Health <= 0f) { Game.I.OnEnemyKilled(this, _gold); Destroy(gameObject); }
        }
    }

    static class Shape
    {
        public static void Add(Transform parent, PrimitiveType kind, Vector3 pos, Vector3 scale, Color color)
        {
            var p = GameObject.CreatePrimitive(kind);
            p.transform.SetParent(parent, false);
            p.transform.localPosition = pos;
            p.transform.localScale = scale;
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            p.GetComponent<Renderer>().sharedMaterial = new Material(shader) { color = color };
            Object.Destroy(p.GetComponent<Collider>());
        }
    }
}
