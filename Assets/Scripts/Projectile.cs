using UnityEngine;

namespace NightWatch
{
    public class Projectile : MonoBehaviour
    {
        Enemy _target;
        Vector3 _targetPos;
        float _damage;
        float _speed;
        TowerAttackMode _mode;
        float _aoeRadius;
        float _slowMult;
        float _slowDuration;
        bool _useTargetPos;

        public static void Fire(Vector3 origin, Enemy target, float damage, TowerType type,
            TowerAttackMode mode, float aoeRadius = 0f, float slowMult = 1f, float slowDuration = 0f)
        {
            if (target == null || !target.IsAlive) return;
            var proj = Create(origin, type, damage, mode, aoeRadius);
            proj._target = target;
            proj._slowMult = slowMult;
            proj._slowDuration = slowDuration;
        }

        public static void FireArc(Vector3 origin, Vector3 targetPos, float damage, TowerType type, float aoeRadius)
        {
            var proj = Create(origin, type, damage, TowerAttackMode.Aoe, aoeRadius);
            proj._targetPos = targetPos;
            proj._useTargetPos = true;
            proj._speed = 9f;
        }

        static Projectile Create(Vector3 origin, TowerType type, float damage, TowerAttackMode mode, float aoeRadius)
        {
            var go = GameObject.CreatePrimitive(type == TowerType.Archer ? PrimitiveType.Cylinder : PrimitiveType.Sphere);
            go.name = "Projectile";
            go.transform.position = origin;
            go.transform.localScale = Vector3.one * (type == TowerType.Archer ? 0.12f : 0.22f);
            Object.Destroy(go.GetComponent<Collider>());
            ModelSpawner.SetUnlitColor(go.GetComponent<Renderer>(), GameConfig.GetProjectileColor(type));

            var proj = go.AddComponent<Projectile>();
            proj._damage = damage;
            proj._mode = mode;
            proj._aoeRadius = aoeRadius;
            proj._speed = type == TowerType.Archer ? 22f : type == TowerType.Cannon ? 14f : 18f;
            return proj;
        }

        void Update()
        {
            Vector3 goal = _useTargetPos
                ? _targetPos
                : (_target != null ? _target.transform.position + Vector3.up * 0.5f : transform.position);

            if (!_useTargetPos && (_target == null || !_target.IsAlive))
            {
                Destroy(gameObject);
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, goal, _speed * Time.deltaTime);
            if (Vector3.Distance(transform.position, goal) < 0.35f) Impact();
        }

        void Impact()
        {
            if (_mode == TowerAttackMode.Aoe)
            {
                var center = _useTargetPos ? _targetPos : _target.transform.position;
                foreach (var e in GameManager.Instance.ActiveEnemies)
                {
                    if (e == null || !e.IsAlive) continue;
                    if (Vector3.Distance(center, e.transform.position) <= _aoeRadius)
                        e.TakeDamage(_damage);
                }
            }
            else if (_mode == TowerAttackMode.Slow && _target != null && _target.IsAlive)
            {
                _target.TakeDamage(_damage);
                _target.ApplySlow(_slowMult, _slowDuration);
            }
            else if (_target != null && _target.IsAlive)
            {
                _target.TakeDamage(_damage);
            }
            Destroy(gameObject);
        }
    }
}
