using UnityEngine;

namespace Player {
    public class PlayerAnimation : MonoBehaviour {
        [Header("References")]
        [SerializeField] private PlayerMovement playerMovement;
        
        [Header("Body Parts")]
        [SerializeField] private Transform head;
        [SerializeField] private Transform floatingPrism;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg;

        [Header("Prism Settings")]
        [SerializeField] private float prismRotationSpeed = 30f;

        [Header("Arms Swing Settings")]
        [SerializeField, Range(0, 1)] private float verticalPrecision = 1;
        [SerializeField] private float armSwingSpeed = 5f;
        [SerializeField] private float armSwingMaxAngle = 20f;
        [SerializeField] private float armSprintMultiplier = 1.5f;

        [Header("Leg Swing Settings")]
        [SerializeField] private float legSwingSpeed = 5f;
        [SerializeField] private float legSwingMaxAngle = 20f;
        [SerializeField] private float legSprintMultiplier = 1.5f;

        [Header("Weapon Equipped Arm Movement")]
        [SerializeField] private float armLiftSpeed = 8f;
        [SerializeField] private float armSwaySpeed = 5f;
        [SerializeField] private float armSwayAmplitude = 0.5f;
        [SerializeField, Range(3, 1000)] private int sightRange = 1000;

        private Quaternion _initHeadRot;
        private Quaternion _initLeftArmRot;
        private Quaternion _initRightArmRot;
        private Quaternion _initLeftLegRot;
        private Quaternion _initRightLegRot;

        private bool _isMoving;
        private bool _isArmed;
        private float _swingTimer;
        private float _smoothedSpeed; 

        void Start() {
            if (head) _initHeadRot = head.localRotation;
            if (leftArm) _initLeftArmRot = leftArm.localRotation;
            if (rightArm) _initRightArmRot = rightArm.localRotation;
            if (leftLeg) _initLeftLegRot = leftLeg.localRotation;
            if (rightLeg) _initRightLegRot = rightLeg.localRotation;
        }

        void Update() {
            SmoothAnimation();

            HandleHeadRotation();
            HandlePrismRotation();

            LeftArmAnimation();
            RightArmAnimation();

            LeftLegAnimation();
            RightLegAnimation();
        }

        public void SetIsArmed(bool armed) => _isArmed = armed;

        private void SmoothAnimation() {
            if (!playerMovement) return;

            _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, playerMovement.CurrentSpeed, Time.deltaTime * 10f);
            _isMoving = _smoothedSpeed > 0.1f && playerMovement.IsGrounded;     // Smooth player's speed

            if (_isMoving) {                                                    // Incremente timer if move
                float speedRatio = _smoothedSpeed / playerMovement.BaseSpeed;
                _swingTimer += Time.deltaTime * armSwingSpeed * speedRatio;
            }
        }

        private void HandleHeadRotation() {
            if (!head || ! playerMovement) return;

            float pitch = playerMovement.PlayerCamera.localEulerAngles.x;
            if (pitch > 180f) pitch -= 360f;

            head.localRotation = _initHeadRot * Quaternion.Euler(pitch, 0f, 0f);    // Keep init rotation and apply pitch
        }

        private void HandlePrismRotation() {
            if (!floatingPrism) return;
            floatingPrism.Rotate(Vector3.up * (prismRotationSpeed * Time.deltaTime));
        }

        private void LeftArmAnimation() {
            if (!leftArm || !playerMovement) return;

            if (_isMoving) {
                float speedRatio = _smoothedSpeed / playerMovement.BaseSpeed;
                float swingAmount = armSwingMaxAngle * speedRatio;
                swingAmount = Mathf.Clamp(swingAmount, 0f, armSwingMaxAngle * armSprintMultiplier);

                float angle = Mathf.Sin(_swingTimer) * swingAmount;
                leftArm.localRotation = _initLeftArmRot * Quaternion.Euler(angle, 0f, 0f);  // Soft rotation
            } else leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, _initLeftArmRot, Time.deltaTime * armLiftSpeed);
        }
        
        private void RightArmAnimation() {
            if (!rightArm || !playerMovement) return;

            if (_isArmed) {
                float pitch = playerMovement.PlayerCamera.localEulerAngles.x;
                if (pitch > 180f) pitch -= 360f;

                Vector3 dynamicOffset = GetArmOffset();
                float y = -90f + pitch * verticalPrecision + dynamicOffset.y;
                Quaternion targetRotation = Quaternion.Euler(y, dynamicOffset.x, dynamicOffset.z);

                if (_isMoving) {                                                // If moving, add swing
                    float sway = Mathf.Sin(Time.time * armSwaySpeed) * armSwayAmplitude;
                    targetRotation *= Quaternion.Euler(sway, 0f, 0f);
                }

                rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, targetRotation, Time.deltaTime * armLiftSpeed);
            } else if (_isMoving) {
                float speedRatio = _smoothedSpeed / playerMovement.BaseSpeed;
                float swingAmount = armSwingMaxAngle * speedRatio;
                swingAmount = Mathf.Clamp(swingAmount, 0f, armSwingMaxAngle * armSprintMultiplier);

                float angle = Mathf.Sin(_swingTimer + Mathf.PI) * swingAmount;
                rightArm.localRotation = _initRightArmRot * Quaternion.Euler(angle, 0f, 0f);    // Relative rotation
            } else rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, _initRightArmRot, Time.deltaTime * armLiftSpeed);
        }

        private void LeftLegAnimation() {
            if (!leftLeg || !playerMovement) return;

            if (_isMoving) {
                float speedRatio = _smoothedSpeed / playerMovement.BaseSpeed;
                float swingAmount = legSwingMaxAngle * speedRatio;
                swingAmount = Mathf.Clamp(swingAmount, 0f, legSwingMaxAngle * legSprintMultiplier);

                float angle = Mathf.Sin(_swingTimer + Mathf.PI) * swingAmount; 
                leftLeg.localRotation = _initLeftLegRot * Quaternion.Euler(angle, 0f, 0f);  // Relative rotation
            } else leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, _initLeftLegRot, Time.deltaTime * legSwingSpeed);
        }

        private void RightLegAnimation() {
            if (!rightLeg || !playerMovement) return;

            if (_isMoving) {
                float speedRatio = _smoothedSpeed / playerMovement.BaseSpeed;
                float swingAmount = legSwingMaxAngle * speedRatio;
                swingAmount = Mathf.Clamp(swingAmount, 0f, legSwingMaxAngle * legSprintMultiplier);

                float angle = Mathf.Sin(_swingTimer) * swingAmount;
                rightLeg.localRotation = _initRightLegRot * Quaternion.Euler(angle, 0f, 0f);    // Relative rotation
            } else rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, _initRightLegRot, Time.deltaTime * legSwingSpeed);
        }

        private Vector3 GetArmOffset() {
            Vector3 offset = Vector3.zero;
            if (!playerMovement) return offset;
            Vector3 lookDir = playerMovement.PlayerCamera.forward;

            if (Physics.Raycast(playerMovement.PlayerCamera.position, lookDir, out RaycastHit hit, sightRange)) {
                float t = Mathf.InverseLerp(sightRange, 3f, hit.distance);
                offset = Vector3.Lerp(offset, new Vector3(0, -5f, -5f), t);
            }
            return offset;
        }
    }
}
