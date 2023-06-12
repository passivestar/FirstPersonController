using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace FirstPersonController
{
    [RequireComponent(typeof(Player))]
    public class PlayerInteraction : MonoBehaviour
    {
        public UnityEvent<GameObject, RaycastHit> onLookAt;
        public UnityEvent<GameObject> onLookAway;
        public UnityEvent<GameObject, RaycastHit> onStarted;
        public UnityEvent<GameObject, RaycastHit, Vector2, Vector2> onDrag;
        public UnityEvent<GameObject, RaycastHit> onPerformed;
        public UnityEvent onStartedNoHit;
        public UnityEvent onPerformedNoHit;

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
            if (interactAction != null)
            {
                interactAction.started -= OnInteractStarted;
                interactAction.performed -= OnInteractPerformed;
            }
        }

        void Update()
        {
            if (isActive)
            {
                var delta = lookAction.ReadValue<Vector2>();
                if (delta.magnitude > 10f)
                    return;

                var lookDelta = player.GetLookDelta();
                totalDelta.x += lookDelta.x;
                totalDelta.y += lookDelta.y;

                if (hitCollider == null)
                    return;
 
                NotifyMove(hitCollider.gameObject, hit, delta, totalDelta);
            }
        }

        void OnBeforeMove()
        {
            // Don't cast rays while holding interact
            if (isActive)
                return;

            var from = player.cameraHelper.transform.position;
            var direction = player.cameraHelper.transform.forward;

            if (Physics.Raycast(from, direction, out hit, player.settings.interactionMaxDistance, player.settings.interactionMask))
            {
                if (hitCollider != hit.collider)
                {
                    if (hitCollider != null)
                    {
                        NotifyLookAway(hitCollider.gameObject);
                    }
                    NotifyLookAt(hit.collider.gameObject);
                    hitCollider = hit.collider;
                }
            }
            else if (hitCollider != null)
            {
                NotifyLookAway(hitCollider.gameObject);
                hitCollider = null;
            }
        }

        void OnInteractStarted(InputAction.CallbackContext context)
        {
            if (hitCollider != null)
            {
                isActive = true;
                totalDelta = Vector2.zero;
                NotifyStarted(hitCollider.gameObject);
            }
            else
            {
                onStartedNoHit.Invoke();
            }
        }

        void OnInteractPerformed(InputAction.CallbackContext context)
        {
            isActive = false;

            if (hitCollider != null)
            {
                NotifyPerformed(hitCollider.gameObject, hit);
            }
            else
            {
                onPerformedNoHit.Invoke();
            }
        }

        void NotifyLookAt(GameObject target)
        {
            if (target.TryGetComponent<PlayerInteractable>(out var interactable))
            {
                onLookAt.Invoke(target, hit);
                interactable.onLookAt.Invoke(hit);
            }
        }

        void NotifyLookAway(GameObject target)
        {
            if (target.TryGetComponent<PlayerInteractable>(out var interactable))
            {
                onLookAway.Invoke(target);
                interactable.onLookAway.Invoke();
            }
        }

        void NotifyStarted(GameObject target)
        {
            if (target.TryGetComponent<PlayerInteractable>(out var interactable))
            {
                if (interactable.lockInputOnInteract)
                    player.settings.inputEnabled = false;
                onStarted.Invoke(target, hit);
                interactable.onStarted.Invoke(hit);
            }
        }

        void NotifyPerformed(GameObject target, RaycastHit hit)
        {
            if (target.TryGetComponent<PlayerInteractable>(out var interactable))
            {
                if (interactable.lockInputOnInteract)
                    player.settings.inputEnabled = true;
                onPerformed.Invoke(target, hit);
                interactable.onPerformed.Invoke(hit);
            }
        }

        void NotifyMove(GameObject target, RaycastHit hit, Vector2 delta, Vector2 totalDelta)
        {
            if (target.TryGetComponent<PlayerInteractable>(out var interactable))
            {
                onDrag.Invoke(target, hit, delta, totalDelta);
                interactable.onDrag.Invoke(hit, delta, totalDelta);
            }
        }
    }
}