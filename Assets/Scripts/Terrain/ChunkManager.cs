using System.Collections.Generic;
using UnityEngine;

// ReSharper disable Unity.PerformanceCriticalCodeInvocation
public class ChunkManager : MonoBehaviour {
    [Header("References")]
    public Transform target;

    [Header("Chunk Settings")]
    public GameObject chunkPrefab;
    public int chunkSize = 16;
    [Range(1, 50)] public int renderDistance = 20;

    [Header("Terrain Settings")]
    public int maxHeight = 30;
    public float noiseScale = 30f;

    private Vector2Int _currentChunkCoord = new(int.MinValue, int.MinValue);
    private readonly Dictionary<Vector2Int, GameObject> _chunks = new();

    private void Start() {
        if (!target) target = Camera.main?.transform;
        GenerateTerrain();
    }

    private void Update() {
        Vector2Int newChunkCoord = GetChunkCoordFromPosition(target.position);
        if (newChunkCoord != _currentChunkCoord) {
            _currentChunkCoord = newChunkCoord;
            GenerateTerrain();
        }
    }

    private Vector2Int GetChunkCoordFromPosition(Vector3 pos) {
        return new Vector2Int(
            Mathf.FloorToInt(pos.x / chunkSize),
            Mathf.FloorToInt(pos.z / chunkSize)
        );
    }

    private void GenerateTerrain() {
        if (!chunkPrefab) return;

        List<Vector2Int> needed = new();
        
        // Collect all needed chunk coordinates
        for (int x = -renderDistance; x <= renderDistance; x++) {
            for (int z = -renderDistance; z <= renderDistance; z++) {
                needed.Add(new Vector2Int(_currentChunkCoord.x + x, _currentChunkCoord.y + z));
            }
        }

        // SORT chunks by distance BEFORE generating them
        Vector3 targetPos = target.position;
        needed.Sort((a, b) => {
            float distA = Vector2.Distance(new Vector2(a.x, a.y) * chunkSize, new Vector2(targetPos.x, targetPos.z));
            float distB = Vector2.Distance(new Vector2(b.x, b.y) * chunkSize, new Vector2(targetPos.x, targetPos.z));
            return distA.CompareTo(distB);
        });

        HashSet<Vector2Int> neededSet = new HashSet<Vector2Int>(needed);

        // Destroy unnecessary chunks
        List<Vector2Int> removeList = new();
        foreach (var kvp in _chunks) {
            if (!neededSet.Contains(kvp.Key)) {
                Destroy(kvp.Value);
                removeList.Add(kvp.Key);
            }
        }
        foreach (var coord in removeList)
            _chunks.Remove(coord);

        // Create missing chunks IN ORDER OF PRIORITY
        foreach (var coord in needed) {
            if (!_chunks.ContainsKey(coord)) {
                CreateChunk(coord);
            }
        }
    }

    private void CreateChunk(Vector2Int coord) {
        Vector3 pos = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);

        GameObject chunkObj = Instantiate(chunkPrefab, pos, Quaternion.identity, transform);
        chunkObj.name = $"Chunk_{coord.x}_{coord.y}";

        Chunk chunk = chunkObj.GetComponent<Chunk>();
        chunk.chunkSize = chunkSize;
        chunk.maxHeight = maxHeight;
        chunk.noiseScale = noiseScale;

        _chunks.Add(coord, chunkObj);
    }
}
