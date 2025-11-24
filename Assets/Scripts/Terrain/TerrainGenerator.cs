using UnityEngine;

// ReSharper disable Unity.PerformanceCriticalCodeInvocation
// ReSharper disable Unity.PerformanceAnalysis
[ExecuteInEditMode]
public class TerrainGenerator : MonoBehaviour {                                 // Create relief on a square area
    [Header("Chunk Prefab (must have Chunk.cs)")]
    public GameObject chunkPrefab;

    [Header("Terrain Settings")]
    public string parentName = "TerrainMap";
    public int width = 100;
    public int length = 100;
    public int maxHeight = 20;

    [Header("Noise Settings")]
    public float noiseScale = 10f;

    [Header("Chunk Settings")]
    public int chunkSize = 16;

    private Transform _terrainParent;
    private bool _isDirty;

    private void Update() {
        if (_isDirty) {
            GenerateTerrain();
            _isDirty = false;
        }
    }

    private void OnValidate() => _isDirty = true;

    [ContextMenu("Generate Terrain")]
    public void GenerateTerrain() {
        if (!chunkPrefab) {
            Debug.LogError("Assign a Chunk Prefab with Chunk.cs inside!");
            return;
        }

        ClearPreviousTerrain();
        CreateParent();

        int chunksX = Mathf.CeilToInt((float)width / chunkSize);
        int chunksZ = Mathf.CeilToInt((float)length / chunkSize);

        for (int cx = 0; cx < chunksX; cx++)
            for (int cz = 0; cz < chunksZ; cz++)
                GenerateChunk(cx, cz);
    }

    private void ClearPreviousTerrain() {
        Transform old = transform.Find(parentName);
        if (old) DestroyImmediate(old.gameObject);
    }

    private void CreateParent() {
        GameObject parent = new GameObject(parentName) {
            transform = { parent = transform, localPosition = Vector3.zero } };
        _terrainParent = parent.transform;
    }

    private void GenerateChunk(int cx, int cz) {
        Vector3 chunkPos = new Vector3(cx * chunkSize, 0, cz * chunkSize);      // Global pos of chunk in scene
        print(chunkPos);    // !!!

        GameObject obj = Instantiate(chunkPrefab, transform.position + chunkPos, Quaternion.identity, _terrainParent);
        obj.name = $"Chunk_{cx}_{cz}";

        Chunk chunk = obj.GetComponent<Chunk>();
        chunk.chunkSize = chunkSize;
        chunk.maxHeight = maxHeight;
        chunk.noiseScale = noiseScale;
    }
}
