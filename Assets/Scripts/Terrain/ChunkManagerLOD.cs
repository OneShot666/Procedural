// ChunkManager with LOD (D) and memory management (pooling + frustum culling)
// - LOD levels: 0 (near, chunkSize), 1 (mid, chunkSize*2), 2 (far, chunkSize*4)
// - Pooling of chunk GameObjects per LOD
// - Priority load by distance; generation spread across frames
// - Frustum culling and aggressive unload beyond keepDistance
// Usage:
//  * Attach this script to an object in scene.
//  * Provide a chunkPrefab that has a Chunk component (see earlier Chunk.cs) and supports 'voxelStep' (int) property.
//  * Configure chunkSize, renderDistance, lodDistances and pool sizes.
//  * The manager will request/chunk.GenerateBlockData() on pooled chunk instances (synchronous).

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ReSharper disable Unity.PerformanceCriticalCodeInvocation
// ReSharper disable IteratorNeverReturns
[RequireComponent(typeof(MonoBehaviour))]
public class ChunkManagerLOD : MonoBehaviour {
    [Header("References")]
    public Transform target;                       // player or camera to follow
    public Camera renderCamera;
    public GameObject chunkPrefab;                 // prefab must include a Chunk component

    [Header("Base Chunk Settings")]
    public int chunkSize = 16;                     // smallest chunk size (LOD0)
    public int maxHeight = 32;
    public float noiseScale = 20f;

    [Header("LOD & Distances")]
    public int[] lodMultipliers = { 1, 2, 4 }; // multiplies chunkSize
    public float[] lodRanges = { 50f, 150f, 400f }; // distance thresholds for LOD0,LOD1,LOD2

    [Header("Render & Memory")]
    public int renderRadiusInChunks = 4;          // how many chunks (LOD0) around target to attempt to keep
    public int keepRadiusExtra = 2;               // extra ring kept in memory before destroying
    public float updateInterval = 0.18f;          // seconds between checks
    public int createPerFrame = 2;                // spread creation across frames

    [Header("Pooling")]
    public int poolInitialPerLOD = 8;

    // internals
    private Vector2Int _currentChunkCoord = new Vector2Int(int.MinValue, int.MinValue);
    private readonly Dictionary<Vector2Int, ChunkInstance> _activeChunks = new(); // map coord->instance
    private readonly Dictionary<int, Stack<GameObject>> _pool = new(); // LODIndex -> pool stack
    private Plane[] _frustumPlanes;
    private bool _isUpdating;

    void Awake() {
        if (!renderCamera) renderCamera = Camera.main;
        if (!target) target = Camera.main?.transform;

        float seedX = Random.Range(0f, 10000f);
        float seedZ = Random.Range(0f, 10000f);
        chunkPrefab.GetComponent<Chunk>().perlinOffset = new Vector2(seedX, seedZ);

        // prepare pools
        for (int i = 0; i < lodMultipliers.Length; i++) {
            _pool[i] = new Stack<GameObject>(poolInitialPerLOD);
            for (int p = 0; p < poolInitialPerLOD; p++) {
                var go = CreateChunkGameObject(i);
                ReturnToPool(i, go);
            }
        }

        StartCoroutine(UpdateLoop());
    }

    private IEnumerator UpdateLoop() {
        while (true) {
            if (target && chunkPrefab) UpdateIfNeeded();
            yield return new WaitForSeconds(updateInterval);
        }
    }

    private void UpdateIfNeeded() {
        Vector2Int newCoord = WorldToChunk(target.position);
        if (newCoord != _currentChunkCoord && !_isUpdating) {
            _currentChunkCoord = newCoord;
            StartCoroutine(UpdateChunksCoroutine());
        }
    }

    private IEnumerator UpdateChunksCoroutine() {
        _isUpdating = true;

        // determine desired chunk coordinates with LOD classification
        var desired = new Dictionary<Vector2Int,int>(); // coord -> lodIndex

        // we'll generate a square region in LOD0 units, then compute which LOD each coordinate should use
        int r = renderRadiusInChunks + keepRadiusExtra;
        Vector2 centerWorld = new Vector2(target.position.x, target.position.z);

        for (int dx = -r; dx <= r; dx++) {
            for (int dz = -r; dz <= r; dz++) {
                Vector2Int coord0 = new Vector2Int(_currentChunkCoord.x + dx, _currentChunkCoord.y + dz);
                // compute world position of chunk center
                Vector2 chunkWorldPos = new Vector2((coord0.x + 0.5f) * chunkSize, (coord0.y + 0.5f) * chunkSize);
                float dist = Vector2.Distance(chunkWorldPos, centerWorld);

                int lodIndex = GetLODIndexForDistance(dist);
                desired[coord0] = lodIndex;
            }
        }

        // Remove active chunks that are outside keep radius+margin
        var toDestroy = new List<Vector2Int>();
        foreach (var kv in _activeChunks) {
            Vector2Int coord = kv.Key;
            float dx = coord.x - _currentChunkCoord.x;
            float dz = coord.y - _currentChunkCoord.y;
            if (Mathf.Abs(dx) > r || Mathf.Abs(dz) > r) {
                toDestroy.Add(coord);
            }
        }

        foreach (var coord in toDestroy) {
            ReleaseChunk(coord);
            yield return null;
        }

        // prepare creation queue prioritized by distance to target
        var queue = desired.Keys
            .Where(c => !_activeChunks.ContainsKey(c))
            .Select(c => new { coord = c, dist = ChunkCoordDistance(c, _currentChunkCoord) })
            .OrderBy(x => x.dist)
            .Select(x => x.coord)
            .ToList();

        int createdThisFrame = 0;
        foreach (var coord in queue) {
            int lodIndex = desired[coord];
            // allocate or reuse instance
            GameObject go = GetFromPoolOrNew(lodIndex);
            SetupChunkInstance(go, coord, lodIndex);
            createdThisFrame++;
            if (createdThisFrame >= createPerFrame) { createdThisFrame = 0; yield return null; }
        }

        _isUpdating = false;
    }

