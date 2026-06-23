using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NightWatch
{
    public class Tower : MonoBehaviour
    {
        public RaceType Race { get; private set; }
        public TowerType Type { get; private set; }
        public int Level { get; private set; } = 1;
        public bool IsSelected { get; private set; }
        public BuildZone Zone { get; private set; }

        TowerStats _stats;
        float _attackCooldown;
        int _totalSpent;
        float _durability = 1f;
        GameObject _selectionRing;
        LineRenderer _rangeLine;
        Transform _starsRoot;
        Transform _durabilityRoot;
        Transform _durabilityFill;
        readonly GameObject[] _stars = new GameObject[GameConfig.MaxUpgradeLevel];

        public bool IsBroken => RepairEnabled && _durability <= 0f;
        public float Durability => _durability;

        bool RepairEnabled =>
            GameManager.Instance != null &&
            DifficultyConfig.Get(GameManager.Instance.SelectedDifficulty).TowerRepairEnabled;

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
            RefreshUpgradeStars();
            GameManager.Instance?.RegisterTower(this);
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
            ApplyBarColor(bg.GetComponent<Renderer>(), new Color(0.15f, 0.15f, 0.18f));

            var fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fill.name = "DurabilityFill";
            fill.transform.SetParent(_durabilityRoot);
            fill.transform.localPosition = new Vector3(-0.55f, 0f, 0f);
            fill.transform.localScale = new Vector3(1.1f, 0.1f, 0.14f);
            Destroy(fill.GetComponent<Collider>());
            ApplyBarColor(fill.GetComponent<Renderer>(), new Color(0.35f, 0.85f, 0.4f));
            _durabilityFill = fill.transform;

            UpdateDurabilityBar();
        }

        static void ApplyBarColor(Renderer r, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            r.material = new Material(shader);
            if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", color);
            else r.material.color = color;
        }

        void UpdateDurabilityBar()
        {
            if (_durabilityRoot == null) return;

            bool show = RepairEnabled;
            _durabilityRoot.gameObject.SetActive(show);
            if (!show) return;

            float pct = Mathf.Clamp01(_durability);
            if (_durabilityFill != null)
            {
                _durabilityFill.localScale = new Vector3(1.1f * pct, 0.1f, 0.14f);
                _durabilityFill.localPosition = new Vector3(-0.55f * (1f - pct), 0f, 0f);
                var r = _durabilityFill.GetComponent<Renderer>();
                if (r != null)
                {
                    Color c = pct <= 0f ? new Color(0.85f, 0.2f, 0.2f)
                        : pct < 0.35f ? new Color(0.95f, 0.55f, 0.2f)
                        : new Color(0.35f, 0.85f, 0.4f);
                    if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", c);
                    else r.material.color = c;
                }
            }

            if (IsBroken)
            {
                foreach (var rend in GetComponentsInChildren<Renderer>())
                {
                    if (rend == null || rend.gameObject.name.Contains("Durability") ||
                        rend.gameObject.name.Contains("SelectRing") || rend.gameObject.name.Contains("Star"))
                        continue;
                }
            }
        }

        void UpdateRangeLine() =>
            RangeRingHelper.Draw(_rangeLine, transform.position, GetAttackRange());

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            if (_selectionRing != null)
                _selectionRing.SetActive(selected);
            if (_rangeLine != null)
            {
                if (selected) UpdateRangeLine();
                _rangeLine.gameObject.SetActive(selected);
            }
        }

        public int GetUpgradeCost()
        {
            if (Level >= GameConfig.MaxUpgradeLevel) return -1;
            int cost = GameConfig.GetUpgradeCost(_stats.Cost, Level);
            var rewards = GameManager.Instance?.ActiveRewards;
            if (rewards != null && rewards.UpgradeCostMult < 1f)
                cost = Mathf.Max(1, Mathf.RoundToInt(cost * rewards.UpgradeCostMult));
            return cost;
        }

        public void Upgrade()
        {
            if (Level >= GameConfig.MaxUpgradeLevel) return;
            int cost = GetUpgradeCost();
            _totalSpent += cost;
            Level++;
            transform.localScale *= 1.06f;
            RefreshUpgradeStars();
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

        public float GetAttackRange()
        {
            float range = _stats.Range * GameConfig.GetUpgradeRangeMult(Level) * GameConfig.GetRaceRangeMult(Race);
            var rewards = GameManager.Instance?.ActiveRewards;
            if (rewards != null)
                range *= rewards.GlobalRangeMult;
            return range;
        }

        public float GetDamage()
        {
            float dmg = _stats.Damage * GameConfig.GetUpgradeMult(Level) * GameConfig.GetRaceDamageMult(Race);
            var rewards = GameManager.Instance?.ActiveRewards;
            if (rewards != null && rewards.ArtilleryDamageMult > 1f &&
                (Type == TowerType.Cannon || Type == TowerType.Mortar))
                dmg *= rewards.ArtilleryDamageMult;
            return dmg;
        }

        public float GetFireRate()
        {
            float rate = _stats.FireRate * GameConfig.GetRaceFireRateMult(Race);
            var rewards = GameManager.Instance?.ActiveRewards;
            if (rewards != null && rewards.ArcherFireRateMult > 1f && Type == TowerType.Archer)
                rate *= rewards.ArcherFireRateMult;
            return rate;
        }

        public float GetSlowDuration()
        {
            float dur = _stats.SlowDuration;
            var rewards = GameManager.Instance?.ActiveRewards;
            if (rewards != null)
                dur += rewards.FreezeSlowBonus;
            return dur;
        }

        public string GetStatsText()
        {
            float dmg = GetDamage();
            float range = GetAttackRange();
            float rate = GetFireRate();
            float dps = dmg * rate;

            var sb = new StringBuilder();
            sb.AppendLine($"<b>{GameConfig.TowerNames[(int)Type]}</b>");
            sb.AppendLine($"Рівень: <b>{Level}</b> / {GameConfig.MaxUpgradeLevel}  {GetStarsLabel()}");
            sb.AppendLine();

            if (RepairEnabled)
            {
                int pct = Mathf.RoundToInt(_durability * 100f);
                if (IsBroken)
                    sb.AppendLine("<color=#FF5555><b>ЗЛАМАНА — не стріляє!</b></color>");
                else if (pct < 35)
                    sb.AppendLine($"<color=#FFAA55>Міцність: <b>{pct}%</b></color>");
                else
                    sb.AppendLine($"Міцність: <b>{pct}%</b>");
            }

            sb.AppendLine($"Урон: <b>{dmg:0.#}</b>");
            sb.AppendLine($"Дальність: <b>{range:0.#}</b>");
            sb.AppendLine($"Атака: <b>{rate:0.##}/с</b>  (~{dps:0.#} DPS)");

            switch (_stats.Mode)
            {
                case TowerAttackMode.Aoe:
                    sb.AppendLine($"AOE радіус: <b>{_stats.AoeRadius:0.#}</b>");
                    break;
                case TowerAttackMode.Slow:
                    sb.AppendLine($"Сповільнення: <b>{(1f - _stats.SlowMult) * 100f:0}%</b> на {GetSlowDuration():0.#}с");
                    break;
                case TowerAttackMode.Chain:
                    sb.AppendLine($"Ланцюг: <b>{_stats.ChainTargets}</b> цілей");
                    break;
            }

            int upgradeCost = GetUpgradeCost();
            if (upgradeCost >= 0)
                sb.AppendLine($"Апгрейд: <b>{upgradeCost}</b> золота");
            else
                sb.AppendLine("<color=#FFD700><b>MAX рівень</b></color>");

            if (RepairEnabled && NeedsRepair())
                sb.AppendLine($"Ремонт: <b>{GetRepairCost()}</b> золота (25%)");

            sb.AppendLine($"Продаж: <b>+{GetSellRefund()}</b> золота");
            return sb.ToString();
        }

        string GetStarsLabel()
        {
            if (Level <= 1) return "";
            return new string('*', Level - 1);
        }

        void RefreshUpgradeStars()
        {
            if (_starsRoot == null)
            {
                _starsRoot = new GameObject("UpgradeStars").transform;
                _starsRoot.SetParent(transform);
            }

            _starsRoot.localPosition = Vector3.up * 1.55f;

            int starCount = Mathf.Max(0, Level - 1);
            for (int i = 0; i < GameConfig.MaxUpgradeLevel; i++)
            {
                if (_stars[i] == null)
                    _stars[i] = CreateStarVisual(i, starCount);

                if (i < starCount)
                {
                    float spacing = 0.32f;
                    float start = -(starCount - 1) * spacing * 0.5f;
                    _stars[i].transform.localPosition = new Vector3(start + i * spacing, 0, 0);
                }

                _stars[i].SetActive(i < starCount);
            }
        }

        GameObject CreateStarVisual(int index, int total)
        {
            var star = GameObject.CreatePrimitive(PrimitiveType.Cube);
            star.name = $"Star_{index + 1}";
            star.transform.SetParent(_starsRoot);
            star.transform.localScale = Vector3.one * 0.2f;
            star.transform.localRotation = Quaternion.Euler(0, 45f, 0);

            var r = star.GetComponent<Renderer>();
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            r.material = new Material(shader);
            var gold = new Color(1f, 0.88f, 0.25f);
            if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", gold);
            else r.material.color = gold;

            Destroy(star.GetComponent<Collider>());
            return star;
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

            bool fired = _stats.Mode switch
            {
                TowerAttackMode.Aoe => TryAoeAttack(),
                TowerAttackMode.Slow => TrySlowAttack(),
                TowerAttackMode.Chain => TryChainAttack(),
                _ => TrySingleAttack()
            };

            if (fired) _attackCooldown = 1f / GetFireRate();
        }

        bool TrySingleAttack()
        {
            var enemy = FindBestTarget();
            if (enemy == null) return false;
            Projectile.Fire(GetMuzzlePosition(), enemy, GetDamage(), Type, TowerAttackMode.Single);
            return true;
        }

        bool TryAoeAttack()
        {
            var center = FindBestTarget();
            if (center == null) return false;
            var targetPos = center.transform.position + Vector3.up * 0.4f;
            Projectile.FireArc(GetMuzzlePosition(), targetPos, GetDamage(), Type, _stats.AoeRadius);
            return true;
        }

        bool TrySlowAttack()
        {
            var enemy = FindBestTarget();
            if (enemy == null) return false;
            Projectile.Fire(GetMuzzlePosition(), enemy, GetDamage(), Type, TowerAttackMode.Slow,
                slowMult: _stats.SlowMult, slowDuration: GetSlowDuration());
            return true;
        }

        bool TryChainAttack()
        {
            var targets = FindChainTargets();
            if (targets.Count == 0) return false;

            var muzzle = GetMuzzlePosition();
            foreach (var enemy in targets)
                Projectile.Fire(muzzle, enemy, GetDamage(), Type, TowerAttackMode.Single);
            return true;
        }

        List<Enemy> FindChainTargets()
        {
            var result = new List<Enemy>();
            var first = FindBestTarget();
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

        Enemy FindBestTarget()
        {
            Enemy best = null;
            float bestDist = float.MaxValue;
            foreach (var e in GameManager.Instance.ActiveEnemies)
            {
                if (e == null || !e.IsAlive) continue;
                float d = Vector3.Distance(transform.position, e.transform.position);
                if (d <= GetAttackRange() && d < bestDist)
                {
                    bestDist = d;
                    best = e;
                }
            }
            return best;
        }
    }
}
