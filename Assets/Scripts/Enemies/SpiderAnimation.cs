using System.Collections.Generic;
using UnityEngine;

namespace Enemies {
    public class SpiderAnimation : MonoBehaviour {
        [Header("Animation Settings")]
        [Tooltip("If legs walks in a synchronized way")]
        [SerializeField] private bool isSynchronedWalking = true;
        [Tooltip("List of legs to animate")]
        [SerializeField] private List<Transform> legs = new();
        [Tooltip("Angle of movement (in degree)")]
        [SerializeField] private float legAngle = 15f;
        [Tooltip("Cycle duration (in second)")]
        [SerializeField] private float legCycleDuration = 1.5f;

        [Header("Auto-Detection Settings")]
        [Tooltip("Minimum speed to trigger walking (ignores vertical movement)")]
        [SerializeField, Range(0.01f, 0.5f)] private float moveThreshold = 0.1f;
        [Tooltip("Minimum rotation speed to trigger walking")]
        [SerializeField] private float rotationThreshold = 0.5f;
        [Tooltip("Time to wait before stopping animation (prevents jitter)")]
        [SerializeField] private float stopBufferTime = 0.15f;
        [Tooltip("Smooth return to idle")]
        [SerializeField] private float stopSmoothing = 5f;

        private List<Quaternion> _initialRotations;
        
        private Vector3 _lastPosition;
        private Quaternion _lastRotation;
        
        private float _animationTimer;
        private float _stopBufferTimer;                                         // Timer to delay animation stop
        private bool _isMoving;

        void Start() {
            _initialRotations = new List<Quaternion>();
            foreach (var leg in legs) _initialRotations.Add(leg ? leg.localRotation : Quaternion.identity);

            _lastPosition = transform.position;
            _lastRotation = transform.rotation;
        }

        void Update() {
            if (legs.Count == 0) return;

            CheckMovement();

            if (_isMoving) _animationTimer += Time.deltaTime;                   // Manage animation timer

            ApplyAnimation();

            _lastPosition = transform.position;                                 // Update position
            _lastRotation = transform.rotation;
        }

        private void CheckMovement() {
            Vector3 currentPosFlat = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 lastPosFlat = new Vector3(_lastPosition.x, 0, _lastPosition.z); // Calculate hor distance
            
            float distanceMoved = Vector3.Distance(currentPosFlat, lastPosFlat);
            float speed = distanceMoved / Time.deltaTime;                       // Speed (units per second)
            float angleChanged = Quaternion.Angle(transform.rotation, _lastRotation);   // Calculate rotation
            bool currentlyMoving = speed > moveThreshold || angleChanged > rotationThreshold;   // Check if move

            if (currentlyMoving) {
                _stopBufferTimer = stopBufferTime;                              // Fill buffer
                _isMoving = true;
            } else {
                _stopBufferTimer -= Time.deltaTime;
                if (_stopBufferTimer <= 0) _isMoving = false;                   // Only stop if buffer empty
            }
        }

        private void ApplyAnimation() {
            for (int i = 0; i < legs.Count; i++) {
                if (!legs[i]) continue;

                if (_isMoving) {
                    float phaseOffset = isSynchronedWalking ? i / (float)legs.Count * Mathf.PI * 2 : 
                        i * 99f % (Mathf.PI * 2);                               // Walk
                    float angle = Mathf.Sin((_animationTimer / legCycleDuration) * Mathf.PI * 2 + phaseOffset) * legAngle;
                    Quaternion targetRotation = _initialRotations[i] * Quaternion.Euler(angle, angle * 0.5f, 0f);

                    legs[i].localRotation = Quaternion.Slerp(legs[i].localRotation, targetRotation, Time.deltaTime * 15f);
                } else legs[i].localRotation = Quaternion.Slerp(legs[i].localRotation, 
                    _initialRotations[i], Time.deltaTime * stopSmoothing);  // Slow stop
            }
        }
    }
}
