// Усі числа гри: башні, вороги, хвилі, складність, раси.
using UnityEngine;

namespace NightWatch
{
    public enum RaceType { Elves, Dwarves, Orcs }
    public enum TowerType { Archer, Cannon, Mortar, Freeze, Lightning, Sniper }
    public enum EnemyType { Scout, Fighter, Tank }

    public static class GameConfig
    {
        public const int MaxUpgradeLevel = 3;
        public const int WavesPerLevel = 10;
        public const int TowerTypesCount = 6;
        public const int EnemyTypesCount = 3;
        public const int StartingGold = 120;
        public const float SellRefundRate = 0.7f;
        public const float BossMinionInterval = 3.5f;
        public const int GoldPerWave = 38;
        public const int ScoutKillGold = 9;
        public const int FighterKillGold = 14;
        public const int TankKillGold = 20;
        public const int BossKillGold = 63;
        public const int RewardGoldRush = 150;
        public const int RewardGoldPerWave = 30;
        public const float EnemySpawnInterval = 1.15f;
        public const float MiniWavePause = 2.8f;
        public const int MiniWaveSize = 2;
        public const float OvertimeCrystalDamagePerSecond = 2f;
        public const float BaseWaveTimeLimit = 32f;
        public const float WaveTimeLimitPerWave = 3.5f;
        public const float BossWaveTimeLimit = 65f;
        public const float WaveTimeLimitBonus = 10f;

        public static readonly string[] RaceNames =
        {
            "Кристалічні ельфи",
            "Вогняні гноми",
            "Лісові орки"
        };

        public static readonly string[] RaceBonuses =
        {
            "+15% швидкість атаки",
            "+20% урон башен",
            "+25% золото за вбивства"
        };

        public static readonly Color[] RaceColors =
        {
            new Color(0.35f, 0.65f, 1f),
            new Color(1f, 0.45f, 0.3f),
            new Color(0.35f, 0.85f, 0.45f)
        };

        public static readonly string[] TowerNames =
        {
            "Лучник", "Гармата", "Мортира", "Крижана", "Блискавка", "Снайпер"
        };

        public static readonly string[] EnemyNames = { "Розвідник", "Воїн", "Танк" };

        // Множники: раса, рівень башні, нагороди після 4-ї хвилі
        public static readonly float[] RaceRate   = { 1.15f, 1f, 1f };
        public static readonly float[] RaceDamage = { 1f, 1.2f, 1f };
        public static readonly float[] RaceGold   = { 1f, 1f, 1.25f };
        public static readonly float[] RaceRange  = { 1.08f, 1f, 1f };
        static readonly float[] UpDamage = { 1f, 1.35f, 1.75f };
        static readonly float[] UpRange  = { 1f, 1.2625f, 1.5625f };
        static int Lv(int level) => Mathf.Clamp(level - 1, 0, 2);

        // Підсумкові характеристики башні з урахуванням раси, рівня та бонусів
        public static TowerCombat GetTowerCombat(TowerStats s, TowerType type, int level, RaceType race, WaveRewardBonuses r)
        {
            int li = Lv(level);
            float range = s.Range * UpRange[li] * RaceRange[(int)race];
            if (r != null) range *= r.GlobalRangeMult;

            float dmg = s.Damage * UpDamage[li] * RaceDamage[(int)race];
            if (r != null && r.ArtilleryDamageMult > 1f && (type == TowerType.Cannon || type == TowerType.Mortar))
                dmg *= r.ArtilleryDamageMult;

            float rate = s.FireRate * RaceRate[(int)race];
            if (r != null && r.ArcherFireRateMult > 1f && type == TowerType.Archer)
                rate *= r.ArcherFireRateMult;

            float slow = s.SlowDuration;
            if (r != null) slow += r.FreezeSlowBonus;

            return new TowerCombat { Damage = dmg, Range = range, FireRate = rate, SlowDuration = slow };
        }

        public static int TowerUpgradeCost(int baseCost, int level, WaveRewardBonuses r)
        {
            if (level >= MaxUpgradeLevel) return -1;
            int cost = GetUpgradeCost(baseCost, level);
            if (r != null && r.UpgradeCostMult < 1f)
                cost = Mathf.Max(1, Mathf.RoundToInt(cost * r.UpgradeCostMult));
            return cost;
        }

