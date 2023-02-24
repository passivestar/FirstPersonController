using UnityEngine;

namespace FirstPersonController
{
    [System.Serializable]
    public class PlayerStateClimbing : PlayerState
    {
        public override void FixedUpdate(Player player)
        {
            player.onBeforeMove.Invoke();

            var input = player.settings.inputEnabled
                ? player.GetMovementInput(player.settings.climbingSpeed, false)
                : Vector3.zero;

            var forwardInputFactor = Vector3.Dot(player.gameObject.transform.forward, input.normalized);

            if (forwardInputFactor > 0)
            {
                input.x = input.x * .5f;
                input.z = input.z * .5f;

                if (Mathf.Abs(input.y) > .2f)
                {
                    input.y = Mathf.Sign(input.y) * player.settings.climbingSpeed;
                }
            }
            else
            {
                input.y = 0;
                input.x = input.x * 3f;
                input.z = input.z * 3f;
            }


            var factor = player.settings.climbingAcceleration * Time.deltaTime;
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