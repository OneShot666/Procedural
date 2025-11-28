using UnityEngine;

namespace Enemies {
    /// <summary> Main enemy behavior : detect and pursue player </summary>
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

        [Header("Idle wandering settings")]
        [Tooltip("If wander when player isn't detected")]
        [SerializeField] private bool enableWandering = true;
        [SerializeField] private bool stayInSpawnZone;
        [SerializeField] private float spawnZoneRadius = 8f;
        [SerializeField] private float wanderMoveTime = 1f;
        [Tooltip("Pause between moves")]
        [SerializeField] private float wanderWaitTime = 2.5f;

        [Header("Auto step climbing")]
        [SerializeField] private bool autoClimb = true;
        [SerializeField] private float stepHeight = 1f;
        [SerializeField] private float stepCheckDistance = 0.6f;
        [Tooltip("Ascension speed")]
        [SerializeField] private float stepSmooth = 5f;

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
        private Vector3 _spawnPoint;
        private Vector3 _wanderDirection;
        private bool _isPlayerDetected;
        private bool _isWandering;
        private float _currentHealth;
        private float _wanderTimer;
        private float _climbTimer; 

        public bool IsPlayerDetected => _isPlayerDetected;

        void Start() {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj) _player = playerObj.transform;

            _spawnPoint = transform.position;

            FindTargetInstance();

            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = true;
            _rb.isKinematic = false;
            
            _currentHealth = maxHealth;

            if (healthBarPrefab) {
                GameObject healthBarObj = Instantiate(healthBarPrefab, transform);
                _healthBar = healthBarObj.GetComponent<HealthBar>();
                _healthBar.Initialize(transform, maxHealth);
                _healthBar.SetHealth(_currentHealth);
                _healthBar.gameObject.SetActive(false);
            }
        }

        void FixedUpdate() {
            if (_climbTimer > 0) _climbTimer -= Time.fixedDeltaTime;            // Add time to climb timer

            if (!_player) return;

            DetectTarget();

            if (_currentTarget) MoveTowardsTarget();
            else IdleWandering();
            
            if (_currentHealth < maxHealth) DisplayHealthBar();
        }

        private void HandleStepClimb(Vector3 moveDir) {
            if (moveDir.sqrMagnitude < 0.001f) return;                          // Don't climb if not moving
            
            if (_climbTimer > 0) return;                                        // If last climb was too close in time
            // Check not too far from the ground
            if (!Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.2f)) return;

            Vector3 originLow = transform.position + Vector3.up * 0.1f + moveDir * 0.2f;
            Vector3 originHigh = transform.position + Vector3.up * stepHeight + moveDir * 0.2f;

            if (Physics.Raycast(originLow, moveDir, out _, stepCheckDistance)) {
                if (!Physics.Raycast(originHigh, moveDir, stepCheckDistance + 0.1f)) {
                    Vector3 velocity = _rb.linearVelocity;
                    
                    if (velocity.y < stepSmooth) {                              // If vertical speed isn't too high
                        velocity.y = stepSmooth; 
                        _rb.linearVelocity = velocity;
                        _climbTimer = 0.5f; // Pause between climbs
                    }
                }
            }
        }

        private void FindTargetInstance() {
            if (prioritizeTargetPrefab) {
                string prefabName = prioritizeTargetPrefab.name;

                GameObject foundInstance = GameObject.Find(prefabName);
                if (!foundInstance) {
                    GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                    foreach (var obj in allObjects)
                        if (obj.name.Contains(prefabName)) { foundInstance = obj; break; }
                }

                if (foundInstance) {
                    _prioritizeTargetInstance = foundInstance;
                    _particles = foundInstance.GetComponentInChildren<ParticleSystem>();
                }
            }
        }

        private void DetectTarget() {
            _isPlayerDetected = false;
            _currentTarget = null;

            if (IsTargetInRange()) { _currentTarget = _prioritizeTargetInstance.transform; return; }

            Vector3 directionToPlayer = _player.position - transform.position;
            float distance = directionToPlayer.magnitude;

            if (distance > detectionRange) return;
            if (Mathf.Abs(directionToPlayer.y) > detectionHeight) return;

            float angle = Vector3.Angle(transform.forward, directionToPlayer);
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

            if (distance <= stoppingDistance) { AttackPlayer(); return; }

            dir.y = 0f;
            dir.Normalize();

            if (autoClimb) HandleStepClimb(dir);
            Quaternion targetRot = Quaternion.LookRotation(dir);
            _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRot, Time.fixedDeltaTime * 5f));

            float speed = prioritizeTargetPrefab && _currentTarget == prioritizeTargetPrefab.transform ?
                moveSpeed * 1.5f : moveSpeed;
            Vector3 move = dir * (speed * Time.fixedDeltaTime);
            _rb.MovePosition(transform.position + move);
        }

        private void DisplayHealthBar() {
            if (_healthBar && !_healthBar.gameObject.activeSelf) _healthBar.gameObject.SetActive(true);
        }

        private void IdleWandering() {
            if (!enableWandering || _currentTarget) { _isWandering = false; return; }

            _wanderTimer -= Time.fixedDeltaTime;

            if (_wanderTimer <= 0f) {
                _isWandering = !_isWandering;

                if (_isWandering) {
                    Vector2 randomDir = Random.insideUnitCircle.normalized;
                    _wanderDirection = new Vector3(randomDir.x, 0, randomDir.y);

                    if (stayInSpawnZone) {
                        Vector3 futurePos = transform.position + _wanderDirection * (moveSpeed * wanderMoveTime);

                        if (Vector3.Distance(_spawnPoint, futurePos) > spawnZoneRadius) {
                            _wanderDirection = (_spawnPoint - transform.position).normalized;
                        }
                    }

                    _wanderTimer = wanderMoveTime;
                } else _wanderTimer = wanderWaitTime;
            }

            if (!_isWandering) return;

            Vector3 move = _wanderDirection * (moveSpeed * 0.5f * Time.fixedDeltaTime);
            _rb.MovePosition(transform.position + move);

            if (_wanderDirection != Vector3.zero) {
                Quaternion rot = Quaternion.LookRotation(_wanderDirection);
                _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, rot, Time.fixedDeltaTime * 2f));
            }
        }

        private void AttackPlayer() {
            // ReSharper disable once RedundantJumpStatement
            if (_currentTarget != _player) return;
            // L Add weapons and all health/damage system
        }

        public void TakeDamage(float receiveDamage) {
            _currentHealth -= receiveDamage;
            _currentHealth = Mathf.Clamp(_currentHealth, 0, maxHealth);
            if (_healthBar) _healthBar.SetHealth(_currentHealth);
            if (_currentHealth <= 0) Die();
        }

        private void Die() {
            Destroy(gameObject, 0.5f);
        }

        void OnDrawGizmosSelected() {
            if (!showGizmos) return;

            Gizmos.color = _isPlayerDetected ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            Vector3 leftBoundary = Quaternion.Euler(0, -fieldOfView / 2, 0) * transform.forward;
            Vector3 rightBoundary = Quaternion.Euler(0, fieldOfView / 2, 0) * transform.forward;

            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, leftBoundary * detectionRange);
            Gizmos.DrawRay(transform.position, rightBoundary * detectionRange);
        }
    }
}
