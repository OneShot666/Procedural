using UnityEngine;

namespace Enemies {
    public class ZombieAnimation : MonoBehaviour {
        [Header("References")]
        [SerializeField] private BaseEnemy baseEnemy;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg;

        [Header("Raise Arms Settings")]
        [SerializeField] private float raiseAngle = 90f;
        [SerializeField, Range(0.01f, 10f)] private float raiseDuration = 1f;

        [Header("Walk Cycle Settings")]
        [SerializeField] private float swingSpeed = 2f;
        [SerializeField] private float swingMaxAngle = 25f;

        private Quaternion _leftArmBase;
        private Quaternion _rightArmBase;
        private Quaternion _leftLegBase;
        private Quaternion _rightLegBase;
        private Vector3 _lastPos;
        private float _currentLerp;
        private float _targetLerp;
        private float _swingTimer;


        void Awake() {
            if (!baseEnemy) baseEnemy = GetComponent<BaseEnemy>();
            GetComponent<Rigidbody>();

            if (leftArm) _leftArmBase = leftArm.localRotation;
            if (rightArm) _rightArmBase = rightArm.localRotation;
            if (leftLeg) _leftLegBase = leftLeg.localRotation;
            if (rightLeg) _rightLegBase = rightLeg.localRotation;

            _lastPos = transform.position;
        }

        void Update() {
            float speed = ComputeSpeed();
            AnimateArmRaise();
            AnimateWalk(speed);
        }

        private float ComputeSpeed() {                                          // Calculate speed based on pos
            Vector3 currentPos = transform.position;
            float speed = (currentPos - _lastPos).magnitude / Time.deltaTime;
            _lastPos = currentPos;
            return speed;
        }

        private void AnimateArmRaise() {                                        // When player is detected
            _targetLerp = baseEnemy.IsPlayerDetected ? 1f : 0f;
            float lerpSpeed = 1f / raiseDuration;

            _currentLerp = Mathf.MoveTowards(_currentLerp, _targetLerp, lerpSpeed * Time.deltaTime);
            float angle = Mathf.Lerp(0f, raiseAngle, _currentLerp);

            Quaternion rot = Quaternion.AngleAxis(angle, Vector3.left);

            if (leftArm)  leftArm.localRotation  = _leftArmBase  * rot;
            if (rightArm) rightArm.localRotation = _rightArmBase * rot;
        }

        private void AnimateWalk(float speed) {                                 // Animate arms and legs
            bool isMoving = speed > 0.1f;

            if (!isMoving) {                                                    // Slowly go back to default pos
                if (leftLeg) leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, _leftLegBase, Time.deltaTime * 5f);
                if (rightLeg) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, _rightLegBase, Time.deltaTime * 5f);

                if (!baseEnemy.IsPlayerDetected) {                              // Keep arms up if player detected
                    if (leftArm) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, _leftArmBase, Time.deltaTime * 5f);
                    if (rightArm) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, _rightArmBase, Time.deltaTime * 5f);
                }

                return;
            }

            _swingTimer += Time.deltaTime * swingSpeed * speed;                 // Walking animation
            float swing = Mathf.Clamp(speed * swingMaxAngle, 0, swingMaxAngle);

            float armLeftRot  = Mathf.Sin(_swingTimer) * swing;
            float armRightRot = Mathf.Sin(_swingTimer + Mathf.PI) * swing;
            float legLeftRot  = Mathf.Sin(_swingTimer + Mathf.PI) * swing;
            float legRightRot = Mathf.Sin(_swingTimer) * swing;

            // Legs swing
            if (leftLeg) leftLeg.localRotation = _leftLegBase * Quaternion.Euler(legLeftRot, 0, 0);
            if (rightLeg) rightLeg.localRotation = _rightLegBase * Quaternion.Euler(legRightRot, 0, 0);

            if (baseEnemy.IsPlayerDetected) return;                             // If arms raised, don't swing them

            // Arms swing
            if (leftArm) leftArm.localRotation = _leftArmBase * Quaternion.Euler(armLeftRot, 0, 0);
            if (rightArm) rightArm.localRotation = _rightArmBase * Quaternion.Euler(armRightRot, 0, 0);
        }
    }
}
