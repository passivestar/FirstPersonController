using UnityEngine;

namespace FirstPersonController
{
    [RequireComponent(typeof(Player))]
    public class PlayerCameraEffects : MonoBehaviour
    {
        Player player;
        CharacterController controller;
        GameObject cameraEffectsHelper;

        float headBobbingTimer = 0f;
        Vector3 inertiaOffsetPosition, inertiaTargetOffsetPosition, inertiaPreviousVelocity;
        Quaternion inertiaOffsetRotation, inertiaTargetOffsetRotation;
        float inertiaPreviousPositionY;
        float roll, angularSpeed, previousRotationY;
        float groundFactor = 0f;
        float groundFactorInterpolationSpeed = 20f;

#if USE_CINEMACHINE
        Cinemachine.CinemachineVirtualCamera cinemachineCamera;
#else
        Camera regularCamera;
#endif
        float initialFOV;
        float currentFOV;

        void Awake()
        {
            player = GetComponent<Player>();
            controller = GetComponent<CharacterController>();
        }

        void Start()
        {
            previousRotationY = transform.localEulerAngles.y;
            cameraEffectsHelper = player.cameraEffectsHelper;
#if USE_CINEMACHINE
            cinemachineCamera = player.cameraObject.GetComponent<Cinemachine.CinemachineVirtualCamera>();
            initialFOV = cinemachineCamera.m_Lens.FieldOfView;
#else
            regularCamera = player.cameraObject.GetComponent<Camera>();
            initialFOV = regularCamera.fieldOfView;
#endif
            currentFOV = initialFOV;
        }

        void UpdateHeadBobbing(float horizontalSpeed)
        {
            var ampHorizontal = horizontalSpeed * player.settings.headBobbingHorizontalAmplitudeMultiplier * groundFactor;
            var ampVertical = horizontalSpeed * player.settings.headBobbingVerticalAmplitudeMultiplier * groundFactor;

            var ampAngle = horizontalSpeed * player.settings.headBobbingAngleAmplitudeMultiplier * groundFactor;
            var freq = Mathf.Min(horizontalSpeed * player.settings.headBobbingFrequencyMultiplier, player.settings.headBobbingFrequencyLimit);
            headBobbingTimer += Time.deltaTime * freq;

            var offsetHorizontal = Mathf.Cos(headBobbingTimer) * ampHorizontal;
            var offsetVertical = Mathf.Sin(headBobbingTimer * 2) * ampVertical;
            cameraEffectsHelper.transform.Translate(0, offsetVertical, offsetHorizontal, Space.World);

            var pitch = -Mathf.Sin(headBobbingTimer * 2) * ampAngle;
            cameraEffectsHelper.transform.Rotate(new Vector3(pitch, 0, 0));
        }

        void AddCameraImpulse(Vector3 impulse, float angleOffset = 1f)
        {
            // Clamping to controller height so that camera doesnt go
            // through the ground when falling from a large distance
            inertiaTargetOffsetPosition += Vector3.ClampMagnitude(impulse * player.settings.inertiaPositionOffsetMultiplier, controller.height * 0.9f);
            if (player.settings.inertiaUseRotation)
            {
                var angle = impulse.magnitude * angleOffset * player.settings.inertiaAngleOffsetMultiplier;
                var randomZ = Random.Range(-angle, angle);
                inertiaTargetOffsetRotation *= Quaternion.Euler(angle, 0, randomZ);
            }
        }

        void AddCameraDamageImpulse(float damage)
        {
            var offset = Mathf.Min(damage * 10, 50f);
            var impulse = new Vector3(0, 0, offset);
            AddCameraImpulse(impulse, damage * .01f);
        }

