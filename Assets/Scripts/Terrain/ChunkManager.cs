using Random = UnityEngine.Random;
using System.Collections.Generic;
using System.Collections;                                                       // For coroutine
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
        
        private Coroutine _generationCoroutine;                                 // To stop coroutine if needed

        private void Awake() {
            if (useRandomSeed) seed = Random.Range(-100_000, 100_000);
        }

        private void Start() {
            mapSize = renderDistance * chunkSize;
            if (cameraPlayer) UpdateTerrain();
        }

        private void Update() {
            if (!cameraPlayer) return;

            Vector2Int newChunkCoord = WorldToChunkCoord(cameraPlayer.position);
            if (newChunkCoord != _currentChunkCoord) {
                _currentChunkCoord = newChunkCoord;
                UpdateTerrain();
            }
        }

        public void SetCameraPlayer(Transform newTarget) {
            cameraPlayer = newTarget;
            UpdateTerrain();
        }

        public override float GetHeight(float x, float z) {
            float amplitude = 1;
            float frequency = 1;
            float noiseHeight = 0;

            for (int i = 0; i < octaves; i++) {
                float sampleX = (x + perlinOffset.x + seed) / noiseScale * frequency;
                float sampleZ = (z + perlinOffset.y + seed) / noiseScale * frequency;
                float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ) * 2 - 1;    // Between -1 & 1

                noiseHeight += perlinValue * amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            float normalizedHeight = (noiseHeight + 1) / 2f;                    // To normalize between 0 & 1
            int h = Mathf.FloorToInt(normalizedHeight * maxHeight);
            return Mathf.Clamp(h, 1, maxHeight);
        }

        public Transform GetChunkTransform(int x, int z) {
            Vector2Int coord = WorldToChunkCoord(new Vector3(x, 0, z));
            if (_chunks.TryGetValue(coord, out GameObject chunkObj)) return chunkObj.transform;
            return null;
        }

        public override bool IsPositionGenerated(float x, float z) {
            var c = WorldToChunkCoord(new Vector3(x, 0, z));
            return _chunks.ContainsKey(c);
        }

        private void UpdateTerrain() {
            if (!chunkPrefab || !cameraPlayer) return;
            if (_generationCoroutine != null) StopCoroutine(_generationCoroutine);  // Stop current coroutine
            _generationCoroutine = StartCoroutine(GenerateChunksRoutine());     // Have a progressive generation
        }

        IEnumerator GenerateChunksRoutine() {
            List<Vector2Int> needed = new();
            Vector3 p = cameraPlayer.position;
            Vector2Int centerChunk = WorldToChunkCoord(p);

            for (int x = -renderDistance; x <= renderDistance; x++) {           // Calculate what's required
                for (int z = -renderDistance; z <= renderDistance; z++) {
                    var c = new Vector2Int(centerChunk.x + x, centerChunk.y + z);
                    needed.Add(c);
                }
            }

            HashSet<Vector2Int> neededSet = new(needed);                        // Clean old chunks
            List<Vector2Int> toRemove = new();
            foreach (var kv in _chunks) {
                if (!neededSet.Contains(kv.Key)) {
                    Destroy(kv.Value);
                    toRemove.Add(kv.Key);
                }
            }

            foreach (var c in toRemove) _chunks.Remove(c);

            needed.Sort((a, b) => {                                             // Sort by distance
                float distA = Vector2.Distance(a * chunkSize, new Vector2(p.x, p.z));
                float distB = Vector2.Distance(b * chunkSize, new Vector2(p.x, p.z));
                return distA.CompareTo(distB);
            });

            foreach (var coord in needed) {                                     // Generate chunks one by one
                if (!_chunks.ContainsKey(coord)) {
                    CreateChunk(coord);
                    yield return null;                                          // Once per frame
                }
            }
            
            _generationCoroutine = null;
        }

        private void CreateChunk(Vector2Int coord) {
            Vector3 pos = new(coord.x * chunkSize, 0, coord.y * chunkSize);
            GameObject chunkObj = Instantiate(chunkPrefab, pos, Quaternion.identity, transform);
            chunkObj.name = $"Chunk_{coord.x}_{coord.y}";

            Chunk chunk = chunkObj.GetComponent<Chunk>();
            chunk.chunkSize = chunkSize;
            chunk.maxHeight = maxHeight;
            chunk.seed = seed;
            chunk.octaves = octaves;
            chunk.noiseScale = noiseScale;
            chunk.persistence = persistence;
            chunk.lacunarity = lacunarity;
            chunk.perlinOffset = perlinOffset;

            _chunks.Add(coord, chunkObj);

            if (ChunkBiomeGenerator.AllBiomes != null)
                foreach (var biome in ChunkBiomeGenerator.AllBiomes)            // Ask biomes if has to place objects in self
                    if (biome) biome.ProcessChunk(chunk);
        }
    }
}