        public static int KillGold(int baseGold, RaceType race, WaveRewardBonuses r)
        {
            float mult = RaceGold[(int)race];
            if (r != null) mult *= r.KillGoldMult;
            return Mathf.RoundToInt(baseGold * mult);
        }

        public static TowerStats GetTowerStats(TowerType type) => type switch
        {
            TowerType.Archer => new TowerStats(50, 7f, 12f, 1.8f, TowerAttackMode.Single),
            TowerType.Cannon => new TowerStats(90, 6f, 32f, 0.5f, TowerAttackMode.Single),
            TowerType.Mortar => new TowerStats(120, 8f, 22f, 0.45f, TowerAttackMode.Aoe, 3f),
            TowerType.Freeze => new TowerStats(80, 5.5f, 6f, 1f, TowerAttackMode.Slow, slowMult: 0.5f, slowDuration: 2.2f),
            TowerType.Lightning => new TowerStats(100, 6.5f, 14f, 1f, TowerAttackMode.Chain, chainTargets: 3),
            _ => new TowerStats(140, 11f, 55f, 0.35f, TowerAttackMode.Single)
        };

        static readonly Color[] TowerColors =
        {
            new(0.35f, 0.75f, 0.45f), new(0.75f, 0.45f, 0.3f), new(0.85f, 0.55f, 0.2f),
            new(0.45f, 0.75f, 0.95f), new(0.65f, 0.5f, 0.95f), new(0.55f, 0.55f, 0.65f)
        };

        static readonly Color[] ProjectileColors =
        {
            new(0.55f, 0.95f, 0.45f), new(0.95f, 0.55f, 0.2f), new(1f, 0.65f, 0.15f),
            new(0.55f, 0.9f, 1f), new(0.85f, 0.75f, 1f), new(0.9f, 0.95f, 1f)
        };

        public static Color GetTowerColor(TowerType type) => TowerColors[(int)type];
        public static Color GetProjectileColor(TowerType type) => ProjectileColors[(int)type];

        public static int GetUpgradeCost(int baseCost, int level) => baseCost + level * 25;

        static float WaveHpScale(int waveIndex) => 1f + waveIndex * 0.14f;

        /// <summary>Scout — швидкий, мало HP. Fighter — звичайний. Tank — повільний, багато HP.</summary>
        public static EnemyStats GetEnemyStats(EnemyType type, int waveIndex, Difficulty difficulty)
        {
            var d = DifficultyConfig.Get(difficulty);
            float s = WaveHpScale(waveIndex) * d.EnemyHpMult;
            return type switch
            {
                EnemyType.Scout => new EnemyStats(28f * s, 3.4f, 5, Mathf.RoundToInt(ScoutKillGold * d.KillGoldMult)),
                EnemyType.Fighter => new EnemyStats(75f * s, 2.0f, 12, Mathf.RoundToInt(FighterKillGold * d.KillGoldMult)),
                _ => new EnemyStats(190f * s, 1.05f, 22, Mathf.RoundToInt(TankKillGold * d.KillGoldMult))
            };
        }

        public static EnemyStats GetBossStats(int waveIndex, Difficulty difficulty)
        {
            var d = DifficultyConfig.Get(difficulty);
            float s = WaveHpScale(waveIndex) * d.EnemyHpMult;
            return new EnemyStats(900f * s, 0.75f, 35, Mathf.RoundToInt(BossKillGold * d.KillGoldMult));
        }

        /// <summary>Хвилі 1–9: [Scout, Fighter, Tank]. Хвиля 10 — окремо (бос).</summary>
        public static int[] GetWaveComposition(int waveIndex, Difficulty difficulty)
        {
            int w = waveIndex + 1;
            if (waveIndex >= 9) return new[] { 0, 0, 0 };

            int[] baseCounts = waveIndex switch
            {
                <= 2 => new[] { 3 + w, 1, 0 },
                <= 5 => new[] { 2 + w / 2, 2 + w / 2, 1 },
                <= 7 => new[] { 1, 3 + w / 2, 2 },
                _ => new[] { 1, 2 + w / 2, 3 + w / 3 }
            };

            float mult = DifficultyConfig.Get(difficulty).EnemyCountMult;
            for (int i = 0; i < baseCounts.Length; i++)
                baseCounts[i] = Mathf.Max(0, Mathf.CeilToInt(baseCounts[i] * mult));
            return baseCounts;
        }

