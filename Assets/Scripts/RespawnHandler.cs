using System.Collections.Generic;
using UnityEngine;

/// <summary> Respawn objects only if they leave a vertical range </summary>
public class RespawnHandler : MonoBehaviour {
    [Header("Settings")]
    [Tooltip("If show limit in scene")]
    [SerializeField] private bool showBoundary = true;
    [SerializeField] private Color boundaryColor = Color.cyan;
    [Tooltip("Minimum Y area")]
    [SerializeField] private float minY = -1000f;
    [Tooltip("Maximum Y area")]
    [SerializeField] private float maxY = 10000f;
    [Tooltip("List of objects to watch out.")]
    [SerializeField] private List<Transform> trackedObjects = new();
    [Tooltip("Time between each clean check to remove tracked objects (in seconds)")]
    [SerializeField, Range(5, 600)] private int timerCleaning = 120;
    [Tooltip("Time between each detection of movable objects (in seconds)")]
    [SerializeField, Range(60, 3600)] private int timerDetect = 600;

    private bool _automatic;

    void Awake() {
        if (trackedObjects.Count == 0) {
            AutoDetectMovableObjects();
            _automatic = true; 
        }
    }

    void LateUpdate() {
        if (Time.frameCount % timerCleaning == 0) trackedObjects.RemoveAll(t => !t);

        foreach (var obj in trackedObjects) {
            if (!obj) continue;

            float y = obj.position.y;
            if (y < minY || y > maxY) RespawnObject(obj);
        }

        if (_automatic && Time.frameCount % timerDetect == 0) AutoDetectMovableObjects();
    }

    private void AutoDetectMovableObjects() {
        trackedObjects.Clear();

        CharacterController[] controllers = FindObjectsByType<CharacterController>(FindObjectsSortMode.None);
        Rigidbody[] rigidbodies = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
        Collider[] colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);

        foreach (var cc in controllers) AddObject(cc.transform);
        foreach (var rb in rigidbodies) AddObject(rb.transform);
        foreach (var col in colliders) AddObject(col.transform);
    }

    private void RespawnObject(Transform obj) {
        float height = GetObjectHeight(obj);

        Vector3 pos = obj.position;
        pos.y = height + 1f;                                                    // Respawn vertical
        obj.position = pos;

        if (obj.TryGetComponent(out Rigidbody rb)) {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private float GetObjectHeight(Transform obj) {
        if (obj.TryGetComponent(out Collider col)) return col.bounds.extents.y;
        return 1f;
    }

    private void AddObject(Transform obj) {
        if (obj && !trackedObjects.Contains(obj)) trackedObjects.Add(obj);
    }

    void OnDrawGizmosSelected() {
        if (!showBoundary) return;

        Gizmos.color = boundaryColor;

        // Draw vertical limits as horizontal planes
        Gizmos.DrawWireCube(new Vector3(0, minY, 0), new Vector3(500, 0.1f, 500));
        Gizmos.DrawWireCube(new Vector3(0, maxY, 0), new Vector3(500, 0.1f, 500));
    }
}
