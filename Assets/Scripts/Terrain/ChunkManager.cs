using System.Collections.Generic;
using UnityEngine;

// ReSharper disable Unity.PerformanceCriticalCodeInvocation
namespace Terrain {
    public class ChunkManager : BaseTerrainGenerator {
        [Header("References")]
        public Transform cameraPlayer;

        [Header("Chunk Settings")]
        [Range(1, 50)] public int renderDistance = 8;

        private Vector2Int _currentChunkCoord = new(int.MinValue, int.MinValue);
        private readonly Dictionary<Vector2Int, GameObject> _chunks = new();

        private void Start() {
            mapSize = renderDistance * chunkSize;
            if (cameraPlayer) GenerateTerrain();
        }

        private void Update() {
            if (!cameraPlayer) return;

            Vector2Int newChunkCoord = WorldToChunkCoord(cameraPlayer.position);
            if (newChunkCoord != _currentChunkCoord) {
                _currentChunkCoord = newChunkCoord;
                GenerateTerrain();
            }
        }

        public void SetCameraPlayer(Transform newTarget) => cameraPlayer = newTarget;

        public override float GetHeight(float x, float z) {
            float noise = Mathf.PerlinNoise(x / noiseScale, z / noiseScale);
            return noise * maxHeight;
        }

        public override bool IsPositionGenerated(float x, float z) {
            var c = WorldToChunkCoord(new Vector3(x, 0, z));
            return _chunks.ContainsKey(c);
        }

        private void GenerateTerrain() {
            if (!chunkPrefab || ! cameraPlayer) return;

            List<Vector2Int> needed = new();

            Vector3 p = cameraPlayer.position;
            Vector2Int centerChunk = WorldToChunkCoord(p);                      // Get current central chunk coord

            for (int x = -renderDistance; x <= renderDistance; x++) {
                for (int z = -renderDistance; z <= renderDistance; z++) {
                    var c = new Vector2Int(centerChunk.x + x, centerChunk.y + z);
                    needed.Add(c);
                }
            }

            needed.Sort((a, b) => {
                float distA = Vector2.Distance(a * chunkSize, new Vector2(p.x, p.z));
                float distB = Vector2.Distance(b * chunkSize, new Vector2(p.x, p.z));
                return distA.CompareTo(distB);
            });

            HashSet<Vector2Int> neededSet = new(needed);
            List<Vector2Int> toRemove = new();                                  // Delete unused chunks

            foreach (var kv in _chunks) {
                if (!neededSet.Contains(kv.Key)) {
                    Destroy(kv.Value);
                    toRemove.Add(kv.Key);
                }
            }

            foreach (var c in toRemove) _chunks.Remove(c);

            foreach (var coord in needed)                                       // Create missing
                if (!_chunks.ContainsKey(coord)) CreateChunk(coord);
        }

        private void CreateChunk(Vector2Int coord) {
            Vector3 pos = new(coord.x * chunkSize, 0, coord.y * chunkSize);
            GameObject chunkObj = Instantiate(chunkPrefab, pos, Quaternion.identity, transform);
            chunkObj.name = $"Chunk_{coord.x}_{coord.y}";

            Chunk chunk = chunkObj.GetComponent<Chunk>();
            chunk.chunkSize = chunkSize;
            chunk.maxHeight = maxHeight;
            chunk.noiseScale = noiseScale;
            chunk.perlinOffset = perlinOffset;

            _chunks.Add(coord, chunkObj);
            chunk.GenerateBlockData();                                          // Generate mesh under player
        }
    }
}
