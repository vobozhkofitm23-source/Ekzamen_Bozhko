using UnityEngine;

namespace NightWatch
{
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

        public static DifficultyParams Get(Difficulty d) => d switch
        {
            Difficulty.Easy => new DifficultyParams(
                enemyHpMult: 0.85f,
                enemyCountMult: 1.35f,
                killGoldMult: 0.9f,
                waveGoldMult: 1f,
                crystalHp: 120,
                waveTimerMult: 1.1f,
                miniWavePauseMult: 1f,
                towerRepair: false),
            Difficulty.Medium => new DifficultyParams(
                enemyHpMult: 1.2f,
                enemyCountMult: 1.65f,
                killGoldMult: 0.65f,
                waveGoldMult: 0.85f,
                crystalHp: 90,
                waveTimerMult: 0.8f,
                miniWavePauseMult: 1f,
                towerRepair: false),
            _ => new DifficultyParams(
                enemyHpMult: 1.55f,
                enemyCountMult: 2f,
                killGoldMult: 0.5f,
                waveGoldMult: 0.7f,
                crystalHp: 60,
                waveTimerMult: 0.72f,
                miniWavePauseMult: 0.7f,
                towerRepair: true)
        };

        public static string GetDescription(Difficulty d)
        {
            var p = Get(d);
            var lines = new System.Text.StringBuilder();
            lines.AppendLine($"<b>{Names[(int)d]}</b>");
            lines.AppendLine($"<size=85%>Кристал: {p.CrystalHp} HP · вороги x{p.EnemyHpMult:0.##} HP</size>");
            lines.AppendLine($"<size=85%>Мобів більше · золота менше · час коротший</size>");
            if (p.TowerRepairEnabled)
                lines.AppendLine("<size=85%><color=#FF8888>Башні ламаються — потрібен ремонт!</color></size>");
            return lines.ToString();
        }
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

        public DifficultyParams(float enemyHpMult, float enemyCountMult, float killGoldMult, float waveGoldMult,
            int crystalHp, float waveTimerMult, float miniWavePauseMult, bool towerRepair)
        {
            EnemyHpMult = enemyHpMult;
            EnemyCountMult = enemyCountMult;
            KillGoldMult = killGoldMult;
            WaveGoldMult = waveGoldMult;
            CrystalHp = crystalHp;
            WaveTimerMult = waveTimerMult;
            MiniWavePauseMult = miniWavePauseMult;
            TowerRepairEnabled = towerRepair;
        }
    }
}
