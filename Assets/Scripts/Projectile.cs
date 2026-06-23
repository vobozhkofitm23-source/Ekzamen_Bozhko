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

            var go = GameObject.CreatePrimitive(GetShape(type));
            go.name = "Projectile";
            go.transform.position = origin;
            go.transform.localScale = Vector3.one * GetSize(type);
            Object.Destroy(go.GetComponent<Collider>());

            var r = go.GetComponent<Renderer>();
            r.material = CreateProjectileMaterial(GameConfig.GetProjectileColor(type));

            var proj = go.AddComponent<Projectile>();
            proj._target = target;
            proj._damage = damage;
            proj._speed = GetSpeed(type);
            proj._mode = mode;
            proj._aoeRadius = aoeRadius;
            proj._slowMult = slowMult;
            proj._slowDuration = slowDuration;
        }

        public static void FireArc(Vector3 origin, Vector3 targetPos, float damage, TowerType type, float aoeRadius)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "MortarShell";
            go.transform.position = origin;
            go.transform.localScale = Vector3.one * 0.35f;
            Object.Destroy(go.GetComponent<Collider>());

            var r = go.GetComponent<Renderer>();
            r.material = CreateProjectileMaterial(new Color(1f, 0.55f, 0.15f));

            var proj = go.AddComponent<Projectile>();
            proj._targetPos = targetPos;
            proj._useTargetPos = true;
            proj._damage = damage;
            proj._speed = 9f;
            proj._mode = TowerAttackMode.Aoe;
            proj._aoeRadius = aoeRadius;
        }

        static PrimitiveType GetShape(TowerType type) => type switch
        {
            TowerType.Archer => PrimitiveType.Cylinder,
            TowerType.Cannon => PrimitiveType.Sphere,
            TowerType.Freeze => PrimitiveType.Cube,
            TowerType.Lightning => PrimitiveType.Cylinder,
            _ => PrimitiveType.Sphere
        };

        static float GetSize(TowerType type) => type switch
        {
            TowerType.Archer => 0.12f,
            TowerType.Cannon => 0.22f,
            TowerType.Mortar => 0.35f,
            TowerType.Freeze => 0.18f,
            TowerType.Lightning => 0.1f,
            _ => 0.2f
        };

        static float GetSpeed(TowerType type) => type switch
        {
            TowerType.Archer => 22f,
            TowerType.Cannon => 14f,
            TowerType.Freeze => 16f,
            TowerType.Lightning => 28f,
            _ => 18f
        };

        static Material CreateProjectileMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;
            return mat;
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

            var dir = goal - transform.position;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);

            if (Vector3.Distance(transform.position, goal) < 0.35f)
                Impact();
        }

        void Impact()
        {
            switch (_mode)
            {
                case TowerAttackMode.Aoe:
                    foreach (var e in GameManager.Instance.ActiveEnemies)
                    {
                        if (e == null || !e.IsAlive) continue;
                        var center = _useTargetPos ? _targetPos : _target.transform.position;
                        if (Vector3.Distance(center, e.transform.position) <= _aoeRadius)
                            e.TakeDamage(_damage);
                    }
                    break;
                case TowerAttackMode.Slow:
                    if (_target != null && _target.IsAlive)
                    {
                        _target.TakeDamage(_damage);
                        _target.ApplySlow(_slowMult, _slowDuration);
                    }
                    break;
                default:
                    if (_target != null && _target.IsAlive)
                        _target.TakeDamage(_damage);
                    break;
            }
            Destroy(gameObject);
        }
    }
}
