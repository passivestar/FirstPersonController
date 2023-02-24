using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace FirstPersonController
{
    [RequireComponent(typeof(Player)), RequireComponent(typeof(PlayerInteraction))]
    public class PlayerGrabbing : MonoBehaviour
    {
        public UnityEvent<GameObject> onGameObjectGrabbableLookAt;
        public UnityEvent<GameObject> onGameObjectGrabbableLookAway;
        public UnityEvent<GameObject, Rigidbody> onGameObjectGrabbed;
        public UnityEvent<GameObject, Rigidbody> onGameObjectReleased;
        public UnityEvent onGameObjectDestroyed;

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
            playerInteraction.onPlayerLookAt.AddListener(OnLookAt);
            playerInteraction.onPlayerLookAway.AddListener(OnLookAway);
            playerInteraction.onPlayerInteractStarted.AddListener(OnInteractStarted);
            playerInteraction.onPlayerInteractStartedNoHit.AddListener(OnInteractStartedNoHit);
            if (playerInputInstance != null)
            {
                fireAction.performed += Throw;
                player.rigidbodyObject.GetComponent<CollisionEvents>().onCollisionStay += OnRigidbodyCollisionStay;
            }
        }

        void OnDisable()
        {
            player.onBeforeMove.RemoveListener(OnBeforeMove);
            playerInteraction.onPlayerLookAt.RemoveListener(OnLookAt);
            playerInteraction.onPlayerLookAway.RemoveListener(OnLookAway);
            playerInteraction.onPlayerInteractStarted.RemoveListener(OnInteractStarted);
            playerInteraction.onPlayerInteractStartedNoHit.RemoveListener(OnInteractStartedNoHit);
            fireAction.performed -= Throw;

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
                onGameObjectGrabbableLookAt.Invoke(gameObject);
                if (player.settings.useSendMessageForInteraction)
                {
                    gameObject.SendMessage("OnPlayerGrabbableLookAt", SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        void OnLookAway(GameObject gameObject)
        {
            if (lookedAtGrabbableObject != null)
            {
                onGameObjectGrabbableLookAway.Invoke(gameObject);
                if (player.settings.useSendMessageForInteraction)
                {
                    gameObject.SendMessage("OnPlayerGrabbableLookAway", SendMessageOptions.DontRequireReceiver);
                }
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

                grabbedObjectRigidbody.mass /= 20f;
                grabbedObjectRigidbody.angularDrag = 20f;

                if (player.settings.grabForceInterpolation)
                {
                    grabbedObjectInitialInterpolation = grabbedObjectRigidbody.interpolation;
                    grabbedObjectRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                }

                grabbedObjectInitialCollisionDetectionMode = grabbedObjectRigidbody.collisionDetectionMode;
                grabbedObjectRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                onGameObjectGrabbed.Invoke(grabbedObject, grabbedObjectRigidbody);
                if (player.settings.useSendMessageForInteraction)
                {
                    grabbedObject.SendMessage("OnPlayerGrabbed", SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        void StopHolding()
        {
            if (isHolding)
            {
                onGameObjectReleased.Invoke(grabbedObject, grabbedObjectRigidbody);
                if (player.settings.useSendMessageForInteraction)
                {
                    grabbedObject.SendMessage("OnPlayerReleased", SendMessageOptions.DontRequireReceiver);
                }

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

                if (distanceToGrabPosition > player.settings.grabMaxHoldDistance)
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
                onGameObjectDestroyed.Invoke();
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