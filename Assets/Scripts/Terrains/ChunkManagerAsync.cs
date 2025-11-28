using System.Collections.Generic;
using System.Collections;
using UnityEngine;

// ReSharper disable IteratorNeverReturns
// ReSharper disable Unity.PerformanceCriticalCodeInvocation
// [UNUSED IN THE PROJECT YET]
namespace Terrains {
    public class ChunkManagerAsync : BaseTerrainGenerator {
        [Header("References")]
        public Transform target;                
        public Camera renderCamera;             

        [Header("Chunk Settings")]
        [Range(1, 50)] public int renderDistance = 20;

        [Header("Performance")]
        public float updateInterval = 0.25f;                                    // Optimize script
        public int chunksPerFrame = 2;                                          // Spread load

        private Vector2Int _currentChunkCoord = new(int.MinValue, int.MinValue);
        private readonly Dictionary<Vector2Int, GameObject> _chunks = new();
        private Plane[] _frustumPlanes;
        private bool _isUpdating;

        private void Start() {
            if (!target) target = Camera.main?.transform;
            if (!renderCamera) renderCamera = Camera.main;

            StartCoroutine(UpdateLoop());
        }

        private IEnumerator UpdateLoop() {
            while (true) {
                if (target) GenerateTerrain();
                yield return new WaitForSeconds(updateInterval);
            }
        }

        private void GenerateTerrain() {
            if (!target || !chunkPrefab || _isUpdating) return;

            Vector2Int newChunkCoord = GetChunkCoordFromPosition(target.position);
            if (newChunkCoord != _currentChunkCoord) {
                _currentChunkCoord = newChunkCoord;
                StartCoroutine(UpdateChunksSmooth());
            }
        }

        private Vector2Int GetChunkCoordFromPosition(Vector3 pos) {
            int x = Mathf.FloorToInt(pos.x / chunkSize);
            int z = Mathf.FloorToInt(pos.z / chunkSize);
            return new Vector2Int(x, z);
        }

        private IEnumerator UpdateChunksSmooth() {
            _isUpdating = true;

            HashSet<Vector2Int> neededChunks = new();

            for (int x = -renderDistance; x <= renderDistance; x++) {
                for (int z = -renderDistance; z <= renderDistance; z++) {

                    Vector2Int coord = new Vector2Int(
                        _currentChunkCoord.x + x,
                        _currentChunkCoord.y + z
                    );

                    neededChunks.Add(coord);

                    if (!_chunks.ContainsKey(coord)) {
                        CreateChunk(coord);
                        if (--chunksPerFrame <= 0) {
                            chunksPerFrame = 2;
                            yield return null;
                        }
                    }
                }
            }

            List<Vector2Int> toRemove = new();
            foreach (var c in _chunks) {
                if (!neededChunks.Contains(c.Key)) {
                    Destroy(c.Value);
                    toRemove.Add(c.Key);
                    yield return null;
                }
            }
            foreach (var r in toRemove) _chunks.Remove(r);

            _isUpdating = false;
        }

        private void LateUpdate() {
            if (!renderCamera) return;

            _frustumPlanes = GeometryUtility.CalculateFrustumPlanes(renderCamera);

            foreach (var kvp in _chunks) {
                GameObject chunk = kvp.Value;
                Chunk c = chunk.GetComponent<Chunk>();

                if (!c) continue;

                Bounds b = new Bounds(
                    chunk.transform.position + new Vector3(chunkSize / 2f, maxHeight / 2f, chunkSize / 2f),
                    new Vector3(chunkSize, maxHeight, chunkSize)
                );

                bool visible = GeometryUtility.TestPlanesAABB(_frustumPlanes, b);
                chunk.SetActive(visible);
            }
        }

        private void CreateChunk(Vector2Int coord) {
            Vector3 position = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);
            GameObject chunkObj = Instantiate(chunkPrefab, position, Quaternion.identity, transform);

            Chunk chunk = chunkObj.GetComponent<Chunk>();
            if (chunk) {
                chunk.chunkSize = chunkSize;
                chunk.maxHeight = maxHeight;
                chunk.noiseScale = noiseScale;
                chunkObj.name = $"Chunk_{coord.x}_{coord.y}";
            }

            _chunks.Add(coord, chunkObj);
        }
    }
}
