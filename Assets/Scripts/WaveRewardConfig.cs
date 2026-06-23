using System;
using System.Collections.Generic;
using UnityEngine;

namespace NightWatch
{
    public enum WaveRewardType
    {
        GoldRush,
        RichHunt,
        VictorTax,
        SwiftArchers,
        HeavyArtillery,
        FrostStorm,
        ExtendedRange,
        MasterUpgrade,
        StrongCrystal
    }

    [Serializable]
    public struct WaveRewardDef
    {
        public WaveRewardType Type;
        public string Name;
        public string Description;
        public Color Color;

        public WaveRewardDef(WaveRewardType type, string name, string description, Color color)
        {
            Type = type;
            Name = name;
            Description = description;
            Color = color;
        }
    }

    public class WaveRewardBonuses
    {
        public bool MilestoneClaimed;
        public WaveRewardType? Chosen;

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
            Chosen = null;
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
            new(WaveRewardType.GoldRush, "Золотий приплив", "+200 золота одразу",
                new Color(1f, 0.85f, 0.25f)),
            new(WaveRewardType.RichHunt, "Багате полювання", "+30% золота за вбивства",
                new Color(0.45f, 0.9f, 0.45f)),
            new(WaveRewardType.VictorTax, "Податок переможців", "+40 золота після кожної хвилі",
                new Color(0.55f, 0.75f, 1f)),
            new(WaveRewardType.SwiftArchers, "Швидкі лучники", "Лучники: +15% швидкість атаки",
                new Color(0.4f, 0.85f, 0.5f)),
            new(WaveRewardType.HeavyArtillery, "Важка артилерія", "Гармата і мортира: +20% урон",
                new Color(0.9f, 0.5f, 0.3f)),
            new(WaveRewardType.FrostStorm, "Крижана буря", "Крижана: slow на +0.8 с довше",
                new Color(0.5f, 0.85f, 1f)),
            new(WaveRewardType.ExtendedRange, "Розширений дальнобій", "Усі башні: +10% дальність",
                new Color(0.6f, 0.65f, 0.95f)),
            new(WaveRewardType.MasterUpgrade, "Майстер апгрейду", "Апгрейд на 25% дешевше",
                new Color(0.75f, 0.55f, 0.95f)),
            new(WaveRewardType.StrongCrystal, "Міцний кристал", "Кристал: +30 HP",
                new Color(0.45f, 0.95f, 1f)),
        };

        public static WaveRewardDef Get(WaveRewardType type) => All[(int)type];

        public static WaveRewardType[] PickRandomOffers(int count = OfferCount)
        {
            var pool = new List<WaveRewardType>();
            for (int i = 0; i < All.Length; i++)
                pool.Add((WaveRewardType)i);

            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            var result = new WaveRewardType[Mathf.Min(count, pool.Count)];
            for (int i = 0; i < result.Length; i++)
                result[i] = pool[i];
            return result;
        }
    }
}
