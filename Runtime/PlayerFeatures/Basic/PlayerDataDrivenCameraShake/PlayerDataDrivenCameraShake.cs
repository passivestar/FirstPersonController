using UnityEngine;

namespace FirstPersonController
{
    public class PlayerDataDrivenCameraShake : MonoBehaviour
    {
        Player player;
        CharacterController controller;
        GameObject cameraEffectsHelper;

        float groundFactor = 0f;
        float groundFactorInterpolationSpeed = 4f;

        // This is used to gradually transition into the camera shake animation
        // based on the player's speed.
        float delayedHorizontalSpeed = 0f;
        float delayedHorizontalSpeedInterpolationSpeed = 6f;

        float animationTimeInSeconds;
        float currentAnimationTime;

        void Awake()
        {
            player = GetComponent<Player>();
            controller = GetComponent<CharacterController>();
            var numberOfKeys = player.settings.dataDrivenCameraShakeData.PosX.keys.Length;
            animationTimeInSeconds = player.settings.dataDrivenCameraShakeData.PosX.keys[numberOfKeys - 1].time;
        }

        void Start()
        {
            cameraEffectsHelper = player.cameraEffectsHelper;
        }

        void OnEnable()
        {
            player.onBeforeMove.AddListener(OnBeforeMove);
        }

        void OnDisable()
        {
            player.onBeforeMove.RemoveListener(OnBeforeMove);
        }

        void OnBeforeMove()
        {
            // Interpolate ground factor
            groundFactor = Mathf.Lerp(
                groundFactor,
                player.IsGrounded ? 1 : 0,
                Time.deltaTime * groundFactorInterpolationSpeed
            );

            var horizontalSpeed = Vector3.ProjectOnPlane(controller.velocity, Vector3.up).magnitude;

            // Interpolate the delayed horizontal speed
            delayedHorizontalSpeed = Mathf.Lerp(
                delayedHorizontalSpeed,
                horizontalSpeed,
                Time.deltaTime * delayedHorizontalSpeedInterpolationSpeed
            );

            var data = player.settings.dataDrivenCameraShakeData;

            var maxSpeed = player.settings.walkingSpeed * player.settings.sprintSpeedMultiplier;

            var speedFactor = Mathf.InverseLerp(0, maxSpeed, delayedHorizontalSpeed);
            speedFactor = player.settings.dataDrivenCameraShakeSpeedFactorRemap.Evaluate(speedFactor);
            var speed = Mathf.Clamp01(player.settings.dataDrivenCameraShakeSpeedMultiplier * speedFactor);

            currentAnimationTime += Time.deltaTime * data.FPS * speed;
            currentAnimationTime = Mathf.Repeat(currentAnimationTime, animationTimeInSeconds);

            var amplitude = player.settings.dataDrivenCameraShakeAmplitudeMultiplier * groundFactor * delayedHorizontalSpeed;
            var rotationAmplitude = player.settings.dataDrivenCameraShakeRotationAmplitudeMultiplier * groundFactor * delayedHorizontalSpeed;

            var positionOffset = new Vector3(
                data.PosX.Evaluate(currentAnimationTime) * amplitude,
                data.PosY.Evaluate(currentAnimationTime) * amplitude,
                data.PosZ.Evaluate(currentAnimationTime) * amplitude
            );

            var rotationOffset = new Vector3(
                data.RotX.Evaluate(currentAnimationTime) * rotationAmplitude,
                data.RotY.Evaluate(currentAnimationTime) * rotationAmplitude,
                data.RotZ.Evaluate(currentAnimationTime) * rotationAmplitude 
            );

            cameraEffectsHelper.transform.Translate(positionOffset, Space.World);
            cameraEffectsHelper.transform.Rotate(rotationOffset);
        }
    }
}