    private int GetLODIndexForDistance(float dist) {
        for (int i = 0; i < lodRanges.Length; i++) {
            if (dist <= lodRanges[i]) return Mathf.Min(i, lodMultipliers.Length - 1);
        }
        return Mathf.Min(lodRanges.Length, lodMultipliers.Length - 1);
    }

    private float ChunkCoordDistance(Vector2Int a, Vector2Int b) {
        Vector2 wa = new Vector2((a.x + 0.5f) * chunkSize, (a.y + 0.5f) * chunkSize);
        Vector2 wb = new Vector2((b.x + 0.5f) * chunkSize, (b.y + 0.5f) * chunkSize);
        return Vector2.Distance(wa, wb);
    }

    // Create or reuse GameObject for a given LOD
    private GameObject GetFromPoolOrNew(int lodIndex) {
        if (_pool.TryGetValue(lodIndex, out var stack) && stack.Count > 0) {
            var go = stack.Pop();
            go.SetActive(true);
            return go;
        }
        return CreateChunkGameObject(lodIndex);
    }

    private void ReturnToPool(int lodIndex, GameObject go) {
        go.SetActive(false);
        if (!_pool.ContainsKey(lodIndex)) _pool[lodIndex] = new Stack<GameObject>();
        _pool[lodIndex].Push(go);
    }

    private GameObject CreateChunkGameObject(int lodIndex) {
        var go = Instantiate(chunkPrefab, transform);
        go.name = $"PooledChunk_lod{lodIndex}";
        go.SetActive(false);
        // ensure it has a Chunk component
        var chunk = go.GetComponent<Chunk>();
        if (!chunk) {
            Debug.LogError("chunkPrefab must contain a Chunk component");
        }
        return go;
    }

    private void SetupChunkInstance(GameObject go, Vector2Int coord0, int lodIndex) {
        // compute actual chunk size for this LOD
        int mult = lodMultipliers[Mathf.Clamp(lodIndex, 0, lodMultipliers.Length - 1)];
        int actualChunkSize = chunkSize * mult;

        // position = coord0 * chunkSize (we keep coord0 grid), but for larger LOD the chunk covers multiple coords visually
        Vector3 pos = new Vector3(coord0.x * chunkSize, 0f, coord0.y * chunkSize);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.identity;

        var chunk = go.GetComponent<Chunk>();
        if (!chunk) return;

        // Configure chunk generation parameters
        chunk.chunkSize = actualChunkSize;
        chunk.maxHeight = maxHeight;
        chunk.noiseScale = noiseScale * mult; // scale noise for LOD (coarser)
        // chunk.voxelStep is optional; if your Chunk supports it, set it. We try reflectively to be safe.
        var voxelStepField = chunk.GetType().GetField("voxelStep");
        if (voxelStepField != null) voxelStepField.SetValue(chunk, mult);

        chunk.GenerateBlockData(); // synchronous generation: chunk's method will build mesh

        // register active
        var inst = new ChunkInstance { Go = go, LOD = lodIndex, Coord = coord0 };
        _activeChunks[coord0] = inst;
    }

    private void ReleaseChunk(Vector2Int coord) {
        if (!_activeChunks.TryGetValue(coord, out var inst)) return;
        int lod = inst.LOD;
        GameObject go = inst.Go;
        _activeChunks.Remove(coord);
        ReturnToPool(lod, go);
    }

    // convert world position to LOD0 chunk coords
    private Vector2Int WorldToChunk(Vector3 worldPos) {
        int x = Mathf.FloorToInt(worldPos.x / chunkSize);
        int z = Mathf.FloorToInt(worldPos.z / chunkSize);
        return new Vector2Int(x, z);
    }

    void LateUpdate() {
        // frustum culling: disable GameObjects that are not visible
        if (!renderCamera) return;
        _frustumPlanes = GeometryUtility.CalculateFrustumPlanes(renderCamera);

        foreach (var kv in _activeChunks) {
            var inst = kv.Value;
            var go = inst.Go;
            // compute an AABB roughly covering the chunk
            int mult = lodMultipliers[Mathf.Clamp(inst.LOD,0,lodMultipliers.Length-1)];
            Vector3 center = go.transform.position + new Vector3(chunkSize * mult * 0.5f, maxHeight * 0.5f, chunkSize * mult * 0.5f);
            Vector3 size = new Vector3(chunkSize * mult, maxHeight, chunkSize * mult);
            var bounds = new Bounds(center, size);
            bool visible = GeometryUtility.TestPlanesAABB(_frustumPlanes, bounds);
            if (go.activeSelf != visible) go.SetActive(visible);
        }
    }

    // ReSharper disable once NotAccessedField.Local
    private class ChunkInstance {                                               // Small helper struct
        public GameObject Go;
        public int LOD;
        public Vector2Int Coord;
    }
}
