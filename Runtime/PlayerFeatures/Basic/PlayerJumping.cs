using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace FirstPersonController
{
    [RequireComponent(typeof(Player))]
    public class PlayerJumping : MonoBehaviour
    {
        public UnityEvent onJump;

        Player player;
        PlayerInput playerInputInstance;
        InputAction jumpAction;

        bool tryingToJump;
        float lastJumpPressTime;
        float lastGroundedTime;
        int jumps;

        void Awake()
        {
            player = GetComponent<Player>();
        }

        void Start()
        {
            playerInputInstance = player.playerInputInstance;
            jumpAction = playerInputInstance.actions["jump"];
            jumpAction.performed += OnJump;
        }

        void OnEnable()
        {
            player.onBeforeMove.AddListener(OnBeforeMove);
            player.onUngrounded.AddListener(OnUngrounded);
            if (playerInputInstance != null)
            {
                jumpAction.performed += OnJump;
            }
        }

        void OnDisable()
        {
            player.onBeforeMove.RemoveListener(OnBeforeMove);
            player.onUngrounded.RemoveListener(OnUngrounded);
            jumpAction.performed -= OnJump;
        }

        void OnJump(InputAction.CallbackContext context)
        {
            tryingToJump = true;
            lastJumpPressTime = Time.time;
        }

        void OnBeforeMove()
        {
            if (player.IsGrounded) jumps = 0;

            var isWalking = player.GetState() == PlayerState.Walking;

            var wasTryingToJump = Time.time - lastJumpPressTime < player.settings.jumpPressBufferTime;
            var wasGrounded = Time.time - lastGroundedTime < player.settings.jumpGroundGraceTime;

            var isOrWasTryingToJump = tryingToJump || (wasTryingToJump && player.IsGrounded);
            var isOrWasGrounded = player.IsGrounded || wasGrounded;

            var isSliding = player.IsSliding;
            var jumpAllowed = jumps < player.settings.maxJumps;
            var thereIsCeiling = player.ceilingObject != null;

            var canJump = jumpAllowed && isOrWasTryingToJump && isOrWasGrounded && isWalking && !isSliding && !thereIsCeiling;

            if (canJump)
            {
                player.Launch(new Vector3(0, player.settings.jumpSpeed, 0), false, true);
                jumps++;
                onJump.Invoke();
            }

            tryingToJump = false;
        }

        void OnUngrounded()
        {
            lastGroundedTime = Time.time;
        }
    }
}