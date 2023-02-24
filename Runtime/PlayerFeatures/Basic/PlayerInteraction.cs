using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace FirstPersonController
{
    [RequireComponent(typeof(Player))]
    public class PlayerInteraction : MonoBehaviour
    {
        public UnityEvent<GameObject, RaycastHit> onPlayerLookAt;
        public UnityEvent<GameObject> onPlayerLookAway;
        public UnityEvent<GameObject, RaycastHit> onPlayerInteractStarted;
        public UnityEvent<GameObject, RaycastHit, Vector2, Vector2> onPlayerInteractMove;
        public UnityEvent<GameObject, RaycastHit> onPlayerInteractPerformed;
        public UnityEvent onPlayerInteractStartedNoHit;
        public UnityEvent onPlayerInteractPerformedNoHit;

        Player player;
        PlayerInput playerInputInstance;
        InputAction interactAction;
        InputAction lookAction;

        Collider hitCollider;
        RaycastHit hit;

        // Keep track of mouse delta while dragging:
        bool isActive;
        Vector2 totalDelta;

        void Awake()
        {
            player = GetComponent<Player>();
        }

        void Start()
        {
            playerInputInstance = player.playerInputInstance;
            interactAction = playerInputInstance.actions["interact"];
            lookAction = playerInputInstance.actions["look"];
            interactAction.started += OnInteractStarted;
            interactAction.performed += OnInteractPerformed;
        }

        void OnEnable()
        {
            player.onBeforeMove.AddListener(OnBeforeMove);
            if (playerInputInstance != null)
            {
                interactAction.started += OnInteractStarted;
                interactAction.performed += OnInteractPerformed;
            }
        }

        void OnDisable()
        {
            player.onBeforeMove.RemoveListener(OnBeforeMove);
            interactAction.started -= OnInteractStarted;
            interactAction.performed -= OnInteractPerformed;
        }

        void Update()
        {
            if (isActive)
            {
                var delta = lookAction.ReadValue<Vector2>();
                if (delta.magnitude > 10f) return;

                var invertY = player.settings.invertLookY;
                totalDelta.x += delta.x * player.settings.mouseSensitivity;
                totalDelta.y += delta.y * player.settings.mouseSensitivity * (invertY ? -1f : 1f);

                if (hitCollider == null)
                {
                    return;
                }

                onPlayerInteractMove.Invoke(hitCollider.gameObject, hit, delta, totalDelta);
                if (player.settings.useSendMessageForInteraction)
                {
                    hitCollider.gameObject.SendMessage(
                        "OnPlayerInteractMove",
                        (hit, delta, totalDelta),
                        SendMessageOptions.DontRequireReceiver
                    );
                }
            }
        }

        void OnBeforeMove()
        {
            // Don't cast rays while holding interact
            if (isActive) return;

            var from = player.cameraHelper.transform.position;
            var direction = player.cameraHelper.transform.forward;

            if (Physics.Raycast(from, direction, out hit, player.settings.interactionMaxDistance, player.settings.interactionMask))
            {
                if (hitCollider != hit.collider)
                {
                    if (hitCollider != null)
                    {
                        SendLookAway(hitCollider.gameObject);
                    }
                    SendLookAt(hit.collider.gameObject);
                    hitCollider = hit.collider;
                }
            }
            else if (hitCollider != null)
            {
                SendLookAway(hitCollider.gameObject);
                hitCollider = null;
            }
        }

        void SendLookAt(GameObject target)
        {
            onPlayerLookAt.Invoke(target, hit);
            if (player.settings.useSendMessageForInteraction)
            {
                target.SendMessage("OnPlayerInteractLookAt", hit, SendMessageOptions.DontRequireReceiver);
            }
        }

        void SendLookAway(GameObject target)
        {
            onPlayerLookAway.Invoke(target);
            if (player.settings.useSendMessageForInteraction)
            {
                target.SendMessage("OnPlayerInteractLookAway", SendMessageOptions.DontRequireReceiver);
            }
        }

        void OnInteractStarted(InputAction.CallbackContext context)
        {
            if (hitCollider != null)
            {
                isActive = true;
                totalDelta = Vector2.zero;

                if (player.settings.lockInputOnInteract)
                {
                    player.settings.inputEnabled = false;
                }

                onPlayerInteractStarted.Invoke(hitCollider.gameObject, hit);
                if (player.settings.useSendMessageForInteraction)
                {
                    hitCollider.gameObject.SendMessage("OnPlayerInteractStarted", hit, SendMessageOptions.DontRequireReceiver);
                }
            }
            else
            {
                onPlayerInteractStartedNoHit.Invoke();
            }
        }

        void OnInteractPerformed(InputAction.CallbackContext context)
        {
            isActive = false;

            if (player.settings.lockInputOnInteract)
            {
                player.settings.inputEnabled = true;
            }

            if (hitCollider != null)
            {
                onPlayerInteractPerformed.Invoke(hitCollider.gameObject, hit);
                if (player.settings.useSendMessageForInteraction)
                {
                    hitCollider.gameObject.SendMessage("OnPlayerInteractPerformed", hit, SendMessageOptions.DontRequireReceiver);
                }
            }
            else
            {
                onPlayerInteractPerformedNoHit.Invoke();
            }
        }
    }
}