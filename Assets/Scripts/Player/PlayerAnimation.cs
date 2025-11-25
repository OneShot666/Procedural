using UnityEngine;

namespace Player {
    public class PlayerAnimation : MonoBehaviour {
        [Header("References")]
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg;

        [Header("Arms Swing Settings")]
        [SerializeField, Range(0, 1)] private float verticalPrecision = 1;
        [SerializeField] private float armSwingSpeed = 2f;
        [SerializeField] private float armSwingMaxAngle = 15f;
        [SerializeField] private float armSprintMultiplier = 1.8f;

        [Header("Leg Swing Settings")]
        [SerializeField] private float legSwingSpeed = 2f;
        [SerializeField] private float legSwingMaxAngle = 15f;
        [SerializeField] private float legSprintMultiplier = 1.8f;

        [Header("Weapon Equipped Arm Movement")]
        [SerializeField] private float armLiftSpeed = 5f;
        [SerializeField] private float armSwaySpeed = 5f;
        [SerializeField] private float armSwayAmplitude = 0.5f;
        [SerializeField, Range(3, 1000)] private int sightRange = 1000;

        private Quaternion _initLeftArmRot;
        private Quaternion _initRightArmRot;
        private Quaternion _initLeftLegRot;
        private Quaternion _initRightLegRot;

        private bool _isArmed;
        private float _swingTimer;

        void Start() {
            if (leftArm) _initLeftArmRot = leftArm.localRotation;
            if (rightArm) _initRightArmRot = rightArm.localRotation;
            if (leftLeg) _initLeftLegRot = leftLeg.localRotation;
            if (rightLeg) _initRightLegRot = rightLeg.localRotation;
        }

        void Update() {
            if (!playerMovement) return;

            LeftArmAnimation();
            RightArmAnimation();

            LeftLegAnimation();
            RightLegAnimation();
        }

        public void SetIsArmed(bool armed) => _isArmed = armed;

        private void LeftArmAnimation() {
            float moveSpeed = playerMovement.CurrentSpeed;
            bool isMoving = moveSpeed > 0.1f && playerMovement.IsGrounded;

            if (isMoving) {
                _swingTimer += Time.deltaTime * armSwingSpeed * (moveSpeed / playerMovement.BaseSpeed);

                float swingAmount = armSwingMaxAngle * (moveSpeed / playerMovement.BaseSpeed);    // Get amplitude based on speed
                swingAmount = Mathf.Clamp(swingAmount, 0f, armSwingMaxAngle * armSprintMultiplier);

                float leftRotation = Mathf.Sin(_swingTimer) * swingAmount;      // Swing arm
                leftArm.localRotation = Quaternion.Euler(leftRotation, 0f, leftRotation);
            } else {                                                            // Slowly go back to neutral position
                leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, _initLeftArmRot, Time.deltaTime * armLiftSpeed);
            }
        }
        
        private void RightArmAnimation() {
            float moveSpeed = playerMovement.CurrentSpeed;
            bool isMoving = moveSpeed > 0.1f && playerMovement.IsGrounded;

            if (_isArmed) {
                float pitch = playerMovement.PlayerCamera.localEulerAngles.x;   // Orientation also depends on player's look
                if (pitch > 180f) pitch -= 360f;

                Vector3 dynamicOffset = GetArmOffset();                         // Upgrade precision
                float y = -90f + pitch * verticalPrecision + dynamicOffset.y;
                Quaternion targetRotation = Quaternion.Euler(y, dynamicOffset.x, dynamicOffset.z);  // Raise arm

                rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, targetRotation, Time.deltaTime * armLiftSpeed);

                if (isMoving) {                                                 // Arm move a bit when walking
                    float sway = Mathf.Sin(Time.time * armSwaySpeed) * armSwayAmplitude;
                    rightArm.localRotation *= Quaternion.Euler(sway, 0f, 0f);
                }
            } else if (isMoving) {
                _swingTimer += Time.deltaTime * armSwingSpeed * (moveSpeed / playerMovement.BaseSpeed);

                float swingAmount = armSwingMaxAngle * (moveSpeed / playerMovement.BaseSpeed);    // Get amplitude based on speed
                swingAmount = Mathf.Clamp(swingAmount, 0f, armSwingMaxAngle * armSprintMultiplier);

                float rightRotation = Mathf.Sin(_swingTimer + Mathf.PI) * swingAmount;  // Swing arm (opposed to left)
                rightArm.localRotation = Quaternion.Euler(rightRotation, 0f, 0f);
            } else {                                                            // Slowly go back to neutral position
                rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, _initRightArmRot, Time.deltaTime * armLiftSpeed);
            }
        }

        private void LeftLegAnimation() {
            float moveSpeed = playerMovement.CurrentSpeed;
            bool isMoving = moveSpeed > 0.1f && playerMovement.IsGrounded;

            if (isMoving) {
                float swingAmount = legSwingMaxAngle * (moveSpeed / playerMovement.BaseSpeed);
                swingAmount = Mathf.Clamp(swingAmount, 0f, legSwingMaxAngle * legSprintMultiplier);

                float leftRotation = Mathf.Sin(_swingTimer + Mathf.PI) * swingAmount; 
                leftLeg.localRotation = Quaternion.Euler(leftRotation, 0f, 0f);
            } else leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation,
                _initLeftLegRot, Time.deltaTime * legSwingSpeed);
        }

        private void RightLegAnimation() {
            float moveSpeed = playerMovement.CurrentSpeed;
            bool isMoving = moveSpeed > 0.1f && playerMovement.IsGrounded;

            if (isMoving) {
                float swingAmount = legSwingMaxAngle * (moveSpeed / playerMovement.BaseSpeed);
                swingAmount = Mathf.Clamp(swingAmount, 0f, legSwingMaxAngle * legSprintMultiplier);

                float rightRotation = Mathf.Sin(_swingTimer) * swingAmount;
                rightLeg.localRotation = Quaternion.Euler(rightRotation, 0f, 0f);
            } else rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation,
                _initRightLegRot, Time.deltaTime * legSwingSpeed);
        }

        private Vector3 GetArmOffset() {                                        // Center player's aim
            Vector3 lookDir = playerMovement.PlayerCamera.forward;
            Vector3 offset = Vector3.zero;

            if (Physics.Raycast(playerMovement.PlayerCamera.position, lookDir, out RaycastHit hit, sightRange)) {
                float t = Mathf.InverseLerp(sightRange, 3f, hit.distance);      // The closer the object the player look at...
                offset = Vector3.Lerp(offset, new Vector3(0, -5f, -5f), t);     // ... the greater the offset to the center
            }

            return offset;
        }
    }
}
