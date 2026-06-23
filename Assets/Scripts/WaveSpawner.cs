using System.Collections;
using UnityEngine;

namespace NightWatch
{
    public class WaveSpawner : MonoBehaviour
    {
        public bool FinishedSpawning { get; private set; }

        Coroutine _routine;
        int _spawnIndex;

        public void StartWave(int waveIndex)
        {
            if (_routine != null)
                StopCoroutine(_routine);
            FinishedSpawning = false;
            _spawnIndex = 0;
            _routine = StartCoroutine(SpawnWave(waveIndex));
        }

        IEnumerator SpawnWave(int waveIndex)
        {
            var paths = GameManager.Instance.SpawnPaths;

            if (GameConfig.IsBossWave(waveIndex))
            {
                yield return SpawnBossWave(waveIndex, paths);
                FinishedSpawning = true;
                yield break;
            }

            var diff = GameManager.Instance.SelectedDifficulty;
            var composition = GameConfig.GetWaveComposition(waveIndex, diff);
            float miniWavePause = GameConfig.MiniWavePause * DifficultyConfig.Get(diff).MiniWavePauseMult;
            int miniWaveCount = 0;

            for (int typeIdx = 0; typeIdx < GameConfig.EnemyTypesCount; typeIdx++)
            {
                int count = composition[typeIdx];
                var enemyType = (EnemyType)typeIdx;

                for (int i = 0; i < count; i++)
                {
                    int pathIdx = _spawnIndex % paths.Length;
                    _spawnIndex++;
                    SpawnEnemy(enemyType, waveIndex, paths[pathIdx]);

                    miniWaveCount++;
                    yield return new WaitForSeconds(GameConfig.EnemySpawnInterval);

                    if (miniWaveCount >= GameConfig.MiniWaveSize)
                    {
                        miniWaveCount = 0;
                        GameManager.Instance?.SetMessage("Міні-хвиля... наступна група");
                        yield return new WaitForSeconds(miniWavePause);
                    }
                }

                if (count > 0)
                    yield return new WaitForSeconds(1.2f);
            }

            FinishedSpawning = true;
        }

        IEnumerator SpawnBossWave(int waveIndex, Vector3[][] paths)
        {
            var gm = GameManager.Instance;
            gm?.SetMessage("БОС! Викликає швидких міньонів!");

            var diff = GameManager.Instance.SelectedDifficulty;
            int scoutCount = Mathf.CeilToInt(3 * DifficultyConfig.Get(diff).EnemyCountMult);

            for (int i = 0; i < scoutCount; i++)
            {
                SpawnEnemy(EnemyType.Scout, waveIndex, paths[i % paths.Length]);
                yield return new WaitForSeconds(1.2f);
            }

            yield return new WaitForSeconds(2f);

            int bossPath = 1;
            if (paths.Length > bossPath)
                SpawnBoss(waveIndex, paths[bossPath]);
        }

        void SpawnEnemy(EnemyType type, int waveIndex, Vector3[] path)
        {
            if (path == null || path.Length == 0) return;

            var go = new GameObject($"Enemy_{type}");
            go.transform.SetParent(GameManager.Instance.GetEnemyContainer());
            go.transform.position = path[0];
            var enemy = go.AddComponent<Enemy>();
            enemy.Initialize(type, waveIndex, path);
        }

        void SpawnBoss(int waveIndex, Vector3[] path)
        {
            if (path == null || path.Length == 0) return;

            var go = new GameObject("Boss");
            go.transform.SetParent(GameManager.Instance.GetEnemyContainer());
            go.transform.position = path[0];
            var boss = go.AddComponent<Enemy>();
            boss.InitializeAsBoss(waveIndex, path);
        }
    }
}
