using Random = UnityEngine.Random;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable Unity.PerformanceCriticalCodeInvocation
// ReSharper disable InconsistentNaming
namespace Terrain {
    [ExecuteAlways]
    public class ChunkBiomeGenerator : MonoBehaviour {
        [Header("Terrain Reference")]
        public BaseTerrainGenerator terrain;

        [Header("Biome Settings")]
        [Tooltip("Radius or Half-Size of the area.")]
        public float biomeSize = 500f;
        [Tooltip("Check this to use a Square shape instead of Circle")]
        public bool useSquareShape = true;
        [Tooltip("Color of the ground in this biome")]
        public Color areaColor = new(0.4f, 0.7f, 0.2f);                         // Apply color to ground

        [Header("Objects to spawn")]
        public List<GameObject> objectsPrefabs;

        [Header("Spawning settings")]
        [Range(0, 100)] public int objectsDensityPercent = 30;
        public float yOffset;
        [Tooltip("Layer to assign to spawned objects")]
        public int objectLayer = 6;                                             // Default layer: 0; Big: 6; Small: 7

        [Header("Safety Settings")]
        [Tooltip("Prevent biome to spawn object in a radius around origin")]
        public float spawnSafeRadius = 5f; 

        private static readonly List<ChunkBiomeGenerator> _allBiomes = new();

        public static List<ChunkBiomeGenerator> AllBiomes => _allBiomes;

        private void OnEnable() {                                               // Save biomes
            if (!_allBiomes.Contains(this)) _allBiomes.Add(this);
        }

        private void OnDisable() {                                              // Remove biomes
            if (_allBiomes.Contains(this)) _allBiomes.Remove(this);
        }

        /// <summary> Check if position(x, z) inside biome </summary>
        public bool IsPointInBiome(float x, float z) {
            float dx = Mathf.Abs(x - transform.position.x);
            float dz = Mathf.Abs(z - transform.position.z);

            if (useSquareShape) return dx <= biomeSize && dz <= biomeSize;    // Square biome

            float distSqr = (x - transform.position.x) * (x - transform.position.x) + 
                            (z - transform.position.z) * (z - transform.position.z);
            return distSqr <= biomeSize * biomeSize;                          // Circle biome
        }

        public void ProcessChunk(Chunk chunk) {
            if (objectsPrefabs == null || objectsPrefabs.Count == 0) return;

            int chunkSize = chunk.chunkSize;
            Vector3 chunkPos = chunk.transform.position;
            float safeRadiusSqr = spawnSafeRadius * spawnSafeRadius;            // Calculate radius square

            for (int x = 0; x < chunkSize; x++) {                               // Among all chunks
                for (int z = 0; z < chunkSize; z++) {
                    int worldX = (int)chunkPos.x + x;
                    int worldZ = (int)chunkPos.z + z;

                    float distFromOriginSqr = worldX * worldX + worldZ * worldZ;    // Get distance from origin
                    if (distFromOriginSqr < safeRadiusSqr) continue;            // Ignore if inside spawn zone

                    if (IsPointInBiome(worldX, worldZ)) {
                        float randomVal = RandomHash(worldX, worldZ);
                        if (randomVal < objectsDensityPercent / 2000f) SpawnObjectOnChunk(chunk, worldX, worldZ);
                    }
                }
            }
        }

        private float RandomHash(int x, int z) {                                // Small better random function
            float v = Mathf.Sin(x * 12.9898f + z * 78.233f) * 43758.5453f;
            return v - Mathf.Floor(v);
        }

        private void SpawnObjectOnChunk(Chunk chunk, int x, int z) {
            GameObject prefab = objectsPrefabs[Random.Range(0, objectsPrefabs.Count)];

            float h = terrain.GetHeight(x, z);
            Vector3 pos = new Vector3(x + 0.5f, h + yOffset, z + 0.5f);

            GameObject obj = Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0f, 360f), 0f), chunk.transform);
            SetLayerRecursively(obj, objectLayer);
        }

        private void SetLayerRecursively(GameObject obj, int layer) {
            obj.layer = layer;
            foreach (Transform child in obj.transform) SetLayerRecursively(child.gameObject, layer);
        }

        private void OnDrawGizmosSelected() {
            Gizmos.color = new Color(areaColor.r, areaColor.g, areaColor.b, 0.3f);  // Use biome's color
            Vector3 center = transform.position;
            
            if (useSquareShape) {                                               // Draw square
                Vector3 size = new Vector3(biomeSize * 2, 10f, biomeSize * 2);
                Gizmos.DrawCube(center, new Vector3(size.x, 0.1f, size.z)); // Ground
                Gizmos.DrawWireCube(center, size);                              // Limits
            } else Gizmos.DrawWireSphere(center, biomeSize);                    // Draw circle

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(Vector3.zero, spawnSafeRadius);               // Draw spawn zone
        }
    }
}
