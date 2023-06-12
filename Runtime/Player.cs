using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace FirstPersonController
{
    public class Player : MonoBehaviour
    {
        public static Player Instance { get; private set; }

        public UnityEvent onBeforeMove;
        public UnityEvent<PlayerState> onStateChange;

        public UnityEvent<bool> onGroundStateChange;
        public UnityEvent<float, float> onGrounded;
        public UnityEvent onUngrounded;


        public UnityEvent<bool> onSlidingStateChange;
        public UnityEvent onStartedSliding;
        public UnityEvent onStoppedSliding;

        public PlayerInput playerInput;
        public PlayerSettings settings;

        [HideInInspector] public CharacterController controller;
        [HideInInspector] public Vector3 lastGroundedPosition;
        [HideInInspector] public float lastGroundedTime;

        public bool IsGrounded => groundObject != null;
        public bool IsSliding => isSliding;

        public float Height
        {
            get => controller.height;
            set => controller.height = value;
        }

        public PlayerSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        internal float movementSpeedMultiplier;

        PlayerState currentState = PlayerState.Walking;

        [HideInInspector] public Vector3 velocity;
        Vector2 look;
        
        // Used to fix unity's delta weirdness
        Vector2 previousLookDelta;
        float previousLookUpdateTime;
        float timeSinceCursorLock;

        (Vector3, Quaternion) initialPositionAndRotation;

        // Used for smooth animations between states
        internal float movementFactor;

        internal bool wasGroundedLastFrame;

        internal bool wasSlidingLastFrame;
        internal bool isSliding;

        internal Vector3 initialCameraPosition;

        internal GameObject rigidbodyObject;
        internal Rigidbody rb;
        internal CapsuleCollider rigidbodyCollider;

        internal GameObject cameraHelper;
        internal GameObject cameraEffectsHelper;
        internal GameObject cameraObject;

        // Ground-related variables
        internal GameObject groundObject;
        internal Vector3 groundNormal;
        internal Rigidbody groundObjectRigidbody;
        internal Material groundObjectMaterial;
        internal Vector3 groundVelocity;
        float groundAngularVelocityY, lookOffset = 0f;

        // Ceiling-related variables
        internal GameObject ceilingObject;

        InputAction moveAction;
        InputAction lookAction;
        InputAction jumpAction;
        InputAction crouchAction;

        internal PlayerInput playerInputInstance;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            controller = GetComponent<CharacterController>();

            playerInputInstance = Instantiate(playerInput);

            moveAction = playerInputInstance.actions["move"];
            lookAction = playerInputInstance.actions["look"];
            jumpAction = playerInputInstance.actions["jump"];
            crouchAction = playerInputInstance.actions["crouch"];

#if USE_CINEMACHINE
            cameraObject = GetComponentInChildren<Cinemachine.CinemachineVirtualCamera>().gameObject;
#else
            cameraObject = GetComponentInChildren<Camera>().gameObject;
#endif

            initialCameraPosition = cameraObject.transform.localPosition;

            // Parent a new camera effects helper to this object
            cameraEffectsHelper = new GameObject();
            cameraEffectsHelper.name = "_CameraEffectsHelper_";
            cameraEffectsHelper.layer = gameObject.layer;
            cameraEffectsHelper.transform.SetParent(transform);
            cameraEffectsHelper.transform.localPosition = initialCameraPosition;

            cameraHelper = new GameObject();
            cameraHelper.name = "_CameraHelper_";
            cameraHelper.layer = gameObject.layer;
            cameraHelper.transform.SetParent(cameraEffectsHelper.transform, false);

            var interpolationComponent = cameraObject.AddComponent<TransformInterpolation>();
            interpolationComponent.target = cameraHelper.transform;

            cameraObject.transform.SetParent(cameraHelper.transform, false);

            // Add a rigidbody to our controller to push objects around
            SetupRigidbody();
        }

        void Start()
        {
            initialPositionAndRotation = (transform.position, transform.rotation);
            look = new Vector2(transform.eulerAngles.y, 0);
            lastGroundedPosition = transform.position;
            lastGroundedTime = Time.time;

            if (settings.lockCursorOnStart)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public void SetupRigidbody()
        {
            // Rigid body mount
            var rigidbodyMountObject = new GameObject();
            rigidbodyMountObject.name = "_RigidbodyMountObject_";
            rigidbodyMountObject.layer = gameObject.layer;
            rigidbodyMountObject.transform.SetParent(transform, false);

            var rbMount = rigidbodyMountObject.AddComponent<Rigidbody>();
            rbMount.isKinematic = true;

            // Rigid body
            rigidbodyObject = new GameObject();
            rigidbodyObject.name = "_RigidbodyObject_";
            rigidbodyObject.layer = gameObject.layer;
            rigidbodyObject.transform.SetParent(transform, false);

            // "rigidbody" would shadow
            rb = rigidbodyObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.freezeRotation = true;

            rigidbodyCollider = rigidbodyObject.AddComponent<CapsuleCollider>();
            rigidbodyCollider.height = controller.height;
            rigidbodyCollider.radius = controller.radius + settings.rigidbodyColliderMargin;

            var events = rigidbodyObject.AddComponent<CollisionEvents>();

            events.onCollisionEnter += (Collision collision) =>
            {
                // Ignore anything that doesn't have a rigidbody component
                var body = collision.collider.attachedRigidbody;
                if (body == null && !Physics.GetIgnoreCollision(collision.collider, rigidbodyCollider))
                {
                    Physics.IgnoreCollision(collision.collider, rigidbodyCollider);
                }
            };

            events.onCollisionStay += (Collision collision) =>
            {
                var body = collision.collider.attachedRigidbody;
                if (body != null)
                {
                    // Push player out of collisions with rigid bodies
                    // to avoid the bug when jumping near rigid bodies
                    var vel = Vector3.ClampMagnitude(collision.impulse * 0.001f, .1f);
                    velocity += vel;
                }
            };

            var joint = rigidbodyObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = rbMount;
            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Limited;

            var limit = new SoftJointLimit();
            limit.limit = settings.rigidbodyColliderMargin;
            joint.linearLimit = limit;

            var drive = new JointDrive();
            drive.positionSpring = settings.rigidbodyPositionSpring;
            drive.maximumForce = Mathf.Infinity;
            joint.xDrive = drive;
            joint.zDrive = drive;

            Physics.IgnoreCollision(rigidbodyCollider, controller);
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            transform.position = position;
            Physics.SyncTransforms();
            look.x = rotation.eulerAngles.y;
            look.y = rotation.eulerAngles.z;
            velocity = Vector3.zero;
        }

        public void Launch(Vector3 launchVelocity, bool overrideHorizontalVelocity = false, bool overrideVerticalVelocity = false)
        {
            if (overrideHorizontalVelocity)
            {
                velocity.x = launchVelocity.x;
                velocity.z = launchVelocity.z;
            }
            else
            {
                velocity.x += launchVelocity.x;
                velocity.z += launchVelocity.z;
            }

            if (overrideVerticalVelocity)
            {
                velocity.y = launchVelocity.y;
            }
            else
            {
                velocity.y += launchVelocity.y;
            }
        }

        public void AddForce(Vector3 force)
        {
            velocity += force * Time.deltaTime;
        }

        void Update()
        {
            currentState.Update(this);
            UpdateTimeSinceCursorLock();
        }

        void UpdateTimeSinceCursorLock()
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                timeSinceCursorLock += Time.unscaledDeltaTime;
            }
            else
            {
                timeSinceCursorLock = 0;
            }
        }

        void FixedUpdate()
        {
            // Reset movement speed multiplier before applying new values
            movementSpeedMultiplier = 1f;

            // Reset camera effects transform before applying new values
            cameraEffectsHelper.transform.localPosition = initialCameraPosition;
            cameraEffectsHelper.transform.localEulerAngles = Vector3.zero;

            // Execute current state logic
            currentState.FixedUpdate(this);

            // Interpolate movement factor
            var moving = controller.velocity.sqrMagnitude > .1f;
            movementFactor = Mathf.Lerp
            (
                movementFactor,
                moving ? 1 : 0,
                Time.deltaTime * settings.movementFactorInterpolationSpeed
            );
        }

        public void SetState(PlayerState state)
        {
            if (currentState != null)
            {
                currentState.Exit(this);
            }
            currentState = state;
            state.Enter(this);
            onStateChange.Invoke(state);
        }

        public PlayerState GetState() => currentState;

        public void CheckBounds()
        {
            if (transform.position.y < settings.worldBottomBoundary)
            {
                var (position, rotation) = initialPositionAndRotation;
                Teleport(position, rotation);
            }
        }

        public void UpdateGround()
        {
            // Raycast further if the platform is moving down and we're not trying to jump to stick to it:
            var raycastDistance = groundVelocity.y < 0 && velocity.y <= 0
                ? settings.platformRaycastDistance
                : settings.groundRaycastDistance;

            groundObject = null;
            groundObjectRigidbody = null;
            groundObjectMaterial = null;

            var sphereCastVerticalOffset = controller.height / 2 - controller.radius;
            var castOrigin = transform.position - new Vector3(0, sphereCastVerticalOffset, 0);

            if (Physics.SphereCast(castOrigin, controller.radius - .001f, Vector3.down,
                out var hit, raycastDistance, ~LayerMask.GetMask("Player"), QueryTriggerInteraction.Ignore))
            {
                groundObject = hit.collider.gameObject;
                groundNormal = hit.normal;
                groundObjectRigidbody = hit.collider.attachedRigidbody;
                groundObjectMaterial = groundObject.GetComponent<Renderer>()?.sharedMaterial;

                // Sliding:
                var isWalkable = settings.walkableGroundMask == (settings.walkableGroundMask | (1 << groundObject.layer));
                if (!isWalkable || Vector3.Angle(Vector3.up, hit.normal) > controller.slopeLimit)
                {
                    var normal = hit.normal;
                    var yInverse = 1f - normal.y;
                    velocity.x += yInverse * normal.x * settings.slideSpeedMultiplier;
                    velocity.z += yInverse * normal.z * settings.slideSpeedMultiplier;
                    isSliding = true;
                }
                else
                {
                    isSliding = false;
                }
            }
            else
            {
                isSliding = false;
            }

            // Sliding state change detection
            if (wasSlidingLastFrame != isSliding)
            {
                wasSlidingLastFrame = isSliding;
                onSlidingStateChange.Invoke(IsSliding);
                if (isSliding)
                {
                    onStartedSliding.Invoke();
                }
                else
                {
                    onStoppedSliding.Invoke();
                }
            }

            // Move the controller by ground velocity if the ground is kinematic
            // to make moving platforms possible
            if (groundObjectRigidbody != null && groundObjectRigidbody.isKinematic)
            {
                groundVelocity = groundObjectRigidbody.GetPointVelocity(castOrigin);
                groundAngularVelocityY = groundObjectRigidbody.angularVelocity.y;
                lookOffset += groundAngularVelocityY * Mathf.Rad2Deg * Time.deltaTime;
                var groundVelocityYClamped = groundVelocity;
                if (groundVelocityYClamped.y > 0)
                {
                    // Don't move up on elevators, collision resolution will do the trick
                    groundVelocityYClamped.y = 0;
                }
                // controller.Move(groundVelocityYClamped * Time.deltaTime);
                controller.Move(groundVelocity * Time.deltaTime);
            }

            // Ground state change detection
            if (wasGroundedLastFrame != IsGrounded)
            {
                onGroundStateChange.Invoke(IsGrounded);
                wasGroundedLastFrame = IsGrounded;
                if (IsGrounded)
                {
                    var fallDistance = Vector3.Distance(transform.position, (Vector3)lastGroundedPosition);
                    var fallTime = Time.time - lastGroundedTime;

                    onGrounded.Invoke(fallDistance, fallTime);
                }
                else
                {
                    lastGroundedPosition = transform.position;
                    lastGroundedTime = Time.time;
                    onUngrounded.Invoke();
                    velocity += groundVelocity;
                    groundVelocity = Vector3.zero;
                    groundAngularVelocityY = 0;
                }
            }
        }

        // This makes sure the player doesn't get stuck in the ceiling
        public void UpdateCeiling()
        {
            ceilingObject = null;

            var verticalOffset = controller.height / 2 - controller.radius;
            var castOrigin = transform.position + new Vector3(0, verticalOffset, 0);
            if (Physics.SphereCast(castOrigin, controller.radius - .001f, Vector3.up, out RaycastHit hit, 0.2f))
            {
                ceilingObject = hit.collider.gameObject;
                velocity.y -= settings.ceilingUnstickForce * Time.deltaTime;
            }
        }

        public void UpdateGravity()
        {
            if (controller.isGrounded)
            {
                velocity.y = -settings.stickToGroundVelocity;
            }
            else
            {
                var weight = settings.gravity * settings.mass;
                AddForce(weight);
            }
        }

        public bool IsVerticalMovementState(PlayerState state)
        {
            return state == PlayerState.Flying
                || state == PlayerState.Swimming
                || state == PlayerState.Climbing;
        }

        public Vector3 GetMovementInput(float speed, bool horizontal = true)
        {
            var moveInput = moveAction.ReadValue<Vector2>();
            var input = new Vector3();
            var referenceTransform = horizontal ? transform : cameraHelper.transform;

            input += referenceTransform.forward * moveInput.y;
            input += referenceTransform.right * moveInput.x;

            if (IsVerticalMovementState(currentState) && !horizontal)
            {
                var jumpValue = jumpAction.ReadValue<float>();
                var crouchValue = crouchAction.ReadValue<float>();
                input += transform.up * jumpValue + transform.up * -crouchValue;
            }

            input = Vector3.ClampMagnitude(input, 1f);
            input *= speed * movementSpeedMultiplier;

            var rbOffset = rigidbodyObject.transform.position - transform.position;
            if
            (
                Vector3.Dot(input, rbOffset) < 0
                && rbOffset.magnitude > settings.rigidbodyColliderMargin * .2f
            )
            {
                return Vector3.ProjectOnPlane(input, rbOffset);
            }

            return input;
        }

        public float GetHorizontalLookSensitivity()
        {
            return playerInputInstance.currentControlScheme == "Keyboard&Mouse"
                ? settings.lookSensitivity
                : settings.gamepadHorizontalLookSensitivity;
        }

        public float GetVerticalLookSensitivity()
        {
            return playerInputInstance.currentControlScheme == "Keyboard&Mouse"
                ? settings.lookSensitivity
                : settings.gamepadVerticalLookSensitivity;
        }

        public Vector2 GetLookDelta()
        {
            var delta = lookAction.ReadValue<Vector2>() * 0.05f;
            return new Vector2(
                delta.x * GetHorizontalLookSensitivity(),
                delta.y * GetVerticalLookSensitivity() * (settings.invertLookY ? -1 : 1)
            );

        }

        public void UpdateLook()
        {
            // Skip input if recently locked to avoid a spike
            if (Cursor.lockState != CursorLockMode.Locked || timeSinceCursorLock < .1f)
                return;

            // Skip the first non-zero delta after no input
            // because it's a spike after the cursor is locked
            var timeSinceLastInput = Time.realtimeSinceStartup - previousLookUpdateTime;
            if (timeSinceLastInput > .5f)
            {
                previousLookUpdateTime = Time.realtimeSinceStartup;
                return;
            }

            var delta = GetLookDelta();

            // Skip empty input
            if (delta.sqrMagnitude == 0)
                return;

            // Mitigate delta spikes
            var deltaDifferenceSquared = (delta - previousLookDelta).sqrMagnitude;
            if (deltaDifferenceSquared > 100f)
                return;

            look.x += delta.x;
            look.y += delta.y;

            var pitchLimit = settings.lookPitchLimit;
            look.y = Mathf.Clamp(look.y, -pitchLimit, pitchLimit);

            cameraHelper.transform.localRotation = Quaternion.Euler(-look.y, 0, 0);
            transform.localRotation = Quaternion.Euler(0, look.x + lookOffset, 0);

            previousLookDelta = delta;
            previousLookUpdateTime = Time.realtimeSinceStartup;
        }
    }
}