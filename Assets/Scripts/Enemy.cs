using UnityEngine;

namespace NightWatch
{
    public class Enemy : MonoBehaviour
    {
        public bool IsAlive => _hp > 0f;
        public bool IsBoss { get; private set; }
        public EnemyType Type { get; private set; }

        float _hp;
        float _maxHp;
        float _speed;
        int _objectiveDamage;
        int _goldReward;
        int _wp;
        Vector3[] _path;
        float _slowMult = 1f;
        float _slowTimer;
        float _minionTimer;
        float _frostTrailTimer;
        GameObject _frostHead;
        GameObject _frostRing;
        Renderer[] _renderers;
        Color[] _baseColors;

        public void Initialize(EnemyType type, int waveIndex, Vector3[] path)
        {
            IsBoss = false;
            Type = type;
            _path = path;
            var diff = GameManager.Instance != null ? GameManager.Instance.SelectedDifficulty : Difficulty.Medium;
            var stats = GameConfig.GetEnemyStats(type, waveIndex, diff);
            ApplyStats(stats);
            ModelSpawner.CreateEnemyModel(type, false, transform);
            transform.localScale = Vector3.one * (IsBoss ? 1.3f : 1f);
            CacheRenderers();
            GameManager.Instance?.RegisterEnemy(this);
        }

        public void InitializeAsBoss(int waveIndex, Vector3[] path)
        {
            IsBoss = true;
            Type = EnemyType.Tank;
            _path = path;
            var diff = GameManager.Instance != null ? GameManager.Instance.SelectedDifficulty : Difficulty.Medium;
            var stats = GameConfig.GetBossStats(waveIndex, diff);
            ApplyStats(stats);
            _minionTimer = GameConfig.BossMinionInterval;
            ModelSpawner.CreateEnemyModel(EnemyType.Tank, true, transform);
            transform.localScale = Vector3.one * 1.3f;
            CacheRenderers();
            GameManager.Instance?.RegisterEnemy(this);
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

        void ApplyStats(EnemyStats stats)
        {
            _hp = stats.Hp;
            _maxHp = stats.Hp;
            _speed = stats.Speed;
            _objectiveDamage = stats.DamageToObjective;
            _goldReward = stats.GoldReward;
            _wp = 0;
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
                UpdateFrostVisuals(true);
                _frostTrailTimer -= Time.deltaTime;
                if (_frostTrailTimer <= 0f)
                {
                    _frostTrailTimer = 0.22f;
                    SpawnFrostTrailMark();
                }

                if (_slowTimer <= 0f)
                {
                    _slowMult = 1f;
                    UpdateFrostVisuals(false);
                }
            }
            else
            {
                UpdateFrostVisuals(false);
            }

            if (_wp >= _path.Length)
            {
                ReachCrystal();
                return;
            }

            var target = _path[_wp];
            float moveSpeed = _speed * _slowMult;
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, target) < 0.25f)
                _wp++;

            var dir = target - transform.position;
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(dir);
        }

        public void ApplySlow(float mult, float duration)
        {
            _slowMult = Mathf.Min(_slowMult, mult);
            _slowTimer = Mathf.Max(_slowTimer, duration);
            EnsureFrostObjects();
            UpdateFrostVisuals(true);
        }

        void EnsureFrostObjects()
        {
            if (_frostHead != null) return;

            _frostHead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _frostHead.name = "FrostHead";
            _frostHead.transform.SetParent(transform);
            _frostHead.transform.localPosition = Vector3.up * (IsBoss ? 2.1f : 1.35f);
            _frostHead.transform.localScale = Vector3.one * (IsBoss ? 0.55f : 0.38f);
            Destroy(_frostHead.GetComponent<Collider>());
            ApplyFrostMaterial(_frostHead.GetComponent<Renderer>(), new Color(0.55f, 0.88f, 1f, 0.85f));

            _frostRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _frostRing.name = "FrostRing";
            _frostRing.transform.SetParent(transform);
            _frostRing.transform.localPosition = Vector3.up * 0.05f;
            _frostRing.transform.localScale = new Vector3(IsBoss ? 1.6f : 1.1f, 0.015f, IsBoss ? 1.6f : 1.1f);
            Destroy(_frostRing.GetComponent<Collider>());
            ApplyFrostMaterial(_frostRing.GetComponent<Renderer>(), new Color(0.45f, 0.78f, 1f, 0.7f));

            _frostHead.SetActive(false);
            _frostRing.SetActive(false);
        }

        static void ApplyFrostMaterial(Renderer r, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            r.material = new Material(shader);
            if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", color);
            else r.material.color = color;
        }

        void UpdateFrostVisuals(bool active)
        {
            if (_frostHead != null) _frostHead.SetActive(active);
            if (_frostRing != null) _frostRing.SetActive(active);

            if (_renderers == null) return;

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                var mat = _renderers[i].material;
                var tint = active
                    ? Color.Lerp(_baseColors[i], new Color(0.55f, 0.82f, 1f), 0.55f)
                    : _baseColors[i];
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
                else mat.color = tint;
            }

            if (active && _frostHead != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.08f;
                _frostHead.transform.localScale = Vector3.one * (IsBoss ? 0.55f : 0.38f) * pulse;
            }
        }

        void SpawnFrostTrailMark()
        {
            var mark = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mark.name = "FrostTrail";
            mark.transform.position = transform.position + Vector3.up * 0.03f;
            mark.transform.localScale = new Vector3(IsBoss ? 0.9f : 0.55f, 0.02f, IsBoss ? 0.9f : 0.55f);
            Destroy(mark.GetComponent<Collider>());
            ApplyFrostMaterial(mark.GetComponent<Renderer>(), new Color(0.5f, 0.85f, 1f, 0.55f));
            Destroy(mark, 2.8f);
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
            GameManager.Instance?.Crystal?.TakeDamage(_objectiveDamage);
            GameManager.Instance?.ActiveEnemies.Remove(this);
            Destroy(gameObject);
        }
    }
}
