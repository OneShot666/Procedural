using UnityEngine;

// L Prioritize target feature seems to work but could be upgraded : finding target seems laggy
namespace Enemies {
    /// <summary> Main enemy behaviour : detect and pursue player </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BaseEnemy : MonoBehaviour {
        [Header("Health settings")]
        [SerializeField] private float maxHealth = 100;

        [Header("Optional prioritize target")]
        [Tooltip("If set, the enemy will prioritize this target when within detection range")]
        [SerializeField] private GameObject prioritizeTargetPrefab;

        [Header("Attack settings")]
        [SerializeField] private int damage;

        [Header("Detection settings")]
        [Tooltip("Range of sight of enemy")]
        [SerializeField] private float detectionRange = 15f;
        [Tooltip("Angle of filed of vision (in degree)")]
        [SerializeField] private float fieldOfView = 120f;
        [Tooltip("Max height of detection")]
        [SerializeField] private float detectionHeight = 3f;

        [Header("Movement settings")]
        [Tooltip("Enemy move speed")]
        [SerializeField] private float moveSpeed = 3f;
        [Tooltip("Min distance to stop before reaching player (attack range")]
        [SerializeField] private float stoppingDistance = 1f;

        [Header("UI settings")]
        [SerializeField] private GameObject healthBarPrefab;

        [Header("Debug settings")]
        [SerializeField] private bool showGizmos = true;

        private Rigidbody _rb;
        private Transform _player;
        private Transform _currentTarget;
        private GameObject _prioritizeTargetInstance;
        private ParticleSystem _particles;
        private HealthBar _healthBar;
        private bool _isPlayerDetected;
        private float _currentHealth;

        public bool IsPlayerDetected => _isPlayerDetected;

        void Start() {
            var playerObj = GameObject.FindGameObjectWithTag("Player");         // Auto-find player
            if (playerObj) _player = playerObj.transform;

            FindTargetInstance();                                               // Check if prioritized target is around

            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = true;
            _rb.isKinematic = false;
            
            _currentHealth = maxHealth;                                         // Start full life

            if (healthBarPrefab) {                                              // Create health bar but hide it
                GameObject healthBarObj = Instantiate(healthBarPrefab, transform);
                _healthBar = healthBarObj.GetComponent<HealthBar>();
                _healthBar.Initialize(transform, maxHealth);
                _healthBar.SetHealth(_currentHealth);
                _healthBar.gameObject.SetActive(false);
            }
        }

        void FixedUpdate() {
            if (!_player) return;                                               // Player not found (shouldn't happen)

            DetectTarget();

            if (_currentTarget) MoveTowardsTarget();
            
            if (_currentHealth < maxHealth) DisplayHealthBar();
        }

        private void FindTargetInstance() {                                     // Find prioritize target in scene
            if (prioritizeTargetPrefab) {
                string prefabName = prioritizeTargetPrefab.name;

                GameObject foundInstance = GameObject.Find(prefabName);         // Try to find by name
                if (!foundInstance) {                                           // Try to find by type
                    GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                    foreach (var obj in allObjects)
                        if (obj.name.Contains(prefabName)) { foundInstance = obj; break; }
                }
                
                // if (GetComponent<SpiderAnimation>())                            // !!!
                //     print($"Found {foundInstance.name} {foundInstance.transform.position}!");

                if (foundInstance) {
                    _prioritizeTargetInstance = foundInstance;
                    _particles = foundInstance.GetComponentInChildren<ParticleSystem>();
                }
                // else Debug.LogWarning($"[BaseEnemy] No target '{prefabName}' found !");   // !!!
            }
        }

        private void DetectTarget() {                                           // Check if target/player is around
            _isPlayerDetected = false;
            _currentTarget = null;

            if (IsTargetInRange()) { _currentTarget = _prioritizeTargetInstance.transform; return; }

            Vector3 directionToPlayer = _player.position - transform.position;
            float distance = directionToPlayer.magnitude;

            if (distance > detectionRange) return;                              // Check distance
            if (Mathf.Abs(directionToPlayer.y) > detectionHeight) return;       // Check height

            float angle = Vector3.Angle(transform.forward, directionToPlayer);  // Check field of vision angle
            if (angle > fieldOfView / 2f) return;

            _isPlayerDetected = true;
            _currentTarget = _player;
        }

        private bool IsTargetInRange() {
            if (!_prioritizeTargetInstance) return false;

            bool isActive = !(_particles && !_particles.isPlaying);
            Vector3 direction = prioritizeTargetPrefab.transform.position - transform.position;
            float distance = direction.magnitude;

            return isActive && distance <= detectionRange && Mathf.Abs(direction.y) <= detectionHeight;
        }

        private void MoveTowardsTarget() {
            Vector3 dir = _currentTarget.position - transform.position;
            float distance = dir.magnitude;

            if (distance <= stoppingDistance) { AttackPlayer(); return; }       // If close enough from player, attack it

            dir.y = 0f;                                                         // Untouched : manage by gravity
            dir.Normalize();

            Quaternion targetRot = Quaternion.LookRotation(dir);
            _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRot, Time.fixedDeltaTime * 5f));

            float speed = prioritizeTargetPrefab && _currentTarget == prioritizeTargetPrefab.transform ?
                moveSpeed * 1.5f : moveSpeed;                                   // Move faster if prioritize target is detected
            Vector3 move = dir * (speed * Time.fixedDeltaTime);
            _rb.MovePosition(transform.position + move);
        }

        private void AttackPlayer() {
            // ReSharper disable once RedundantJumpStatement
            if (_currentTarget != _player) return;

            // PlayerStat script = _player.GetComponent<PlayerStat>();          // M Inflict damage to player
            // script.TakeDamage(damage);
            // L Add timer between each attack
        }

        private void DisplayHealthBar() {                                       // Show health bar above enemy
            if (_healthBar && !_healthBar.gameObject.activeSelf) _healthBar.gameObject.SetActive(true);
        }

        void OnDrawGizmosSelected() {
            if (!showGizmos) return;

            Gizmos.color = _isPlayerDetected ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);          // Detection range of enemy

            Vector3 leftBoundary = Quaternion.Euler(0, -fieldOfView / 2, 0) * transform.forward; // Field of vision
            Vector3 rightBoundary = Quaternion.Euler(0, fieldOfView / 2, 0) * transform.forward;

            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, leftBoundary * detectionRange);
            Gizmos.DrawRay(transform.position, rightBoundary * detectionRange);
        }

        public void TakeDamage(float receiveDamage) {
            _currentHealth -= receiveDamage;
            _currentHealth = Mathf.Clamp(_currentHealth, 0, maxHealth);         // Limit health between 0 and max hp
            if (_healthBar) _healthBar.SetHealth(_currentHealth);
            if (_currentHealth <= 0) Die();
        }

        private void Die() {                                                    // Disappear after a small time
            Destroy(gameObject, 0.5f);
        }
    }
}