        public static bool IsBossWave(int waveIndex) => waveIndex == 9;

        public static float GetWaveTimeLimit(int waveIndex, Difficulty difficulty)
        {
            float baseLimit = IsBossWave(waveIndex)
                ? BossWaveTimeLimit
                : BaseWaveTimeLimit + waveIndex * WaveTimeLimitPerWave;
            return baseLimit * DifficultyConfig.Get(difficulty).WaveTimerMult + WaveTimeLimitBonus;
        }
    }

    public struct TowerCombat
    {
        public float Damage;
        public float Range;
        public float FireRate;
        public float SlowDuration;
    }

    public enum TowerAttackMode { Single, Aoe, Slow, Chain }

    public struct TowerStats
    {
        public int Cost;
        public float Range;
        public float Damage;
        public float FireRate;
        public TowerAttackMode Mode;
        public float AoeRadius;
        public float SlowMult;
        public float SlowDuration;
        public int ChainTargets;

        public TowerStats(int cost, float range, float dmg, float rate, TowerAttackMode mode,
            float aoeRadius = 0f, float slowMult = 1f, float slowDuration = 0f, int chainTargets = 1)
        {
            Cost = cost;
            Range = range;
            Damage = dmg;
            FireRate = rate;
            Mode = mode;
            AoeRadius = aoeRadius;
            SlowMult = slowMult;
            SlowDuration = slowDuration;
            ChainTargets = chainTargets;
        }
    }

    public struct EnemyStats
    {
        public float Hp;
        public float Speed;
        public int DamageToObjective;
        public int GoldReward;

        public EnemyStats(float hp, float speed, int dmg, int gold)
        {
            Hp = hp;
            Speed = speed;
            DamageToObjective = dmg;
            GoldReward = gold;
        }
    }

    public enum Difficulty { Easy, Medium, Hell }

    public static class DifficultyConfig
    {
        public static readonly string[] Names = { "Легко", "Середнє", "АД" };
        public static readonly Color[] Colors =
        {
            new Color(0.3f, 0.72f, 0.45f),
            new Color(0.95f, 0.72f, 0.25f),
            new Color(0.85f, 0.22f, 0.22f)
        };

        public const float TowerDurabilitySeconds = 120f;
        public const float TowerRepairCostRate = 0.25f;

        static readonly DifficultyParams[] Table =
        {
            new(0.85f, 1.35f, 0.9f,  1f,   120, 1.1f,  1f,  false),
            new(1.2f,  1.65f, 0.65f, 0.85f, 90, 0.8f,  1f,  false),
            new(1.55f, 2f,    0.5f,  0.7f,  60, 0.72f, 0.7f, true),
        };

        public static DifficultyParams Get(Difficulty d) => Table[(int)d];
    }

    public readonly struct DifficultyParams
    {
        public readonly float EnemyHpMult;
        public readonly float EnemyCountMult;
        public readonly float KillGoldMult;
        public readonly float WaveGoldMult;
        public readonly int CrystalHp;
        public readonly float WaveTimerMult;
        public readonly float MiniWavePauseMult;
        public readonly bool TowerRepairEnabled;

        public DifficultyParams(float enemyHp, float enemyCount, float killGold, float waveGold,
            int crystalHp, float waveTimer, float miniWavePause, bool towerRepair)
        {
            EnemyHpMult = enemyHp;
            EnemyCountMult = enemyCount;
            KillGoldMult = killGold;
            WaveGoldMult = waveGold;
            CrystalHp = crystalHp;
            WaveTimerMult = waveTimer;
            MiniWavePauseMult = miniWavePause;
            TowerRepairEnabled = towerRepair;
        }
    }

