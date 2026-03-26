using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Controls the 3-wave horde survival system.
    /// Wave 1: 20 normal + 3 bosses at 30s/60s/90s
    /// Wave 2: 40 normal + 3 bosses at 30s/60s/90s
    /// Wave 3: 60 normal + 3 bosses at 30s/60s/90s
    /// </summary>
    public class HordeWaveManager : MonoBehaviour
    {
        public static HordeWaveManager Instance { get; private set; }

        [Header("Wave Configuration")]
        public int[] normalEnemiesPerWave = { 20, 40, 60 };
        public int bossesPerWave = 3;
        public float[] bossSpawnTimes = { 30f, 60f, 90f };

        [Header("Spawn Settings")]
        public float arenaRadius = 40f;
        public float spawnHeight = 0.5f;

        [Header("Enemy Model Paths")]
        public string[] normalEnemyPaths = {
            "Assets/Models/Animals/Mono.glb",
            "Assets/Models/Animals/piranha.glb",
            "Assets/Models/Animals/Sabrewulf.glb",
            "Assets/Models/Animals/Abeja.glb"
        };
        public string[] bossEnemyPaths = {
            "Assets/Models/Animals/T-Rex.glb",
            "Assets/Models/Animals/TyrannosaurusRex.glb"
        };

        public float bossScaleMultiplier = 5f;

        // Runtime state
        public int CurrentWave { get; private set; } = 0;
        public int TotalWaves => normalEnemiesPerWave.Length;
        public bool IsWaveActive { get; private set; } = false;

        private ArenaCombatant _player;
        private List<ArenaCombatant> _activeEnemies = new List<ArenaCombatant>();
        private Coroutine _waveCoroutine;

        private void Awake()
        {
            Instance = this;
        }

        public void StartHorde(ArenaCombatant player)
        {
            _player = player;
            StartNextWave();
        }

        private void StartNextWave()
        {
            CurrentWave++;
            if (CurrentWave > TotalWaves)
            {
                Debug.Log("[HordeWaveManager] All waves cleared - VICTORY!");
                ArenaGameManager.Instance?.TriggerVictory();
                return;
            }

            Debug.Log($"[HordeWaveManager] === WAVE {CurrentWave} / {TotalWaves} STARTING ===");
            IsWaveActive = true;
            _activeEnemies.Clear();

            if (_waveCoroutine != null) StopCoroutine(_waveCoroutine);
            _waveCoroutine = StartCoroutine(RunWaveRoutine(CurrentWave));

            if (ArenaHUD.Instance != null)
                ArenaHUD.Instance.ShowWaveAnnouncement(CurrentWave, TotalWaves);
        }

        private IEnumerator RunWaveRoutine(int waveIndex)
        {
            int normalCount = normalEnemiesPerWave[waveIndex - 1];
            float waveStartTime = Time.time;

            // Spawn all normal enemies in batches to avoid frame spikes
            yield return StartCoroutine(SpawnNormalBatches(normalCount));

            // Timed boss spawns
            for (int i = 0; i < bossesPerWave; i++)
            {
                float delay = waveStartTime + bossSpawnTimes[i] - Time.time;
                if (delay > 0f) yield return new WaitForSeconds(delay);

                SpawnOneBoss();
                Debug.Log($"[HordeWaveManager] Wave {waveIndex} - Boss {i + 1} spawned at {Time.time - waveStartTime:F0}s");
            }

            // Wait until all enemies defeated
            yield return new WaitUntil(AllEnemiesDefeated);

            Debug.Log($"[HordeWaveManager] Wave {waveIndex} complete!");
            IsWaveActive = false;
            yield return new WaitForSeconds(3f);
            StartNextWave();
        }

        private IEnumerator SpawnNormalBatches(int total)
        {
            int batchSize = 5;
            int spawned = 0;
            while (spawned < total)
            {
                int count = Mathf.Min(batchSize, total - spawned);
                for (int i = 0; i < count; i++) SpawnOneNormalEnemy();
                spawned += count;
                yield return new WaitForSeconds(0.4f);
            }
        }

        private void SpawnOneNormalEnemy()
        {
            string path = normalEnemyPaths[Random.Range(0, normalEnemyPaths.Length)];
            bool isAbeja = path.Contains("Abeja");
            Vector3 pos = RandomEdgePosition();
            if (isAbeja) pos.y = 2.5f;

            var combatant = HordeEnemySpawner.SpawnNormalEnemy(path, pos, isAbeja);
            if (combatant != null)
            {
                combatant.OnDeath += (_) => _activeEnemies.RemoveAll(c => c == null || !c.IsAlive);
                _activeEnemies.Add(combatant);
            }
        }

        private void SpawnOneBoss()
        {
            string path = bossEnemyPaths[Random.Range(0, bossEnemyPaths.Length)];
            Vector3 pos = RandomEdgePosition();
            pos.y = 0.5f;

            var combatant = HordeEnemySpawner.SpawnBossEnemy(path, pos, bossScaleMultiplier);
            if (combatant != null)
            {
                combatant.OnDeath += (_) => _activeEnemies.RemoveAll(c => c == null || !c.IsAlive);
                _activeEnemies.Add(combatant);
            }
        }

        private Vector3 RandomEdgePosition()
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float r = Random.Range(arenaRadius * 0.55f, arenaRadius * 0.85f);
            return new Vector3(Mathf.Cos(angle) * r, spawnHeight, Mathf.Sin(angle) * r);
        }

        private bool AllEnemiesDefeated()
        {
            _activeEnemies.RemoveAll(c => c == null);
            return _activeEnemies.Count == 0;
        }

        public int EnemiesRemaining()
        {
            _activeEnemies.RemoveAll(c => c == null);
            return _activeEnemies.Count;
        }
    }
}
