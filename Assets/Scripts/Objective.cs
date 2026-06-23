using UnityEngine;

namespace NightWatch
{
    public class Objective : MonoBehaviour
    {
        public float MaxHp = 100f;
        public float CurrentHp { get; private set; }

        void Awake() => ResetHp();

        public void SetMaxHp(float max)
        {
            MaxHp = max;
            ResetHp();
        }

        public void AddMaxHp(float amount)
        {
            MaxHp += amount;
            CurrentHp += amount;
        }

        public void ResetHp() => CurrentHp = MaxHp;

        public void TakeDamage(float amount)
        {
            CurrentHp = Mathf.Max(0f, CurrentHp - amount);
            if (CurrentHp <= 0f)
                GameManager.Instance?.OnObjectiveDestroyed();
        }
    }
}
