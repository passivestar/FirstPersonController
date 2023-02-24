using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstPersonController
{
    public class PlayerSettings : MonoBehaviour
    {
        // =================================================================
        [Header("General")]
        // =================================================================

        [Tooltip("Player mass")]
        public float mass = 1f;

        [Tooltip("Gravity")]
        public Vector3 gravity = new Vector3(0f, -9.8f, 0f);

        [Tooltip("How fast the movementFactor variable changes. This is used primarily for cosmetic effects")]
        [Range(0, 5f)] public float movementFactorInterpolationSpeed = 3f;

        [Tooltip("Reset position if player's Y is lower than this")]
        public float worldBottomBoundary = -100f;

        [Tooltip("Automatically lock the cursor on start")]
        public bool lockCursorOnStart = true;

        // =================================================================
        [Header("Input")]
        // =================================================================

        [Tooltip("Whether or not to listen to any input")]
        public bool inputEnabled = true;

        [Tooltip("Both inputEnabled and this needs to be true for look to work")]
        public bool lookEnabled = true;

        [Tooltip("Mouse sensitivity")]
        public float mouseSensitivity = 3f;

        [Tooltip("Invert look Y")]
        public bool invertLookY = false;

        // =================================================================
        [Header("Ground")]
        // =================================================================

        [Tooltip("Ground detection raycast when on ground")]
        public float groundRaycastDistance = 0.02f;

        [Tooltip("Ground detection raycast when on a platform")]
        public float platformRaycastDistance = 0.5f;

        [Tooltip("A continuous force applied when on ground. Makes walking down slopes smoother")]
        public float stickToGroundVelocity = 3f;

        [Tooltip("Only walk on these objects, slide off other")]
        public LayerMask walkableGroundMask = ~0; // All layers

        [Tooltip("Slide faster with values greater than 0, or slower with values less than 1")]
        public float slideSpeedMultiplier = 1;

        // =================================================================
        [Header("Physics")]
        // =================================================================

        [Tooltip("The difference in meters between the character controller radius and the rigidbody radius")]
        public float rigidbodyColliderMargin = .15f;

        [Tooltip("If rigidbodies respond with an impulse bigger than this player won't move in the direction of impulse")]
        public float rigidbodyPositionSpring = 15000f;

        // =================================================================
        [Header("State Walking")]
        // =================================================================

        [Tooltip("Movement speed when walking")]
        public float walkingSpeed = 4f;

        [Tooltip("Velocity change rate when walking")]
        public float walkingAcceleration = 10f;

        [Tooltip("Velocity change rate when in the air")]
        public float midairAcceleration = 2f;

        [Tooltip("Force applied to the player when trying to unstick from the ceiling")]
        public float ceilingUnstickForce = 20f;

        // =================================================================
        [Header("State Flying")]
        // =================================================================

        [Tooltip("Movement speed when flying")]
        public float flyingSpeed = 4f;

        [Tooltip("Velocity change rate when flying")]
        public float flyingAcceleration = 10f;

        // =================================================================
        [Header("State Swimming")]
        // =================================================================

        [Tooltip("Movement speed when flying")]
        public float swimmingSpeed = 4f;

        [Tooltip("Velocity change rate when flying")]
        public float swimmingAcceleration = 10f;

        // =================================================================
        [Header("State Climbing")]
        // =================================================================

        [Tooltip("Movement speed when climbing")]
        public float climbingSpeed = 4f;

        [Tooltip("Velocity change rate when climbing")]
        public float climbingAcceleration = 10f;

        // =================================================================
        [Header("Jumping")]
        // =================================================================

        [Tooltip("The jump launch vertical speed")]
        public float jumpSpeed = 4f;

        [Tooltip("Allows jumping if player is trying to jump before hitting the ground. Time in seconds")]
        public float jumpPressBufferTime = .05f;

        [Tooltip("Allows jumping if player is trying to jump after losing the ground. Time in seconds")]
        public float jumpGroundGraceTime = .2f;

        [Tooltip("Maximum allowed number of jumps. Can be set to 0")]
        public int maxJumps = 1;

        // =================================================================
        [Header("Crouching")]
        // =================================================================

        [Tooltip("Player's capsule height when crouched")]
        public float crouchHeight = 1f;

        [Tooltip("The speed of capsule's height change")]
        public float crouchTransitionSpeed = 10f;

        [Tooltip("Multiply the maximum allowed walking speed by this when crouching")]
        public float crouchSpeedMultiplier = .5f;

        [Tooltip("Toggle crouch")]
        public bool crouchToggle = false;

        // =================================================================
        [Header("Sprinting")]
        // =================================================================

        [Tooltip("Multiply the maximum allowed walking speed by this when sprinting")]
        public float sprintSpeedMultiplier = 1.8f;

        // =================================================================
        [Header("Interaction")]
        // =================================================================

        [Tooltip("Prevent player from looking around and moving when dragging with interact")]
        public bool lockInputOnInteract = true;

        [Tooltip("Only allow interaction if an object is closer than this distance")]
        public float interactionMaxDistance = 3f;

        [Tooltip("Only allow interaction if an object matches the mask")]
        public LayerMask interactionMask = ~0; // All layers

        [Tooltip("Use SendMessage to notify objects when player is interacting with them")]
        public bool useSendMessageForInteraction = true;

        // =================================================================
        [Header("Grabbing")]
        // =================================================================

        [Tooltip("Velocity applied to the grabbed object")]
        public float grabVelocity = 30f;

        [Tooltip("Rotation speed applied to the grabbed object")]
        public float grabRotationSpeed = .2f;

        [Tooltip("Force applied to the grabbed object when trying to throw")]
        public float throwForce = 10f;

        [Tooltip("Stop holding the grabbed object if the distance between the grab point and the object exceeds this")]
        public float grabMaxHoldDistance = 3f;

        [Tooltip("Grab position offset relative to player's camera")]
        public Vector3 grabPositionOffset = new Vector3(0, 0, 2f);

        [Tooltip("Only allow holding objects lighter than this")]
        public float grabMassLimit = 20f;

        [Tooltip("Only allow grabbing if an object matches the mask")]
        public LayerMask grabMask = ~0; // All layers

        [Tooltip("Switch the grabbed object's rigidbody interpolation mode to Interpolate")]
        public bool grabForceInterpolation = true;

        // =================================================================
        [Header("Sound")]
        // =================================================================

        [Tooltip("Multiply the volume of footstep clips by this amount")]
        public float footstepsVolumeMultiplier = .1f;

        [Tooltip("Determines the interval between footsteps")]
        public float footstepsIntervalMultiplier = .5f;

        [Tooltip("Maximum interval between footsteps")]
        public float footstepsMaxIntervalLimit = .6f;

        [Tooltip("Minimum interval between footsteps")]
        public float footstepsMinIntervalLimit = .3f;

        [Tooltip("Pitch range for the randomizer")]
        public float footstepsPitchRange = .5f;

        [Serializable]
        public class AudioClipListByMaterials
        {
            public List<Material> materials;
            public List<AudioClip> list;
        }

        [Tooltip("An object holding references to all of the sound clips")]
        public List<AudioClipListByMaterials> footstepClips;

        [Tooltip("Maximum volume when landing")]
        public float landingMaxVolume = .4f;

        [Tooltip("Velocity threshold to determine landing")]
        public float landingVelocityThreshold = .5f;

        [Tooltip("Wind clip to play when moving fast")]
        public AudioClip windClip;

        [Tooltip("Wind clip volume will start increasing from 0 to windVolumeMaxLimit at this speed")]
        public float windVolumeMinSpeed = 10f;

        [Tooltip("Wind clip volume will stop increasing from 0 to windVolumeMaxLimit at this speed")]
        public float windVolumeMaxSpeed = 100f;

        [Tooltip("Maximum wind volume")]
        public float windVolumeMaxLimit = .1f;

        [Tooltip("A clip to play when grabbing an object")]
        public AudioClip grabbingClip;

        // =================================================================
        [Header("Camera Effects")]
        // =================================================================

        [Tooltip("Apply head bobbing to the camera")]
        public bool useHeadBobbing = true;

        [Tooltip("Player speed will be multiplied by this value to determine the vertical amplitude")]
        public float headBobbingVerticalAmplitudeMultiplier = 0.001f;

        [Tooltip("Player speed will be multiplied by this value to determine the horizontal amplitude")]
        public float headBobbingHorizontalAmplitudeMultiplier = 0.003f;

        [Tooltip("Player speed will be multiplied by this value to determine the angle amplitude")]
        public float headBobbingAngleAmplitudeMultiplier = 0.02f;

        [Tooltip("Player speed will be multiplied by this value to determine the frequency")]
        public float headBobbingFrequencyMultiplier = 1.3f;

        [Tooltip("Head bobbing frequency will never be higher than this value")]
        public float headBobbingFrequencyLimit = 14f;

        [Tooltip("Apply inertia to the camera")]
        public bool useInertia = true;

        [Tooltip("How fast to offset the camera")]
        public float inertiaRestitutionRate = 12f;

        [Tooltip("How quickly the player needs to stop for the inertia animation to be player")]
        public float inertiaSpeedThreshold = 2f;

        [Tooltip("How much the camera will move")]
        public float inertiaPositionOffsetMultiplier = .1f;

        [Tooltip("How much the camera will rotate")]
        public float inertiaAngleOffsetMultiplier = .7f;

        [Tooltip("Rotate the camera when playing the inertia animation")]
        public bool inertiaUseRotation = true;

        [Tooltip("Roll the camera")]
        public bool useRoll = true;

        [Tooltip("How much to roll the camera when strafing")]
        public float rollLateralFactor = 0.04f;

        [Tooltip("How much to roll the camera when looking around")]
        public float rollAngularFactor = 0.002f;

        [Tooltip("How fast to roll the camera")]
        public float rollSpeed = 14f;

        [Tooltip("Change FOV when moving")]
        public bool useFOV = true;
        public float fovMultiplier = 1.15f;
        public float fovInterpolationSpeed = 5f;
        public AnimationCurve fovSpeedRemap = AnimationCurve.EaseInOut(0, 0, 1, 1);
    }
}