    // --- Нагорода після 4-ї хвилі (механіка №21 RANDOM.ORG) ---
    public enum WaveRewardType
    {
        GoldRush, RichHunt, VictorTax, SwiftArchers, HeavyArtillery,
        FrostStorm, ExtendedRange, MasterUpgrade, StrongCrystal
    }

    public struct WaveRewardDef
    {
        public string Name;
        public string Description;
        public Color Color;
    }

    public class WaveRewardBonuses
    {
        public bool MilestoneClaimed;
        public float KillGoldMult = 1f;
        public int BonusGoldPerWave;
        public float ArcherFireRateMult = 1f;
        public float ArtilleryDamageMult = 1f;
        public float FreezeSlowBonus;
        public float GlobalRangeMult = 1f;
        public float UpgradeCostMult = 1f;

        public void Reset()
        {
            MilestoneClaimed = false;
            KillGoldMult = 1f;
            BonusGoldPerWave = 0;
            ArcherFireRateMult = 1f;
            ArtilleryDamageMult = 1f;
            FreezeSlowBonus = 0f;
            GlobalRangeMult = 1f;
            UpgradeCostMult = 1f;
        }
    }

    public static class WaveRewardConfig
    {
        public const int MilestoneWave = 4;
        public const int OfferCount = 3;

        static readonly WaveRewardDef[] All =
        {
            new() { Name = "Золотий приплив", Description = $"+{GameConfig.RewardGoldRush}g одразу", Color = new Color(1f, 0.85f, 0.25f) },
            new() { Name = "Багате полювання", Description = "+30% золота за вбивства", Color = new Color(0.45f, 0.9f, 0.45f) },
            new() { Name = "Податок переможців", Description = $"+{GameConfig.RewardGoldPerWave}g за хвилю", Color = new Color(0.55f, 0.75f, 1f) },
            new() { Name = "Швидкі лучники", Description = "Лучники: +15% швидкість", Color = new Color(0.4f, 0.85f, 0.5f) },
            new() { Name = "Важка артилерія", Description = "Гармата/мортира: +20% урон", Color = new Color(0.9f, 0.5f, 0.3f) },
            new() { Name = "Крижана буря", Description = "Slow на +0.8с", Color = new Color(0.5f, 0.85f, 1f) },
            new() { Name = "Розширений дальнобій", Description = "Усі башні: +10% range", Color = new Color(0.6f, 0.65f, 0.95f) },
            new() { Name = "Майстер апгрейду", Description = "Апгрейд −25%", Color = new Color(0.75f, 0.55f, 0.95f) },
            new() { Name = "Міцний кристал", Description = "Кристал +30 HP", Color = new Color(0.45f, 0.95f, 1f) },
        };

        public static WaveRewardDef Get(WaveRewardType type) => All[(int)type];

        public static WaveRewardType[] PickRandomOffers(int count = OfferCount)
        {
            var pool = (WaveRewardType[])System.Enum.GetValues(typeof(WaveRewardType));
            for (int i = pool.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }
            var result = new WaveRewardType[count];
            for (int i = 0; i < count; i++) result[i] = pool[i];
            return result;
        }

        public static void Apply(WaveRewardType type, WaveRewardBonuses b, GameManager gm)
        {
            switch (type)
            {
                case WaveRewardType.GoldRush: gm.Gold += GameConfig.RewardGoldRush; break;
                case WaveRewardType.RichHunt: b.KillGoldMult = 1.3f; break;
                case WaveRewardType.VictorTax: b.BonusGoldPerWave = GameConfig.RewardGoldPerWave; break;
                case WaveRewardType.SwiftArchers: b.ArcherFireRateMult = 1.15f; break;
                case WaveRewardType.HeavyArtillery: b.ArtilleryDamageMult = 1.2f; break;
                case WaveRewardType.FrostStorm: b.FreezeSlowBonus = 0.8f; break;
                case WaveRewardType.ExtendedRange: b.GlobalRangeMult = 1.1f; break;
                case WaveRewardType.MasterUpgrade: b.UpgradeCostMult = 0.75f; break;
                case WaveRewardType.StrongCrystal: gm.AddCrystalHp(30f); break;
            }
        }
    }
}
