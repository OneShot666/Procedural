using Random = UnityEngine.Random;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable Unity.PerformanceCriticalCodeInvocation
// ReSharper disable InconsistentNaming
namespace Terrains {
    [ExecuteAlways]
    public class ChunkBiomeGenerator : MonoBehaviour {
        [Header("Terrain Reference")]
        public BaseTerrainGenerator terrain;

        [Header("Biome Settings")]
        [Tooltip("If true, this biome is everywhere (Background). Use it to fill gaps.")]
        public bool isGlobalBiome;
        [Tooltip("Radius or Half-Size of the area.")]
        public float biomeSize = 500f;
        [Tooltip("Check this to use a Square shape instead of Circle")]
        public bool useSquareShape = true;
        [Tooltip("Color of the ground in this biome")]
        public Color areaColor = new(0.4f, 0.7f, 0.2f);

        [Header("Irregular Borders")]
        [Tooltip("Perturbations size (Higher for larger noise)")]
        public float borderNoiseScale = 20f; 
        [Tooltip("Perturbations force (Distance from border)")]
        public float borderNoiseStrength = 10f; 

        [Header("Objects to spawn")]
        public List<GameObject> objectsPrefabs;
        public bool drawGizmos = true;

        [Header("Spawning settings")]
        [Tooltip("If false, objects won't spawn if ground height <= water level")]
        public bool allowUnderwater;
        [Range(0, 100)] public int objectsDensityPercent = 30;
        public float yOffset;
        [Tooltip("Layer to assign to spawned objects")]
        public int objectLayer = 6;

        [Header("Spawn zone Settings")]
        [Tooltip("Prevent biome to spawn object in a radius around origin")]
        public float spawnSafeRadius = 5f; 

        private static readonly List<ChunkBiomeGenerator> _allBiomes = new();

        public static List<ChunkBiomeGenerator> AllBiomes => _allBiomes;

        private void OnEnable() {
            if (!_allBiomes.Contains(this)) _allBiomes.Add(this);
        }

        private void OnDisable() {
            if (_allBiomes.Contains(this)) _allBiomes.Remove(this);
        }

        public bool IsPointInBiome(float x, float z) {
            if (isGlobalBiome) return true;

            float noiseValue = Mathf.PerlinNoise((x + 1000) / borderNoiseScale, (z + 1000) / borderNoiseScale);
            float noiseOffset = (noiseValue * 2f - 1f) * borderNoiseStrength;
            
            float dx = Mathf.Abs(x - transform.position.x);
            float dz = Mathf.Abs(z - transform.position.z);

            if (useSquareShape) return dx + noiseOffset <= biomeSize && dz + noiseOffset <= biomeSize;

            float dist = Mathf.Sqrt(dx*dx + dz*dz);
            return dist + noiseOffset <= biomeSize;
        }

        public void ProcessChunk(Chunk chunk) {
            if (objectsPrefabs == null || objectsPrefabs.Count == 0) return;

            int chunkSize = chunk.chunkSize;
            Vector3 chunkPos = chunk.transform.position;
            float safeRadiusSqr = spawnSafeRadius * spawnSafeRadius;

            for (int x = 0; x < chunkSize; x++) {
                for (int z = 0; z < chunkSize; z++) {
                    int worldX = (int)chunkPos.x + x;
                    int worldZ = (int)chunkPos.z + z;

                    float distFromOriginSqr = worldX * worldX + worldZ * worldZ;
                    if (distFromOriginSqr < safeRadiusSqr) continue;

                    if (IsPointInBiome(worldX, worldZ)) {
                        float randomVal = RandomHash(worldX, worldZ);
                        if (randomVal < objectsDensityPercent / 2000f) SpawnObjectOnChunk(chunk, worldX, worldZ);
                    }
                }
            }
        }

        private float RandomHash(int x, int z) {
            float v = Mathf.Sin(x * 12.9898f + z * 78.233f) * 43758.5453f;
            return v - Mathf.Floor(v);
        }

        private void SpawnObjectOnChunk(Chunk chunk, int x, int z) {
            float h = terrain.GetHeight(x, z);

            if (!allowUnderwater && h <= chunk.waterLevel) return;              // Don't spawn under water

            GameObject prefab = objectsPrefabs[Random.Range(0, objectsPrefabs.Count)];
            Vector3 pos = new Vector3(x + 0.5f, h + yOffset, z + 0.5f);
            GameObject obj = Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0f, 360f), 0f), chunk.transform);
            SetLayerRecursively(obj, objectLayer);
        }

        private void SetLayerRecursively(GameObject obj, int layer) {
            obj.layer = layer;
            foreach (Transform child in obj.transform) SetLayerRecursively(child.gameObject, layer);
        }

        private void OnDrawGizmosSelected() {
            if (!drawGizmos) return;

            Gizmos.color = new Color(areaColor.r, areaColor.g, areaColor.b, 0.3f);
            Vector3 center = transform.position;

            if (isGlobalBiome) Gizmos.DrawWireCube(center, Vector3.one * 1000f);
            else if (useSquareShape) {
                Vector3 size = new Vector3(biomeSize * 2, 10f, biomeSize * 2);
                Gizmos.DrawCube(center, new Vector3(size.x, 0.1f, size.z));
                Gizmos.DrawWireCube(center, size);
            } else Gizmos.DrawWireSphere(center, biomeSize);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(Vector3.zero, spawnSafeRadius);
        }
    }
}
