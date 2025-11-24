using System.Collections.Generic;
using UnityEngine;

/// <summary> Respawn every outgoing object to origin </summary>
public class WorldHandler : MonoBehaviour {
    [Header("Settings")]
    [Tooltip("If show limit in scene")]
    [SerializeField] private bool showBoundary = true;
    [SerializeField] private Color boundaryColor = Color.cyan;
    [Tooltip("Range of the world from the origin")]
    [SerializeField, Range(100, 1_000_000)] public float worldSize = 1000;
    [Tooltip("List of objects to watch out.")]
    [SerializeField] private List<Transform> trackedObjects = new();

    private bool _automatic;

    void Awake() {
        if (trackedObjects.Count == 0) {
            AutoDetectMovableObjects();
            _automatic = true;                                                  // Automatic mode
        }
    }

    // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
    void LateUpdate() {
        if (Time.frameCount % 120 == 0) trackedObjects.RemoveAll(t => !t);  // Update every 2 minutes

        foreach (var obj in trackedObjects) {                                   // Auto-respawn objects too far
            if (!obj) { RemoveObject(obj); continue; }
            Vector3 pos = obj.position;
            if (pos.sqrMagnitude > Mathf.Pow(worldSize, 2)) RespawnObject(obj);
        }
        
        if (_automatic && Time.frameCount % 600 == 0) AutoDetectMovableObjects();   // Update every 10 minutes
    }

    private void AutoDetectMovableObjects() {                                   // Get all physical objects
        trackedObjects.Clear();

        CharacterController[] controllers = FindObjectsByType<CharacterController>(FindObjectsSortMode.None);
        Rigidbody[] rigidbodies = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
        Collider[] colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);

        foreach (var cc in controllers) AddObject(cc.transform);
        foreach (var rb in rigidbodies) AddObject(rb.transform);
        foreach (var col in colliders) AddObject(col.transform);

        // print($"[WorldHandler] x{trackedObjects.Count} movables objects detected");
    }

    private void RespawnObject(Transform obj) {
        float height = GetObjectHeight(obj);                                    // Get pos above ground
        obj.position = new Vector3(0f, height + 1f, 0f);
        if (obj.TryGetComponent(out Rigidbody rb)) {                            // Reset speed & angle
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private float GetObjectHeight(Transform obj) {
        if (obj.TryGetComponent(out Collider col)) return col.bounds.extents.y;   // Half height of collider
        return 1f;
    }

    /// <summary> Add object to watch out list </summary>
    private void AddObject(Transform obj) {
        if (obj && !trackedObjects.Contains(obj)) trackedObjects.Add(obj);
    }

    /// <summary> Remove object from list </summary>
    private void RemoveObject(Transform obj) {
        if (obj) trackedObjects.Remove(obj);
    }

    void OnDrawGizmosSelected() {
        if (!showBoundary) return;

        Gizmos.color = boundaryColor;
        Gizmos.DrawWireSphere(Vector3.zero, worldSize);
    }
}
