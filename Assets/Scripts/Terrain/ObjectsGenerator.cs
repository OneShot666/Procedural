using Random = UnityEngine.Random;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once Unity.PerformanceCriticalCodeInvocation
[ExecuteAlways]
public class ObjectsGenerator : MonoBehaviour {                                 // Spawn objects in circle area
    [Header("List of objects to spawn")]
    public List<GameObject> treePrefabs;
    public string parentName = "ForestGround";

    [Header("Size of area")]
    [Tooltip("Radius of the forest area (in meters).")]
    public float forestRadius = 50f;

    [Header("Ground Settings")]
    public Color groundColor = new(0.4f, 0.7f, 0.2f);
    public float groundY;

    [Header("Spawning Options")]
    [Tooltip("If true, uses treeDensity instead of treeCount")]
    public bool useDensity = true;
    [Tooltip("Number of objects to spawn (ignored if useDensity = true)")]
    [Range(0, 500)] public int treeCount = 100;
    [Tooltip("Objects per square meter (only if useDensity = true)")]
    [Range(0, 100)] public int treeDensityPercent = 50;

    [Header("Tree Offset & Spacing")]
    [Tooltip("Vertical offset so objects don't appear inside the ground")]
    public float yOffset;
    [Tooltip("Minimum distance between objects")]
    public float minSpacing = 1f;
    [Tooltip("Optional border distance from the edge of the area")]
    public float border;

    private GameObject _groundObject;
    private readonly List<Vector3> _spawnedPositions = new();
    private bool _isDirty;

    private void Start() {
        GenerateObjects();
    }

    private void Update() {
        if (_isDirty) {
            GenerateObjects();
            _isDirty = false;
        }
    }

    private void OnValidate() => _isDirty = true;

    // ReSharper disable Unity.PerformanceAnalysis
    [ContextMenu("Generate Forest")]
    private void GenerateObjects() {
        if (treePrefabs == null || treePrefabs.Count == 0) {
            Debug.LogWarning("No prefabs assigned to ForestGenerator.");
            return;
        }

        ClearPreviousForest();
        CreateGround();
        _spawnedPositions.Clear();

        int count = treeCount;
        if (useDensity) {
            float area = Mathf.PI * forestRadius * forestRadius;
            float treeDensity = treeDensityPercent * 0.001f;                     // % to fraction
            count = Mathf.RoundToInt(area * treeDensity);
        }

        for (int i = 0; i < count; i++) SpawnRandomObject();
    }

    private void ClearPreviousForest() {
        Transform previousParent = transform.Find(parentName);
        if (previousParent) DestroyImmediate(previousParent.gameObject);
    }

    private void CreateGround() {
        _groundObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        _groundObject.name = parentName;
        _groundObject.transform.parent = transform;
        _groundObject.transform.localPosition = new Vector3(0, groundY, 0);

        float scale = forestRadius * 2f;
        _groundObject.transform.localScale = new Vector3(scale, 0.1f, scale);

        var areaRenderer = _groundObject.GetComponent<Renderer>();
        areaRenderer.sharedMaterial = new Material(Shader.Find("Unlit/Color")) { color = groundColor };

        DestroyImmediate(_groundObject.GetComponent<Collider>());
    }

    private void SpawnRandomObject() {
        GameObject prefab = treePrefabs[Random.Range(0, treePrefabs.Count)];

        Vector3 pos;
        int attempts = 0;
        do {
            attempts++;
            Vector2 point = Random.insideUnitCircle * (forestRadius - border);
            pos = new Vector3(point.x, yOffset, point.y) + transform.position;

            if (attempts > 50) break;
        } while (!IsPositionValid(pos));

        _spawnedPositions.Add(pos);

        // Crée un sous-parent pour chaque type de prefab si nécessaire
        Transform typeParent = _groundObject.transform.Find(prefab.name);
        if (!typeParent) {
            GameObject newParent = new GameObject(prefab.name) {
                transform = { parent = _groundObject.transform, localPosition = Vector3.zero }
            };
            typeParent = newParent.transform;
        }

        // Instancie l'objet et le place sous son sous-parent
        GameObject obj = Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0f, 360f), 0f), typeParent);
        obj.name = prefab.name + " " + (_spawnedPositions.Count);
    }

    private bool IsPositionValid(Vector3 pos) {
        foreach (Vector3 existingPos in _spawnedPositions)
            if (Vector3.Distance(pos, existingPos) < minSpacing)
                return false;
        return true;
    }
}
