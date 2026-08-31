using UnityEngine;

namespace NightWatch
{
    public enum TowerType { Archer, Cannon }
    public enum EnemyType { Scout, Fighter, Tank }
    public enum Race { Elf, Dwarf }

    public static class GameConfig
    {
        public const int WaveCount = 5;
        public const int StartGold = 120;
        public const int CrystalMaxHp = 100;
        public const int GoldPerWave = 38;
        public const float SpawnInterval = 1.15f;

        public static readonly string[] TowerNames = { "Лучник", "Гармата" };
        public static readonly string[] RaceNames = { "Ельфи", "Гноми" };

        // scout, fighter, tank на кожну хвилю
        public static readonly (int scout, int fighter, int tank)[] Waves =
        {
            (3, 1, 0), (2, 2, 0), (2, 2, 1), (1, 3, 1), (1, 2, 2)
        };

        public static readonly Vector3[] EnemyPath =
        {
            Cell(1,7), Cell(2,7), Cell(3,7), Cell(4,7), Cell(5,7), Cell(5,6),
            Cell(6,6), Cell(7,6), Cell(8,6), Cell(9,6), Cell(10,6), Cell(11,6), Cell(12,6),
            Cell(12,7), Cell(12,8), Cell(12,9),
            Cell(13,9), Cell(14,9), Cell(15,9), Cell(16,9), Cell(16,8), Cell(16,7)
        };

        static Vector3 Cell(int x, int z) => new((x - 8.5f) * 2f, 0.5f, (z - 6.5f) * 2f);

        public static int TowerCost(TowerType t) => t == TowerType.Archer ? 50 : 90;
        public static float TowerRange(TowerType t) => t == TowerType.Archer ? 12f : 32f;
        public static float TowerDamage(TowerType t) => t == TowerType.Archer ? 7f : 6f;
        public static float TowerFireRate(TowerType t) => t == TowerType.Archer ? 1.8f : 0.5f;
        public static float WaveSeconds(int wave) => 45f + wave * 5f;

        public static float EnemyHp(EnemyType type, int wave)
        {
            float k = 1f + wave * 0.2f;
            return type switch { EnemyType.Scout => 28f * k, EnemyType.Fighter => 75f * k, _ => 190f * k };
        }

        public static float EnemySpeed(EnemyType type) =>
            type switch { EnemyType.Scout => 3.4f, EnemyType.Fighter => 2f, _ => 1.05f };

        public static int EnemyCrystalDamage(EnemyType type) =>
            type switch { EnemyType.Scout => 5, EnemyType.Fighter => 12, _ => 22 };

        public static int EnemyGold(EnemyType type) =>
            type switch { EnemyType.Scout => 9, EnemyType.Fighter => 14, _ => 20 };

        public static void ApplyRace(Race race, ref float damage, ref float fireRate)
        {
            if (race == Race.Elf) fireRate *= 1.15f;
            else damage *= 1.2f;
        }
    }
}
