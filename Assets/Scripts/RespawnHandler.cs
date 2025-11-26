using System.Collections.Generic;
using UnityEngine;
using Terrain;

public class RespawnHandler : MonoBehaviour {
    [Header("Terrain Reference")]
    [Tooltip("Reference to the terrain generator to calculate ground height")]
    public BaseTerrainGenerator terrain;                                        // To know terrain height

    [Header("Settings")]
    [Tooltip("If show limit in scene")]
    [SerializeField] private bool showBoundary = true;
    [SerializeField] private Color boundaryColor = Color.cyan;
    [Tooltip("Minimum Y area (Death zone)")]
    [SerializeField] private float minY = -200f;
    [Tooltip("Maximum Y area")]
    [SerializeField] private float maxY = 1000f;

    [Tooltip("List of objects to watch out.")]
    [SerializeField] private List<Transform> trackedObjects = new();
    
    [Header("Optimization")]
    [Tooltip("Time between each clean check (in frames)")]
    [SerializeField, Range(60, 6000)] private int framesBetweenCleaning = 120;
    [Tooltip("Time between each detection (in frames)")]
    [SerializeField, Range(60, 6000)] private int framesBetweenDetect = 600;

    private bool _automatic;

    void Awake() {
        if (trackedObjects.Count == 0) {
            AutoDetectMovableObjects();
            _automatic = true; 
        }
    }

    void LateUpdate() {
        if (Time.frameCount % framesBetweenCleaning == 0) trackedObjects.RemoveAll(t => !t);    // Cleaning

        foreach (var obj in trackedObjects) {                                   // Check position
            if (!obj) continue;

            float y = obj.position.y;
            if (y < minY || y > maxY) RespawnObject(obj);                       // If out of bounds
        }

        if (_automatic && Time.frameCount % framesBetweenDetect == 0) AutoDetectMovableObjects();   // Auto-detect objects
    }

    private void AutoDetectMovableObjects() {
        CharacterController[] controllers = FindObjectsByType<CharacterController>(FindObjectsSortMode.None);
        Rigidbody[] rigidbodies = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
        
        foreach (var cc in controllers) AddObject(cc.transform);                // Update list
        foreach (var rb in rigidbodies) AddObject(rb.transform);
    }

    private void RespawnObject(Transform obj) {
        Vector3 pos = obj.position;                                             // Get object position

        float terrainHeight = 0f;                                               // Get terrain height
        if (terrain) terrainHeight = terrain.GetHeight((int)pos.x, (int)pos.z);
        else if (Physics.Raycast(new Vector3(pos.x, 1000f, pos.z), Vector3.down, out RaycastHit hit, 2000f))
            terrainHeight = hit.point.y;                                        // Try to detect with raycast

        float halfHeight = GetObjectHeight(obj);                                // Position above ground
        pos.y = terrainHeight + halfHeight + 0.5f;                              // Apply new height
        obj.position = pos;

        if (obj.TryGetComponent(out Rigidbody rb)) {                            // Cancel velocity
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }
    }

    private float GetObjectHeight(Transform obj) {
        if (obj.TryGetComponent(out Collider col)) return col.bounds.extents.y; // Get collider size
        return 1f;
    }

    private void AddObject(Transform obj) {
        if (obj && !trackedObjects.Contains(obj)) trackedObjects.Add(obj);
    }

    void OnDrawGizmosSelected() {
        if (!showBoundary) return;

        Gizmos.color = boundaryColor;                                           // Show boundaries
        Gizmos.DrawWireCube(new Vector3(0, minY, 0), new Vector3(500, 0.1f, 500));
        Gizmos.DrawWireCube(new Vector3(0, maxY, 0), new Vector3(500, 0.1f, 500));
    }
}
