// Башня: стріляє по ворогах, апгрейд, продаж, ремонт (режим АД).
using System.Collections.Generic;
using UnityEngine;
namespace NightWatch
{
    public class Tower : MonoBehaviour
    {
        public RaceType Race { get; private set; }
        public TowerType Type { get; private set; }
        public int Level { get; private set; } = 1;
        public BuildZone Zone { get; private set; }

        TowerStats _stats;
        float _attackCooldown;
        int _totalSpent;
        float _durability = 1f;
        GameObject _selectionRing;
        LineRenderer _rangeLine;
        Transform _durabilityRoot;
        Transform _durabilityFill;

        public bool IsBroken => RepairEnabled && _durability <= 0f;

        bool RepairEnabled =>
            GameManager.Instance != null && GameManager.Instance.Diff.TowerRepairEnabled;

        WaveRewardBonuses Rewards => GameManager.Instance?.ActiveRewards;
        TowerCombat Combat => GameConfig.GetTowerCombat(_stats, Type, Level, Race, Rewards);

        public bool Init(RaceType race, TowerType type, BuildZone zone)
        {
            if (zone == null || zone.Occupied) return false;

            Race = race;
            Type = type;
            Zone = zone;
            _stats = GameConfig.GetTowerStats(type);
            _totalSpent = _stats.Cost;
            _durability = 1f;

            if (!zone.TryBuild(this)) return false;

            ModelSpawner.CreateTowerModel(type, race, transform);

            var existing = GetComponent<BoxCollider>();
            if (existing == null)
                existing = gameObject.AddComponent<BoxCollider>();
            existing.size = new Vector3(2f, 2.4f, 2f);
            existing.center = Vector3.up * 0.6f;

            CreateSelectionRing();
            CreateDurabilityBar();
            if (GameManager.Instance != null)
                GameManager.Instance.Towers.Add(this);
            return true;
        }

        void CreateSelectionRing()
        {
            _selectionRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _selectionRing.name = "SelectRing";
            _selectionRing.transform.SetParent(transform);
            _selectionRing.transform.localPosition = Vector3.up * 0.05f;
            _selectionRing.transform.localScale = new Vector3(1.8f, 0.02f, 1.8f);
            var r = _selectionRing.GetComponent<Renderer>();
            r.material.color = new Color(1f, 0.85f, 0.2f, 0.85f);
            Destroy(_selectionRing.GetComponent<Collider>());
            _selectionRing.SetActive(false);

            _rangeLine = RangeRingHelper.Create(transform, new Color(0.3f, 0.85f, 1f, 1f), 0.28f);
        }

        void CreateDurabilityBar()
        {
            _durabilityRoot = new GameObject("DurabilityBar").transform;
            _durabilityRoot.SetParent(transform);
            _durabilityRoot.localPosition = new Vector3(0f, 1.35f, 0f);

            var bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bg.name = "DurabilityBg";
            bg.transform.SetParent(_durabilityRoot);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localScale = new Vector3(1.1f, 0.08f, 0.12f);
            Destroy(bg.GetComponent<Collider>());
            ModelSpawner.SetUnlitColor(bg.GetComponent<Renderer>(), new Color(0.15f, 0.15f, 0.18f));

            var fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fill.name = "DurabilityFill";
            fill.transform.SetParent(_durabilityRoot);
            fill.transform.localPosition = new Vector3(-0.55f, 0f, 0f);
            fill.transform.localScale = new Vector3(1.1f, 0.1f, 0.14f);
            Destroy(fill.GetComponent<Collider>());
            ModelSpawner.SetUnlitColor(fill.GetComponent<Renderer>(), new Color(0.35f, 0.85f, 0.4f));
            _durabilityFill = fill.transform;

            UpdateDurabilityBar();
        }

        void UpdateDurabilityBar()
        {
            if (_durabilityRoot == null) return;
            _durabilityRoot.gameObject.SetActive(RepairEnabled);
            if (!RepairEnabled || _durabilityFill == null) return;

            float pct = Mathf.Clamp01(_durability);
            _durabilityFill.localScale = new Vector3(1.1f * pct, 0.1f, 0.14f);
            _durabilityFill.localPosition = new Vector3(-0.55f * (1f - pct), 0f, 0f);
            var r = _durabilityFill.GetComponent<Renderer>();
            if (r == null) return;
            Color c = pct <= 0f ? new Color(0.85f, 0.2f, 0.2f)
                : pct < 0.35f ? new Color(0.95f, 0.55f, 0.2f)
                : new Color(0.35f, 0.85f, 0.4f);
            ModelSpawner.SetUnlitColor(r, c);
        }

        void UpdateRangeLine() =>
            RangeRingHelper.Draw(_rangeLine, transform.position, Combat.Range);

        public void SetSelected(bool selected)
        {
            if (_selectionRing != null)
                _selectionRing.SetActive(selected);
            if (_rangeLine != null)
            {
                if (selected) UpdateRangeLine();
                _rangeLine.gameObject.SetActive(selected);
            }
        }

        public int GetUpgradeCost() => GameConfig.TowerUpgradeCost(_stats.Cost, Level, Rewards);

        public void Upgrade()
        {
            if (Level >= GameConfig.MaxUpgradeLevel) return;
            int cost = GetUpgradeCost();
            _totalSpent += cost;
            Level++;
            transform.localScale *= 1.06f;
            if (_rangeLine != null && _rangeLine.gameObject.activeSelf)
                UpdateRangeLine();
        }

        public int GetSellRefund() => Mathf.RoundToInt(_totalSpent * GameConfig.SellRefundRate);

