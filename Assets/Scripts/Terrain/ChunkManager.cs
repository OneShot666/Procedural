using System.Collections.Generic;
using UnityEngine;

// ReSharper disable Unity.PerformanceCriticalCodeInvocation
namespace Terrain {
    public class ChunkManager : BaseTerrainGenerator {
        [Header("References")]
        public Transform target;

        [Header("Chunk Settings")]
        [Range(1, 50)] public int renderDistance = 20;

        private Vector2Int _currentChunkCoord = new(int.MinValue, int.MinValue);
        private readonly Dictionary<Vector2Int, GameObject> _chunks = new();

        private void Start() {
            mapSize = renderDistance * chunkSize;
            if (!target) target = Camera.main?.transform;
            GenerateTerrain();
        }

        private void Update() {
            Vector2Int newChunkCoord = WorldToChunkCoord(target.position);
            if (newChunkCoord != _currentChunkCoord) {
                _currentChunkCoord = newChunkCoord;
                GenerateTerrain();
            }
        }

        public override float GetHeight(float x, float z) {
            float noise = Mathf.PerlinNoise(x / noiseScale, z / noiseScale);
            return noise * maxHeight;
        }

        public override bool IsPositionGenerated(float x, float z) {
            var c = WorldToChunkCoord(new Vector3(x, 0, z));
            return _chunks.ContainsKey(c);
        }

        private void GenerateTerrain() {
            if (!chunkPrefab) return;

            List<Vector2Int> needed = new();
        
            for (int x = -renderDistance; x <= renderDistance; x++) {
                for (int z = -renderDistance; z <= renderDistance; z++) {
                    var c = new Vector2Int(_currentChunkCoord.x + x, _currentChunkCoord.y + z);
                    needed.Add(c);
                }
            }

            Vector3 p = target.position;
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

            _chunks.Add(coord, chunkObj);
        }
    }
}
