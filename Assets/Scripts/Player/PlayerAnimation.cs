using UnityEngine;

namespace Player {
    public class PlayerAnimation : MonoBehaviour {
        [Header("References")]
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;
        [SerializeField] private PlayerMovement playerMovement;

        [Header("Walking Arms Swing Settings")]
        [SerializeField] private float swingSpeed = 2f;
        [SerializeField] private float swingMaxAngle = 15f;
        [SerializeField] private float sprintMultiplier = 1.8f;
        [SerializeField, Range(0, 1)] private float verticalPrecision = 1;

        [Header("Weapon Equipped Arm Movement")]
        [SerializeField] private float armLiftSpeed = 5f;
        [SerializeField] private float armSwaySpeed = 5f;
        [SerializeField] private float armSwayAmplitude = 0.5f;
        [SerializeField, Range(3, 1000)] private int sightRange = 1000;

        private Quaternion _initLeftRot;
        private Quaternion _initRightRot;

        private bool _isArmed;
        // private readonly float _worldSize = 1000f;
        private float _swingTimer;

        void Start() {
            if (leftArm) _initLeftRot = leftArm.localRotation;
            if (rightArm) _initRightRot = rightArm.localRotation;
        }

        void Update() {
            if (!playerMovement) return;

            LeftArmAnimation();
            RightArmAnimation();
        }

        public void SetIsArmed(bool armed) {
            _isArmed = armed;
        }

        private void LeftArmAnimation() {
            float moveSpeed = playerMovement.CurrentSpeed;
            bool isMoving = moveSpeed > 0.1f && playerMovement.IsGrounded;

            if (isMoving) {
                _swingTimer += Time.deltaTime * swingSpeed * (moveSpeed / playerMovement.BaseSpeed);

                float swingAmount = swingMaxAngle * (moveSpeed / playerMovement.BaseSpeed);    // Get amplitude based on speed
                swingAmount = Mathf.Clamp(swingAmount, 0f, swingMaxAngle * sprintMultiplier);

                float leftRotation = Mathf.Sin(_swingTimer) * swingAmount;      // Swing arm
                leftArm.localRotation = Quaternion.Euler(leftRotation, 0f, leftRotation);
            } else {                                                            // Slowly go back to neutral position
                leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, _initLeftRot, Time.deltaTime * armLiftSpeed);
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
                _swingTimer += Time.deltaTime * swingSpeed * (moveSpeed / playerMovement.BaseSpeed);

                float swingAmount = swingMaxAngle * (moveSpeed / playerMovement.BaseSpeed);    // Get amplitude based on speed
                swingAmount = Mathf.Clamp(swingAmount, 0f, swingMaxAngle * sprintMultiplier);

                float rightRotation = Mathf.Sin(_swingTimer + Mathf.PI) * swingAmount;  // Swing arm (opposed to left)
                rightArm.localRotation = Quaternion.Euler(rightRotation, 0f, 0f);
            } else {                                                            // Slowly go back to neutral position
                rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, _initRightRot, Time.deltaTime * armLiftSpeed);
            }
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
