using UnityEngine;

namespace Enemies {
    public class ZombieAnimation : MonoBehaviour {
        [Header("References")]
        [SerializeField] private BaseEnemy baseEnemy;                           // Enemy script
        [SerializeField] private Transform leftShoulder;
        [SerializeField] private Transform rightShoulder;

        [Header("Settings")]
        [SerializeField] private float raiseAngle = 90f;                        // Max raise angle (in degrees)
        [SerializeField, Range(0.001f, 10f)] private float raiseDuration = 1f;  // Time of arm animation

        private Quaternion _leftBaseRot;
        private Quaternion _rightBaseRot;
        private float _currentLerp;
        private float _targetLerp;

        void Awake() {
            if (!baseEnemy) baseEnemy = GetComponent<BaseEnemy>();
            if (leftShoulder) _leftBaseRot = leftShoulder.localRotation;
            if (rightShoulder) _rightBaseRot = rightShoulder.localRotation;
        }

        void Update() {
            if (!baseEnemy) return;

            _targetLerp = baseEnemy.IsPlayerDetected ? 1f : 0f;        // Raise arms if player is detected
            float lerpSpeed = 1f / raiseDuration;
            _currentLerp = Mathf.MoveTowards(_currentLerp, _targetLerp, lerpSpeed * Time.deltaTime);

            ApplyArmRotation();
        }

        private void ApplyArmRotation() {                                       // Apply target rotation to arms
            float currentAngle = Mathf.Lerp(0f, raiseAngle, _currentLerp);
            Quaternion armRot = Quaternion.AngleAxis(currentAngle, Vector3.left);

            if (leftShoulder) leftShoulder.localRotation = _leftBaseRot * armRot;
            if (rightShoulder) rightShoulder.localRotation = _rightBaseRot * armRot;
        }
    }
}
