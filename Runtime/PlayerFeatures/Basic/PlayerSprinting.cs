using UnityEngine;
using UnityEngine.InputSystem;

namespace FirstPersonController
{
    [RequireComponent(typeof(Player))]
    public class PlayerSprinting : MonoBehaviour
    {
        Player player;
        PlayerInput playerInputInstance;
        InputAction sprintAction;
        PlayerCrouching playerCrouching;

        void Awake()
        {
            player = GetComponent<Player>();
        }

        void Start()
        {
            playerInputInstance = player.playerInputInstance;
            sprintAction = playerInputInstance.actions["sprint"];
            TryGetComponent<PlayerCrouching>(out playerCrouching);
        }

        void OnEnable() => player.onBeforeMove.AddListener(OnBeforeMove);
        void OnDisable() => player.onBeforeMove.RemoveListener(OnBeforeMove);

        void OnBeforeMove()
        {
            var sprintInput = sprintAction.ReadValue<float>();
            if (sprintInput == 0) return;

            if (playerCrouching != null && playerCrouching.IsCrouching) return;

            var forwardMovementFactor = Mathf.Clamp01(
                Vector3.Dot(player.transform.forward, player.velocity.normalized)
            );

            var multiplier = Mathf.Lerp(1f, player.settings.sprintSpeedMultiplier, forwardMovementFactor);
            player.movementSpeedMultiplier *= multiplier;
        }
    }
}