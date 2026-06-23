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
        public const int LevelsCount = 1;
        public const int TowerTypesCount = 6;
        public const int EnemyTypesCount = 3;
        public const int StartingGold = 120;
        public const float SellRefundRate = 0.7f;
        public const float BossMinionInterval = 3.5f;
        public const int GoldPerWave = 50;
        public const float GoldIncomeMult = 0.75f;
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

        public static readonly string[] TowerDescriptions =
        {
            "Швидко, мало урону",
            "Повільно, багато урону",
            "AOE бомбежка",
            "Сповільнює ворогів",
            "Урон по 3 цілях",
            "Далеко, один постріл"
        };

        public static readonly string[] EnemyNames = { "Розвідник", "Воїн", "Танк" };

        static readonly string[][] RaceTowerModels =
        {
            new[] { "weapon-turret", "tower-round-build-a", "weapon-catapult", "tower-round-build-c", "tower-round-build-b", "weapon-ballista" },
            new[] { "weapon-turret", "weapon-cannon", "weapon-catapult", "tower-square-build-c", "tower-square-build-a", "weapon-ballista" },
            new[] { "weapon-turret", "weapon-cannon", "weapon-catapult", "wood-structure", "tower-square-build-b", "weapon-ballista" }
        };

        public static readonly string[] EnemyModels = { "enemy-ufo-a", "enemy-ufo-b", "enemy-ufo-c" };

        public static string GetTowerModel(RaceType race, TowerType type) =>
            RaceTowerModels[(int)race][(int)type];

        public static float GetRaceFireRateMult(RaceType race) =>
            race == RaceType.Elves ? 1.15f : 1f;

        public static float GetRaceDamageMult(RaceType race) =>
            race == RaceType.Dwarves ? 1.2f : 1f;

        public static float GetRaceGoldMult(RaceType race) =>
            race == RaceType.Orcs ? 1.25f : 1f;

        public static float GetRaceRangeMult(RaceType race) =>
            race == RaceType.Elves ? 1.08f : 1f;

        public static float GetPreviewRange(TowerType type, RaceType race, int level) =>
            GetTowerStats(type).Range * GetUpgradeRangeMult(level) * GetRaceRangeMult(race);

        public static int ScaleGoldIncome(int amount) =>
            Mathf.Max(0, Mathf.RoundToInt(amount * GoldIncomeMult));

        public static float GetEnemyScale(EnemyType type) => type switch
        {
            EnemyType.Scout => 0.75f,
            EnemyType.Fighter => 0.9f,
            _ => 1.15f
        };

        public static TowerStats GetTowerStats(TowerType type) => type switch
        {
            TowerType.Archer => new TowerStats(50, 7f, 12f, 1.8f, TowerAttackMode.Single),
            TowerType.Cannon => new TowerStats(90, 6f, 32f, 0.5f, TowerAttackMode.Single),
            TowerType.Mortar => new TowerStats(120, 8f, 22f, 0.45f, TowerAttackMode.Aoe, 3f),
            TowerType.Freeze => new TowerStats(80, 5.5f, 6f, 1f, TowerAttackMode.Slow, slowMult: 0.5f, slowDuration: 2.2f),
            TowerType.Lightning => new TowerStats(100, 6.5f, 14f, 1f, TowerAttackMode.Chain, chainTargets: 3),
            _ => new TowerStats(140, 11f, 55f, 0.35f, TowerAttackMode.Single)
        };

        public static string GetTowerTooltip(TowerType type, RaceType race, Difficulty difficulty)
        {
            var s = GetTowerStats(type);
            var diff = DifficultyConfig.Get(difficulty);
            float dmg = s.Damage * GetRaceDamageMult(race);
            float rate = s.FireRate * GetRaceFireRateMult(race);
            float range = s.Range * GetRaceRangeMult(race);
            float dps = dmg * rate;
            int waveGold = ScaleGoldIncome(Mathf.RoundToInt(GoldPerWave * diff.WaveGoldMult));
            int scoutGold = ScaleGoldIncome(Mathf.RoundToInt(12 * diff.KillGoldMult * GetRaceGoldMult(race)));

            var lines = new System.Text.StringBuilder();
            lines.AppendLine($"<b>{TowerNames[(int)type]}</b>");
            lines.AppendLine($"<size=90%>{TowerDescriptions[(int)type]}</size>");
            lines.AppendLine();
            lines.AppendLine($"Ціна: <b>{s.Cost}</b> золота");
            lines.AppendLine($"Дальність: <b>{range:0.#}</b>{RaceStatSuffix(race, "range")}");
            lines.AppendLine($"Урон: <b>{dmg:0.#}</b>{RaceStatSuffix(race, "damage")}");
            lines.AppendLine($"Атака: <b>{rate:0.##}/с</b>{RaceStatSuffix(race, "rate")}  (~{dps:0.#} урон/с)");
            lines.AppendLine($"За хвилю: <b>{waveGold}</b> золота");
            lines.AppendLine($"Розвідник: <b>{scoutGold}g</b>");
            if (diff.TowerRepairEnabled)
                lines.AppendLine($"<size=85%><color=#FF9999>Ремонт: {Mathf.RoundToInt(s.Cost * DifficultyConfig.TowerRepairCostRate)}g (25%)</color></size>");
            lines.AppendLine($"Продаж: <b>{Mathf.RoundToInt(s.Cost * SellRefundRate)}</b> (70%)");

            switch (s.Mode)
            {
                case TowerAttackMode.Aoe:
                    lines.AppendLine($"Радіус AOE: <b>{s.AoeRadius:0.#}</b>");
                    break;
                case TowerAttackMode.Slow:
                    lines.AppendLine($"Сповільнення: <b>{(1f - s.SlowMult) * 100f:0}%</b> на {s.SlowDuration:0.#}с");
                    break;
                case TowerAttackMode.Chain:
                    lines.AppendLine($"Ланцюг: <b>{s.ChainTargets}</b> цілей");
                    break;
            }

            lines.AppendLine();
            lines.AppendLine($"<size=85%><i>{GetTowerRoleHint(type)}</i></size>");
            return lines.ToString();
        }

        public static string GetTowerTooltip(TowerType type, RaceType race) =>
            GetTowerTooltip(type, race, Difficulty.Medium);

        static string RaceStatSuffix(RaceType race, string stat) => stat switch
        {
            "damage" when race == RaceType.Dwarves => " <size=85%>(+20%)</size>",
            "rate" when race == RaceType.Elves => " <size=85%>(+15%)</size>",
            "range" when race == RaceType.Elves => " <size=85%>(+8%)</size>",
            _ => ""
        };

        public static string GetRaceMenuText(RaceType race) =>
            $"<b>{RaceNames[(int)race]}</b>\n<size=88%>{RaceBonuses[(int)race]}</size>";

        static string GetTowerRoleHint(TowerType type) => type switch
        {
            TowerType.Archer => "Ідеально на початку хвилі",
            TowerType.Cannon => "Проти важких ворогів",
            TowerType.Mortar => "Коли вороги йдуть групою",
            TowerType.Freeze => "Тримає ворогів на шляху",
            TowerType.Lightning => "Кілька цілей поруч",
            _ => "Далекі цілі та боси"
        };

        public static Color GetTowerColor(TowerType type) => type switch
        {
            TowerType.Archer => new Color(0.35f, 0.75f, 0.45f),
            TowerType.Cannon => new Color(0.75f, 0.45f, 0.3f),
            TowerType.Mortar => new Color(0.85f, 0.55f, 0.2f),
            TowerType.Freeze => new Color(0.45f, 0.75f, 0.95f),
            TowerType.Lightning => new Color(0.65f, 0.5f, 0.95f),
            _ => new Color(0.55f, 0.55f, 0.65f)
        };

        public static Color GetProjectileColor(TowerType type) => type switch
        {
            TowerType.Archer => new Color(0.55f, 0.95f, 0.45f),
            TowerType.Cannon => new Color(0.95f, 0.55f, 0.2f),
            TowerType.Mortar => new Color(1f, 0.65f, 0.15f),
            TowerType.Freeze => new Color(0.55f, 0.9f, 1f),
            TowerType.Lightning => new Color(0.85f, 0.75f, 1f),
            _ => new Color(0.9f, 0.95f, 1f)
        };

        public static int GetUpgradeCost(int baseCost, int level) => baseCost + level * 25;

        public static float GetUpgradeMult(int level) =>
            level switch { 1 => 1f, 2 => 1.35f, _ => 1.75f };

        public static float GetUpgradeRangeMult(int level) =>
            level switch { 1 => 1f, 2 => 1.2625f, _ => 1.5625f };

        static float WaveHpScale(int waveIndex) => 1f + waveIndex * 0.14f;

        /// <summary>Scout — швидкий, мало HP. Fighter — звичайний. Tank — повільний, багато HP.</summary>
        public static EnemyStats GetEnemyStats(EnemyType type, int waveIndex, Difficulty difficulty)
        {
            float s = WaveHpScale(waveIndex) * DifficultyConfig.Get(difficulty).EnemyHpMult;
            float goldMult = DifficultyConfig.Get(difficulty).KillGoldMult;
            return type switch
            {
                EnemyType.Scout => new EnemyStats(28f * s, 3.4f, 5, Mathf.RoundToInt(12 * goldMult)),
                EnemyType.Fighter => new EnemyStats(75f * s, 2.0f, 12, Mathf.RoundToInt(18 * goldMult)),
                _ => new EnemyStats(190f * s, 1.05f, 22, Mathf.RoundToInt(26 * goldMult))
            };
        }

        public static EnemyStats GetBossStats(int waveIndex, Difficulty difficulty)
        {
            float s = WaveHpScale(waveIndex) * DifficultyConfig.Get(difficulty).EnemyHpMult;
            int gold = Mathf.RoundToInt(85 * DifficultyConfig.Get(difficulty).KillGoldMult);
            return new EnemyStats(900f * s, 0.75f, 35, gold);
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

        public static int[] GetWaveComposition(int waveIndex) =>
            GetWaveComposition(waveIndex, Difficulty.Medium);

        public static bool IsBossWave(int waveIndex) => waveIndex == 9;

        public static float GetWaveTimeLimit(int waveIndex, Difficulty difficulty)
        {
            float baseLimit = IsBossWave(waveIndex)
                ? BossWaveTimeLimit
                : BaseWaveTimeLimit + waveIndex * WaveTimeLimitPerWave;
            return baseLimit * DifficultyConfig.Get(difficulty).WaveTimerMult + WaveTimeLimitBonus;
        }

        public static float GetWaveTimeLimit(int waveIndex) =>
            GetWaveTimeLimit(waveIndex, Difficulty.Medium);
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
}