        void UpdateInertia()
        {
            var diff = inertiaPreviousVelocity - controller.velocity;
            // If stopped quickly
            if (
                transform.position.y - inertiaPreviousPositionY <= 0
                && diff.magnitude > player.settings.inertiaSpeedThreshold
                && inertiaPreviousVelocity.sqrMagnitude > controller.velocity.sqrMagnitude
            )
            {
                AddCameraImpulse(diff);
            }

            var factor = Time.deltaTime * player.settings.inertiaRestitutionRate;

            // Target offset that we're gonna interpolate to
            inertiaTargetOffsetPosition = Vector3.Lerp(inertiaTargetOffsetPosition, Vector3.zero, factor);
            inertiaTargetOffsetRotation = Quaternion.Slerp(inertiaTargetOffsetRotation, Quaternion.identity, factor);

            // Actual offset. Interpolating twice here to
            // make it look smooth when we set it each fram
            inertiaOffsetPosition = Vector3.Lerp(inertiaOffsetPosition, inertiaTargetOffsetPosition, factor);

            // Clamp magnitude to prevent camera going through walls and ceilings
            var radius = controller.radius * .7f;
            inertiaOffsetPosition.x = Mathf.Clamp(inertiaOffsetPosition.x, -radius, radius);
            inertiaOffsetPosition.z = Mathf.Clamp(inertiaOffsetPosition.z, -radius, radius);
            inertiaOffsetPosition.y = Mathf.Clamp(inertiaOffsetPosition.y, -radius, 0);

            inertiaOffsetRotation = Quaternion.Slerp(inertiaOffsetRotation, inertiaTargetOffsetRotation, factor);

            // Apply offset to the helper object
            cameraEffectsHelper.transform.Translate(inertiaOffsetPosition, Space.World);
            cameraEffectsHelper.transform.Rotate(inertiaOffsetRotation.eulerAngles);

            inertiaPreviousVelocity = controller.velocity;
            inertiaPreviousPositionY = transform.position.y;
        }

        void UpdateRoll(float horizontalSpeed)
        {
            // Angular speed
            var rotationY = transform.localEulerAngles.y;
            var difference = Mathf.DeltaAngle(previousRotationY, rotationY);
            var dt = Time.deltaTime != 0 ? Time.deltaTime : Mathf.Epsilon;
            angularSpeed = Mathf.Lerp(angularSpeed, difference / dt, Time.deltaTime * (player.settings.rollSpeed / 2));
            previousRotationY = rotationY;
            var angularTargetAngle = angularSpeed * player.settings.rollAngularFactor;

            // Lateral speed
            var lateral = Vector3.Dot(cameraEffectsHelper.transform.right, player.velocity);
            lateral = horizontalSpeed == 0f ? 0f : lateral / horizontalSpeed;
            var lateralTargetAngle = lateral * horizontalSpeed * horizontalSpeed * player.movementFactor * player.settings.rollLateralFactor;

            var targetAngle = lateralTargetAngle + angularTargetAngle;
            roll = Mathf.Lerp(roll, targetAngle, Time.deltaTime * player.settings.rollSpeed);
            cameraEffectsHelper.transform.Rotate(0, 0, roll, Space.Self);
        }

        void UpdateFOV(float speed)
        {
            var maxSpeed = player.settings.walkingSpeed * player.settings.sprintSpeedMultiplier;

            var targetFOV = Mathf.Lerp(
                initialFOV,
                initialFOV * player.settings.fovMultiplier,
                player.settings.fovSpeedRemap.Evaluate(speed / maxSpeed)
            );

            currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * player.settings.fovInterpolationSpeed);
#if USE_CINEMACHINE
            cinemachineCamera.m_Lens.FieldOfView = currentFOV;
#else     
            camera.fieldOfView = currentFOV;
#endif
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

            var speed = controller.velocity.magnitude;
            var horizontalSpeed = Vector3.ProjectOnPlane(controller.velocity, Vector3.up).magnitude;

            if (player.settings.useHeadBobbing) UpdateHeadBobbing(horizontalSpeed);
            if (player.settings.useInertia) UpdateInertia();
            if (player.settings.useRoll) UpdateRoll(horizontalSpeed);
            if (player.settings.useFOV) UpdateFOV(speed);
        }
    }
}