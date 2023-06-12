using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace FirstPersonController
{
    [RequireComponent(typeof(Player)), RequireComponent(typeof(PlayerInteraction))]
    public class PlayerGrabbing : MonoBehaviour
    {
        public UnityEvent<GameObject> onGrabbableLookAt;
        public UnityEvent<GameObject> onGrabbableLookAway;
        public UnityEvent<GameObject, Rigidbody> onGrabbed;
        public UnityEvent<GameObject, Rigidbody> onReleased;
        public UnityEvent onDestroyed;

        Player player;
        PlayerInteraction playerInteraction;
        PlayerInput playerInputInstance;
        InputAction fireAction;

        bool isHolding;
        GameObject lookedAtGrabbableObject;
        GameObject grabbedObject;
        Rigidbody grabbedObjectRigidbody;

        float grabbedObjectInitialMass;
        float grabbedObjectInitialAngualDrag;

        RigidbodyInterpolation grabbedObjectInitialInterpolation;
        CollisionDetectionMode grabbedObjectInitialCollisionDetectionMode;
        Quaternion grabbedObjectRotationOffset;
        Vector3 grabOffset;

        void Awake()
        {
            player = GetComponent<Player>();
            playerInteraction = GetComponent<PlayerInteraction>();
        }

        void Start()
        {
            playerInputInstance = player.playerInputInstance;
            fireAction = playerInputInstance.actions["fire"];
            fireAction.performed += Throw;
            player.rigidbodyObject.GetComponent<CollisionEvents>().onCollisionStay += OnRigidbodyCollisionStay;
        }

        void OnEnable()
        {
            player.onBeforeMove.AddListener(OnBeforeMove);
            playerInteraction.onLookAt.AddListener(OnLookAt);
            playerInteraction.onLookAway.AddListener(OnLookAway);
            playerInteraction.onStarted.AddListener(OnInteractStarted);
            playerInteraction.onStartedNoHit.AddListener(OnInteractStartedNoHit);
            if (playerInputInstance != null)
            {
                fireAction.performed += Throw;
                player.rigidbodyObject.GetComponent<CollisionEvents>().onCollisionStay += OnRigidbodyCollisionStay;
            }
        }

        void OnDisable()
        {
            player.onBeforeMove.RemoveListener(OnBeforeMove);
            playerInteraction.onLookAt.RemoveListener(OnLookAt);
            playerInteraction.onLookAway.RemoveListener(OnLookAway);
            playerInteraction.onStarted.RemoveListener(OnInteractStarted);
            playerInteraction.onStartedNoHit.RemoveListener(OnInteractStartedNoHit);

            if (fireAction != null)
            {
                fireAction.performed -= Throw;
            }

            player.rigidbodyObject.GetComponent<CollisionEvents>().onCollisionStay -= OnRigidbodyCollisionStay;
        }

        bool CanGrab(GameObject gameObject, RaycastHit hit)
        {
            if (player.settings.grabMask != (player.settings.grabMask | (1 << gameObject.layer))) return false;
            var rigidbody = gameObject.GetComponent<Rigidbody>();
            if (rigidbody == null) return false;
            if (rigidbody.mass > player.settings.grabMassLimit) return false;
            if (rigidbody.isKinematic) return false;
            return true;
        }

        void OnLookAt(GameObject gameObject, RaycastHit hit)
        {
            if (CanGrab(gameObject, hit))
            {
                lookedAtGrabbableObject = gameObject;
                onGrabbableLookAt.Invoke(gameObject);

                if (gameObject.TryGetComponent<PlayerGrabbable>(out var playerGrabbable))
                    playerGrabbable.onLookAt.Invoke();
            }
        }

        void OnLookAway(GameObject gameObject)
        {
            if (lookedAtGrabbableObject != null)
            {
                onGrabbableLookAway.Invoke(gameObject);
                if (gameObject.TryGetComponent<PlayerGrabbable>(out var playerGrabbable))
                    playerGrabbable.onLookAway.Invoke();
                lookedAtGrabbableObject = null;
            }
        }

        void OnInteractStarted(GameObject gameObject, RaycastHit hit)
        {
            if (grabbedObject != null)
            {
                StopHolding();
            }
            else if (lookedAtGrabbableObject != null)
            {
                var rigidbody = lookedAtGrabbableObject.GetComponent<Rigidbody>();
                StartHolding(lookedAtGrabbableObject, rigidbody, hit);
            }
        }

        void OnInteractStartedNoHit()
        {
            if (grabbedObject != null)
            {
                StopHolding();
            }
        }

        void StartHolding(GameObject gameObject, Rigidbody rigidbody, RaycastHit hit)
        {
            // Prevent prop climbing:
            var canHold = player.IsGrounded && rigidbody != player.groundObjectRigidbody;

            if (!isHolding && canHold)
            {
                isHolding = true;
                grabbedObject = gameObject;
                grabbedObjectRigidbody = rigidbody;

                // Storing the offset between the grab point and the pivot of the grabbed object
                grabOffset = player.cameraHelper.transform.InverseTransformDirection(grabbedObject.transform.position - hit.point);
                // Only consider the forward offset
                grabOffset = Vector3.Scale(grabOffset, Vector3.forward);

                grabbedObjectInitialMass = grabbedObjectRigidbody.mass;
                grabbedObjectInitialAngualDrag = grabbedObjectRigidbody.angularDrag;
                grabbedObjectRotationOffset = Quaternion.LookRotation
                (
                    Util.SnappedToNearestAxis
                    (
                        grabbedObject.transform.InverseTransformDirection(-player.cameraHelper.transform.forward)
                    ),
                    Util.SnappedToNearestAxis
                    (
                        grabbedObject.transform.InverseTransformDirection(player.cameraHelper.transform.up)
                    )
                );

                grabbedObjectRigidbody.mass /= 10f;
                grabbedObjectRigidbody.angularDrag = 20f;

                if (player.settings.grabForceInterpolation)
                {
                    grabbedObjectInitialInterpolation = grabbedObjectRigidbody.interpolation;
                    grabbedObjectRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                }

                grabbedObjectInitialCollisionDetectionMode = grabbedObjectRigidbody.collisionDetectionMode;
                grabbedObjectRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                onGrabbed.Invoke(grabbedObject, grabbedObjectRigidbody);
                if (grabbedObject.TryGetComponent<PlayerGrabbable>(out var playerGrabbable))
                    playerGrabbable.onGrabbed.Invoke();
            }
        }

        void StopHolding()
        {
            if (isHolding)
            {
                onReleased.Invoke(grabbedObject, grabbedObjectRigidbody);
                if (grabbedObject.TryGetComponent<PlayerGrabbable>(out var playerGrabbable))
                    playerGrabbable.onReleased.Invoke();

                grabbedObjectRigidbody.mass = grabbedObjectInitialMass;
                grabbedObjectRigidbody.angularDrag = grabbedObjectInitialAngualDrag;

                var velocity = grabbedObjectRigidbody.velocity;

                if (velocity.magnitude > 5f)
                {
                    var force = grabbedObjectRigidbody.velocity.normalized * player.settings.throwForce;
                    grabbedObjectRigidbody.velocity = Vector3.zero;
                    grabbedObjectRigidbody.AddForce(force, ForceMode.Impulse);
                }

                if (player.settings.grabForceInterpolation)
                {
                    grabbedObjectRigidbody.interpolation = grabbedObjectInitialInterpolation;
                }

                grabbedObjectRigidbody.collisionDetectionMode = grabbedObjectInitialCollisionDetectionMode;

                isHolding = false;
                grabbedObject = null;
                grabbedObjectRigidbody = null;
            }
        }

        void Throw(InputAction.CallbackContext context)
        {
            if (!isHolding) return;
            grabbedObjectRigidbody.mass = grabbedObjectInitialMass;
            grabbedObjectRigidbody.AddForce(player.cameraHelper.transform.forward * player.settings.throwForce, ForceMode.Impulse);
            StopHolding();
        }

        void OnBeforeMove()
        {
            if (grabbedObject != null)
            {
                var cameraTransform = player.cameraHelper.transform;

                var grabPosition = cameraTransform.position
                    + cameraTransform.TransformDirection(player.settings.grabPositionOffset)
                    + cameraTransform.TransformDirection(grabOffset);

                var directionToGrabPosition = grabPosition - grabbedObject.transform.position;
                var distanceToGrabPosition = directionToGrabPosition.magnitude;
                var distanceToGrabPositionUnclamped = distanceToGrabPosition;

                // Cast a ray from the object to grab position to check if there is a wall in between
                var ray = new Ray(grabbedObject.transform.position, directionToGrabPosition);
                var mask = player.settings.grabMask;
                var hit = Physics.RaycastAll(ray, distanceToGrabPosition, mask, QueryTriggerInteraction.Ignore);
                if (hit.Length > 0 && hit[0].collider.gameObject != grabbedObject)
                {
                    // Set the grab position to the hit point
                    grabPosition = hit[0].point;
                    directionToGrabPosition = grabPosition - grabbedObject.transform.position;
                    distanceToGrabPosition = directionToGrabPosition.magnitude;
                }

                if (distanceToGrabPositionUnclamped > player.settings.grabMaxHoldDistance)
                {
                    StopHolding();
                    return;
                }

                grabbedObjectRigidbody.velocity = directionToGrabPosition * player.settings.grabVelocity;

                var rotationToPlayer = Quaternion.LookRotation(cameraTransform.position - grabPosition);
                var rotationDifference = rotationToPlayer * Quaternion.Inverse(grabbedObject.transform.rotation * grabbedObjectRotationOffset);

                grabbedObjectRigidbody.angularVelocity = Util.ExtractEulersFromQuaternion(rotationDifference) * player.settings.grabRotationSpeed;
            }
            else if (isHolding)
            {
                isHolding = false;
                onDestroyed.Invoke();
            }
        }

        // Prevent prop climbing:
        void OnRigidbodyCollisionStay(Collision collision)
        {
            if (collision.collider.attachedRigidbody == grabbedObjectRigidbody && !player.IsGrounded
                || player.groundObjectRigidbody == grabbedObjectRigidbody)
            {
                StopHolding();
            }
        }
    }
}