using System.Collections.Generic;
using UnityEngine;
using Terrain;

namespace Enemies {
    public class EntitySpawnManager : MonoBehaviour {
        [Header("Player & Terrain")] 
        public Transform player;
        public BaseTerrainGenerator terrain;

        [Header("Enemy Data")] 
        public List<EnemyGroup> enemyGroups = new();

        [Header("Settings")] 
        public float spawnOffsetY = 1f;
        public float inactiveDistance = 100f;                                       // Sight distance around player

        void Start() {
            PositionPlayer();
            SpawnEnemyGroups();
        }

        void PositionPlayer() {
            float h = terrain.GetHeight(0, 0);
            Vector3 pos = new Vector3(0, h + spawnOffsetY, 0);
            player.position = pos;
        }

        void SpawnEnemyGroups() {
            foreach (var group in enemyGroups) {
                Vector3 spawnPos = GetCornerPosition(group.corner);
                float h = terrain.GetHeight((int)spawnPos.x, (int)spawnPos.z);
                spawnPos.y = h + spawnOffsetY;

                for (int i = 0; i < group.count; i++) {
                    Vector3 offset = Random.insideUnitSphere * group.spread;
                    offset.y = 0;

                    Vector3 finalPos = spawnPos + offset;
                    float localH = terrain.GetHeight((int)finalPos.x, (int)finalPos.z);
                    finalPos.y = localH + spawnOffsetY;

                    GameObject enemy = Instantiate(group.prefab, finalPos, Quaternion.identity);

                    bool tooFar = Vector3.Distance(player.position, finalPos) > inactiveDistance;
                    enemy.SetActive(!tooFar);
                    if (tooFar && enemy.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;
                }
            }
        }

        Vector3 GetCornerPosition(SpawnCorner corner) {
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