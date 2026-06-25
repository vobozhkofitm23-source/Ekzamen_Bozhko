// Ворог: йде по шляху до кристалу, отримує урон, може бути сповільнений.
using UnityEngine;

namespace NightWatch
{
    public class Enemy : MonoBehaviour
    {
        public bool IsAlive => _hp > 0f;
        public bool IsBoss { get; private set; }
        public EnemyType Type { get; private set; }

        float _hp;
        float _speed;
        int _objectiveDamage;
        int _goldReward;
        int _wp;
        Vector3[] _path;
        float _slowMult = 1f;
        float _slowTimer;
        float _minionTimer;
        Renderer[] _renderers;
        Color[] _baseColors;

        public void Initialize(EnemyType type, int waveIndex, Vector3[] path, bool isBoss = false)
        {
            IsBoss = isBoss;
            Type = isBoss ? EnemyType.Tank : type;
            _path = path;

            var diff = GameManager.Instance != null
                ? GameManager.Instance.SelectedDifficulty
                : Difficulty.Medium;
            var stats = isBoss
                ? GameConfig.GetBossStats(waveIndex, diff)
                : GameConfig.GetEnemyStats(type, waveIndex, diff);

            _hp = stats.Hp;
            _speed = stats.Speed;
            _objectiveDamage = stats.DamageToObjective;
            _goldReward = stats.GoldReward;
            _wp = 0;

            if (isBoss) _minionTimer = GameConfig.BossMinionInterval;

            ModelSpawner.CreateEnemyModel(Type, isBoss, transform);
            transform.localScale = Vector3.one * (isBoss ? 1.3f : 1f);
            CacheRenderers();
            GameManager.Instance?.ActiveEnemies.Add(this);
        }

        void CacheRenderers()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _baseColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                var mat = _renderers[i].material;
                _baseColors[i] = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
            }
        }

        void Update()
        {
            if (!IsAlive || _path == null || _path.Length < 2) return;

            if (IsBoss)
            {
                _minionTimer -= Time.deltaTime;
                if (_minionTimer <= 0f)
                {
                    _minionTimer = GameConfig.BossMinionInterval;
                    GameManager.Instance?.SpawnBossMinion(transform.position, _path);
                }
            }

            if (_slowTimer > 0f)
            {
                _slowTimer -= Time.deltaTime;
                SetSlowTint(true);
                if (_slowTimer <= 0f) { _slowMult = 1f; SetSlowTint(false); }
            }
            else SetSlowTint(false);

            if (_wp >= _path.Length) { ReachCrystal(); return; }

            var target = _path[_wp];
            transform.position = Vector3.MoveTowards(transform.position, target, _speed * _slowMult * Time.deltaTime);
            if (Vector3.Distance(transform.position, target) < 0.25f) _wp++;

            var dir = target - transform.position;
            if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir);
        }

        public void ApplySlow(float mult, float duration)
        {
            _slowMult = Mathf.Min(_slowMult, mult);
            _slowTimer = Mathf.Max(_slowTimer, duration);
            SetSlowTint(true);
        }

        void SetSlowTint(bool active)
        {
            if (_renderers == null) return;
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                var mat = _renderers[i].material;
                var tint = active ? Color.Lerp(_baseColors[i], new Color(0.55f, 0.82f, 1f), 0.55f) : _baseColors[i];
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
                else mat.color = tint;
            }
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive) return;
            _hp -= amount;
            if (_hp <= 0f) Die();
        }

        void Die()
        {
            GameManager.Instance?.OnEnemyKilled(this, _goldReward);
            Destroy(gameObject);
        }

        void ReachCrystal()
        {
            GameManager.Instance?.DamageCrystal(_objectiveDamage);
            GameManager.Instance?.ActiveEnemies.Remove(this);
            Destroy(gameObject);
        }
    }
}
