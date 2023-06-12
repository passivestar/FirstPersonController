using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace FirstPersonController
{
    [RequireComponent(typeof(Player))]
    public class PlayerCrouching : MonoBehaviour
    {
        public UnityEvent<bool> onCrouchStateChange;
        public UnityEvent onCrouch;
        public UnityEvent onUncrouch;

        Player player;
        PlayerInput playerInputInstance;
        InputAction crouchAction;

        Vector3 initialCameraPosition;
        float currentHeight;
        float standingHeight;

        bool crouchToggleActive = false;
        bool wasCrouchingLastFrame = false;

        public bool IsCrouching => standingHeight - currentHeight > .1f;

        void Awake()
        {
            player = GetComponent<Player>();
        }

        void Start()
        {
            playerInputInstance = player.playerInputInstance;
            crouchAction = playerInputInstance.actions["crouch"];
            crouchAction.performed += OnCrouch;

            initialCameraPosition = player.cameraEffectsHelper.transform.localPosition;
            standingHeight = currentHeight = player.Height;
        }

        void OnEnable()
        {
            player.onBeforeMove.AddListener(OnBeforeMove);
            if (playerInputInstance != null)
            {
                crouchAction.performed += OnCrouch;
            }
        }

        void OnDisable()
        {
            player.onBeforeMove.RemoveListener(OnBeforeMove);
            if (crouchAction != null)
            {
                crouchAction.performed -= OnCrouch;
            }
        }

        void OnCrouch(InputAction.CallbackContext context)
        {
            crouchToggleActive = !crouchToggleActive;
        }

        void OnBeforeMove()
        {
            var isTryingToCrouch = player.settings.crouchToggle
                ? crouchToggleActive
                : crouchAction.ReadValue<float>() > 0;

            var canCrouch = player.GetState() == PlayerState.Walking;

            var heightTarget = isTryingToCrouch && canCrouch ? player.settings.crouchHeight : standingHeight;

            if (IsCrouching && !isTryingToCrouch)
            {
                var castOrigin = transform.position + new Vector3(0, currentHeight / 2, 0);
                if (Physics.Raycast(castOrigin, Vector3.up, out RaycastHit hit, 0.2f))
                {
                    var distanceToCeiling = hit.point.y - castOrigin.y;
                    heightTarget = Mathf.Max
                    (
                        currentHeight + distanceToCeiling - 0.1f,
                        player.settings.crouchHeight
                    );
                }
            }

            var crouchDelta = Time.deltaTime * player.settings.crouchTransitionSpeed;
            currentHeight = Mathf.Lerp(currentHeight, heightTarget, crouchDelta);

            var halfHeightDifference = new Vector3(0, (standingHeight - currentHeight) / 2, 0);
            var newCameraPosition = initialCameraPosition - halfHeightDifference;
            player.cameraEffectsHelper.transform.localPosition = newCameraPosition;
            player.Height = currentHeight;
            player.rigidbodyCollider.height = currentHeight;

            if (IsCrouching)
            {
                player.movementSpeedMultiplier *= player.settings.crouchSpeedMultiplier;
            }

            if (IsCrouching != wasCrouchingLastFrame)
            {
                onCrouchStateChange.Invoke(IsCrouching);
                wasCrouchingLastFrame = IsCrouching;
                if (IsCrouching)
                {
                    onCrouch.Invoke();
                }
                else
                {
                    onUncrouch.Invoke();
                }
            }
        }
    }
}