        public int GetRepairCost() =>
            Mathf.Max(1, Mathf.RoundToInt(_totalSpent * DifficultyConfig.TowerRepairCostRate));

        public bool NeedsRepair() => RepairEnabled && _durability < 0.999f;

        public void Repair()
        {
            _durability = 1f;
            UpdateDurabilityBar();
        }

        public string GetStatsText()
        {
            var c = Combat;
            string text = $"<b>{GameConfig.TowerNames[(int)Type]}</b>\nРівень: <b>{Level}</b> / {GameConfig.MaxUpgradeLevel}\n\n";
            if (RepairEnabled)
            {
                int pct = Mathf.RoundToInt(_durability * 100f);
                text += IsBroken ? "<color=#FF5555><b>ЗЛАМАНА!</b></color>\n" : $"Міцність: <b>{pct}%</b>\n";
            }
            text += $"Урон: <b>{c.Damage:0.#}</b>  Дальність: <b>{c.Range:0.#}</b>\n";
            text += $"Атака: <b>{c.FireRate:0.##}/с</b>\n";
            if (_stats.Mode == TowerAttackMode.Aoe) text += $"AOE: <b>{_stats.AoeRadius:0.#}</b>\n";
            if (_stats.Mode == TowerAttackMode.Slow) text += $"Slow: <b>{c.SlowDuration:0.#}с</b>\n";
            if (_stats.Mode == TowerAttackMode.Chain) text += $"Ланцюг: <b>{_stats.ChainTargets}</b>\n";
            int upgradeCost = GetUpgradeCost();
            text += upgradeCost >= 0 ? $"Апгрейд: <b>{upgradeCost}</b>g\n" : "<color=#FFD700><b>MAX</b></color>\n";
            if (RepairEnabled && NeedsRepair()) text += $"Ремонт: <b>{GetRepairCost()}</b>g\n";
            text += $"Продаж: <b>+{GetSellRefund()}</b>g";
            return text;
        }

        Vector3 GetMuzzlePosition()
        {
            float height = Type switch
            {
                TowerType.Mortar => 1.15f,
                TowerType.Lightning => 1.25f,
                TowerType.Sniper => 1.2f,
                _ => 1.05f
            };
            return transform.position + Vector3.up * height;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.GameStarted || gm.GameOver) return;

            if (RepairEnabled)
            {
                _durability -= Time.deltaTime / DifficultyConfig.TowerDurabilitySeconds;
                _durability = Mathf.Max(0f, _durability);
                UpdateDurabilityBar();
            }

            if (IsBroken) return;

            _attackCooldown -= Time.deltaTime;
            if (_attackCooldown > 0f) return;

            if (TryFire())
                _attackCooldown = 1f / Combat.FireRate;
        }

        bool TryFire()
        {
            var c = Combat;
            var muzzle = GetMuzzlePosition();

            switch (_stats.Mode)
            {
                case TowerAttackMode.Aoe:
                {
                    var center = FindBestTarget(c.Range);
                    if (center == null) return false;
                    Projectile.FireArc(muzzle, center.transform.position + Vector3.up * 0.4f,
                        c.Damage, Type, _stats.AoeRadius);
                    return true;
                }
                case TowerAttackMode.Slow:
                {
                    var enemy = FindBestTarget(c.Range);
                    if (enemy == null) return false;
                    Projectile.Fire(muzzle, enemy, c.Damage, Type, TowerAttackMode.Slow,
                        slowMult: _stats.SlowMult, slowDuration: c.SlowDuration);
                    return true;
                }
                case TowerAttackMode.Chain:
                {
                    var targets = FindChainTargets(c.Range);
                    if (targets.Count == 0) return false;
                    foreach (var enemy in targets)
                        Projectile.Fire(muzzle, enemy, c.Damage, Type, TowerAttackMode.Single);
                    return true;
                }
                default:
                {
                    var enemy = FindBestTarget(c.Range);
                    if (enemy == null) return false;
                    Projectile.Fire(muzzle, enemy, c.Damage, Type, TowerAttackMode.Single);
                    return true;
                }
            }
        }

        List<Enemy> FindChainTargets(float range)
        {
            var result = new List<Enemy>();
            var first = FindBestTarget(range);
            if (first == null) return result;

            var hit = new HashSet<Enemy>();
            var queue = new Queue<Enemy>();
            queue.Enqueue(first);

            while (queue.Count > 0 && hit.Count < _stats.ChainTargets)
            {
                var e = queue.Dequeue();
                if (e == null || !e.IsAlive || hit.Contains(e)) continue;
                hit.Add(e);
                result.Add(e);

                Enemy next = null;
                float best = float.MaxValue;
                foreach (var other in GameManager.Instance.ActiveEnemies)
                {
                    if (other == null || !other.IsAlive || hit.Contains(other)) continue;
                    float d = Vector3.Distance(e.transform.position, other.transform.position);
                    if (d <= 4f && d < best)
                    {
                        best = d;
                        next = other;
                    }
                }
                if (next != null) queue.Enqueue(next);
            }

            return result;
        }

        Enemy FindBestTarget(float range)
        {
            Enemy best = null;
            float bestDist = float.MaxValue;
            foreach (var e in GameManager.Instance.ActiveEnemies)
            {
                if (e == null || !e.IsAlive) continue;
                float d = Vector3.Distance(transform.position, e.transform.position);
                if (d <= range && d < bestDist)
                {
                    bestDist = d;
                    best = e;
                }
            }
            return best;
        }
    }
}
