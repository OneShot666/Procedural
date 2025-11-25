using UnityEngine;

/// <summary> Spawner object around self </summary>
public class EntitySpawner : MonoBehaviour {
    [Header("Spawn Settings")]
    [SerializeField] private GameObject objectToSpawn;
    [Tooltip("Time between each spawn (in seconds)")]
    [SerializeField] private float spawnInterval = 3f;
    [Tooltip("Total number of entity (-1 for infinite)")]
    [SerializeField] private int maxEntities = -1;
    [Tooltip("Size of the area of spawn around spawner")]
    [SerializeField] private float spawnRadius = 5f;
    [Tooltip("Default height of spawned entities")]
    [SerializeField] private float spawnHeight = 0.1f;
    [Tooltip("[Optional] Parent object in which objects spawn")]
    [SerializeField] private Transform spawnParent;

    [Header("Gizmo settings")]
    [Tooltip("Show area around spawner in scene")]
    [SerializeField] private bool showGizmos = true;

    private int _spawnedCount;
    private float _timer;

    void Update() {
        if (!objectToSpawn) return;
        
        if (!spawnParent) spawnParent = new GameObject($"Spawned {objectToSpawn.gameObject.name}s").transform;

        _timer += Time.deltaTime;
        if (_timer >= spawnInterval) {                                          // Spawn another object
            _timer = 0f;
            if (maxEntities == -1 || _spawnedCount < maxEntities) SpawnEntity();
        }
    }

    private void SpawnEntity() {
        Vector3 randomDir = Random.insideUnitSphere * spawnRadius;              // Get random direction on x-axis
        randomDir.y = 0;
        Vector3 finalPos = transform.position + randomDir + Vector3.up * spawnHeight;

        GameObject entity = Instantiate(objectToSpawn, finalPos, Quaternion.identity, spawnParent);
        entity.name = objectToSpawn.name;                                       // Proper name
        _spawnedCount++;
    }

    void OnDrawGizmosSelected() {
        if (!showGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
