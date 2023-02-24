using UnityEngine;

namespace FirstPersonController
{
    [System.Serializable]
    public class PlayerStateSwimming : PlayerState
    {
        public override void FixedUpdate(Player player)
        {
            player.onBeforeMove.Invoke();

            var input = player.settings.inputEnabled
                ? player.GetMovementInput(player.settings.swimmingSpeed, false)
                : Vector3.zero;

            var factor = player.settings.swimmingAcceleration * Time.deltaTime;
            player.velocity = Vector3.Lerp(player.velocity, input, factor);

            player.controller.Move(player.velocity * Time.deltaTime);
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