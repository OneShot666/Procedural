using Random = UnityEngine.Random;
using System.Collections.Generic;
using System.Collections;
using Caves;
using Gameplay;
using UnityEngine;
using Terrains;

// ReSharper disable IteratorNeverReturns
namespace Enemies {
    public class EntitySpawnManager : MonoBehaviour {
        [Header("Player & Terrain")] 
        public GameObject playerPrefab; 
        public BaseTerrainGenerator terrain;
        public CaveGenerator cave;

        [Header("Enemy Data")] 
        public List<EnemyGroup> enemyGroups = new();

        [Header("Settings")] 
        public float spawnOffsetY = 1f;
        public float inactiveDistance = 200f;
        public float checkInterval = 0.5f;

        private CameraDistanceCulling _cullingSystem;
        private Transform _playerInstance;                                      // To stock player's reference once spawn
        private readonly List<GameObject> _spawnedEntities = new();

        private void Awake() {
            _cullingSystem = GetComponent<CameraDistanceCulling>();
        }

        IEnumerator Start() {
            yield return null;                                                  // Wait until terrain is generated

            SpawnPlayer();
            SpawnEnemyGroups();
            
            StartCoroutine(CheckEnemyDistancesRoutine());
        }

        void SpawnPlayer() {
            if (!terrain || !playerPrefab) return;

            float h = terrain.GetHeight(0, 0);
            Vector3 spawnPos = new Vector3(0, h + spawnOffsetY, 0);

            GameObject p = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            p.name = "[Player] Evets";
            _playerInstance = p.transform;

            if (p.TryGetComponent<Rigidbody>(out var rb)) rb.linearVelocity = Vector3.zero;

            if (terrain is ChunkManager chunkManager) chunkManager.SetCameraPlayer(_playerInstance);    // Chunk Manager

            Camera cam = p.GetComponentInChildren<Camera>();                    // Camera Culling
            if (_cullingSystem && cam) _cullingSystem.SetupCamera(cam);

            if (cave) cave.SetTarget(_playerInstance);
            else {
                var foundCave = FindAnyObjectByType<CaveGenerator>();
                if (foundCave) foundCave.SetTarget(_playerInstance);
            }
        }

        void SpawnEnemyGroups() {
            foreach (var group in enemyGroups) {
                if (!group.prefab) continue;

                string parentName = $"{group.prefab.name}s";                    // Create entities parent
                GameObject container = GameObject.Find(parentName);
                if (!container) container = new GameObject(parentName);
                Transform parentTransform = container.transform;

                Vector3 spawnPos = GetCornerPosition(group.corner);
                float h = terrain.GetHeight((int)spawnPos.x, (int)spawnPos.z);
                spawnPos.y = h + spawnOffsetY;

                for (int i = 0; i < group.count; i++) {
                    Vector3 offset = Random.insideUnitSphere * group.spread;
                    offset.y = 0;
                    Vector3 finalPos = spawnPos + offset;

                    float localH = terrain.GetHeight((int)finalPos.x, (int)finalPos.z);
                    finalPos.y = localH + spawnOffsetY;

                    GameObject enemy = Instantiate(group.prefab, finalPos, Quaternion.identity, parentTransform);
                    enemy.name = group.prefab.name;
                    _spawnedEntities.Add(enemy);

                    if (_playerInstance) {
                        float dist = Vector3.Distance(_playerInstance.position, finalPos);
                        bool isNear = dist <= inactiveDistance;
                        SetEnemyActiveState(enemy, isNear);
                    }
                }
            }
        }

        IEnumerator CheckEnemyDistancesRoutine() {
            while (true) {
                yield return new WaitForSeconds(checkInterval);

                if (!_playerInstance) continue;                                 // Wait until player respawn

                Vector3 playerPos = _playerInstance.position;

                for (int i = _spawnedEntities.Count - 1; i >= 0; i--) {
                    GameObject enemy = _spawnedEntities[i];

                    if (!enemy) { _spawnedEntities.RemoveAt(i); continue; }

                    float dist = Vector3.Distance(playerPos, enemy.transform.position);
                    bool shouldBeActive = dist <= inactiveDistance;

                    if (enemy.activeSelf != shouldBeActive) SetEnemyActiveState(enemy, shouldBeActive);
                }
            }
        }

        void SetEnemyActiveState(GameObject enemy, bool active) {
            enemy.SetActive(active);

            if (enemy.TryGetComponent<Rigidbody>(out var rb)) {
                if (active) {
                    rb.isKinematic = false;
                    rb.WakeUp();
                } else rb.isKinematic = true;
            }
        }

        Vector3 GetCornerPosition(SpawnCorner corner) {
            if (!terrain) return Vector3.zero;
            
            int half = terrain.mapSize / 2;
            return corner switch {
                SpawnCorner.TopLeft => new Vector3(-half, 0, half),
                SpawnCorner.TopRight => new Vector3(half, 0, half),
                SpawnCorner.BottomLeft => new Vector3(-half, 0, -half),
                SpawnCorner.BottomRight => new Vector3(half, 0, -half),
                _ => Vector3.zero
            };
        }
    }

    [System.Serializable]
    public class EnemyGroup {
        public GameObject prefab;
        public int count = 5;         
        public float spread = 5f;
        public SpawnCorner corner;    
    }

    public enum SpawnCorner { TopLeft, TopRight, BottomLeft, BottomRight }
}
