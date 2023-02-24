using UnityEngine;

namespace FirstPersonController
{
    [System.Serializable]
    public class PlayerStateWalking : PlayerState
    {
        public override void FixedUpdate(Player player)
        {
            player.UpdateGround();
            player.UpdateCeiling();
            player.UpdateGravity();
            player.onBeforeMove.Invoke();

            var input = player.settings.inputEnabled
                ? player.GetMovementInput(player.settings.walkingSpeed)
                : Vector3.zero;

            var factor = (player.IsGrounded ? player.settings.walkingAcceleration : player.settings.midairAcceleration) * Time.deltaTime;

            player.velocity.x = Mathf.Lerp(player.velocity.x, input.x, factor);
            player.velocity.z = Mathf.Lerp(player.velocity.z, input.z, factor);

            player.controller.Move(player.velocity * Time.deltaTime);
            player.CheckBounds();
        }

        public override void Update(Player player)
        {
            if (player.settings.inputEnabled && player.settings.lookEnabled)
            {
                player.UpdateLook();
            }
        }
    }
}