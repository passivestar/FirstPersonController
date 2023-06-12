using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FirstPersonController
{
    [RequireComponent(typeof(Player))]
    public class PlayerSound : MonoBehaviour
    {
        List<AudioClip> defaultFootstepSounds;
        List<AudioClip> climbingSounds;
        AudioSource source;
        AudioSource bodySource;
        AudioSource windSource;

        float footstepsInterval;
        float footstepsVolume;
        float previousVelocityY;

        float climbingInterval;
        float climbingVolume;

        float groundFactor = 0f;
        float groundFactorInterpolationSpeed = 30f;

        PlayerGrabbing playerGrabbing;

        IEnumerator footstepsCoroutine;
        IEnumerator climbingCoroutine;

        Player player;

        void Awake()
        {
            player = GetComponent<Player>();
            source = gameObject.AddComponent<AudioSource>();
            source.spatialBlend = 1f;

            bodySource = gameObject.AddComponent<AudioSource>();

            defaultFootstepSounds = GetAudioClipsByMaterial(null);

            windSource = gameObject.AddComponent<AudioSource>();
            windSource.clip = player.settings.windClip;
            windSource.loop = true;
            windSource.volume = 0;
            windSource.Play();
        }

        void OnEnable()
        {
            player.onGroundStateChange.AddListener(OnGroundStateChange);
            player.onStateChange.AddListener(OnStateChange);

            if (gameObject.TryGetComponent<PlayerGrabbing>(out playerGrabbing))
            {
                playerGrabbing.onGrabbed.AddListener(OnGameObjectGrabbed);
            }
        }

        void OnDisable()
        {
            player.onGroundStateChange.RemoveListener(OnGroundStateChange);
            player.onStateChange.RemoveListener(OnStateChange);

            if (playerGrabbing != null)
            {
                playerGrabbing.onGrabbed.RemoveListener(OnGameObjectGrabbed);
            }
        }

        void OnGameObjectGrabbed(GameObject gameObject, Rigidbody rigidbody)
        {
            bodySource.volume = .1f;
            bodySource.clip = player.settings.grabbingClip;
            bodySource.Play();
        }

        void OnGroundStateChange(bool isGrounded)
        {
            if (isGrounded)
            {
                footstepsCoroutine = FootstepsLoop();
                StartCoroutine(footstepsCoroutine);

                // Play landing sound
                var vol = Util.MapRange(previousVelocityY, 0, -4f, 0, player.settings.landingMaxVolume);
                PlayFootstepSound(vol);
            }
            else
            {
                if (footstepsCoroutine != null)
                {
                    StopCoroutine(footstepsCoroutine);
                }
            }
        }

        void OnStateChange(PlayerState state)
        {
            if (state is PlayerStateClimbing)
            {
                climbingCoroutine = ClimbingLoop();
                StartCoroutine(climbingCoroutine);
            }
            else
            {
                if (climbingCoroutine != null)
                {
                    StopCoroutine(climbingCoroutine);
                }
            }
        }

        List<AudioClip> GetAudioClipsByMaterial(Material material)
        {
            return player.settings.footstepClips?.Find(list => {
                if (list.materials == null || list.materials.Count == 0)
                {
                    return material == null;
                }

                return list.materials.Contains(material);
            })?.list;
        }

        IEnumerator FootstepsLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(footstepsInterval);
                if (footstepsVolume > 0)
                {
                    PlayFootstepSound(footstepsVolume);
                }
            }
        }

        void PlayFootstepSound(float volume)
        {
            if (player.groundObject == null)
            {
                return;
            }

            var list = GetAudioClipsByMaterial(player.groundObjectMaterial);
            list = list ?? defaultFootstepSounds;

            if (list != null)
            {
                // Find a new unique clip
                var newClip = list[Random.Range(0, list.Count)];
                while (newClip == source.clip)
                {
                    newClip = list[Random.Range(0, list.Count)];
                }

                source.clip = newClip;
                source.pitch = 1 + Random.Range(-player.settings.footstepsPitchRange / 2, player.settings.footstepsPitchRange / 2);
                source.volume = volume;
                source.Play();
            }
        }

        IEnumerator ClimbingLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(climbingInterval);
                if (climbingVolume > 0)
                {
                    PlayClimbingSound(climbingVolume);
                }
            }
        }

        void PlayClimbingSound(float volume)
        {
            var list = player.settings.climbingClips;

            if (list != null)
            {
                source.clip = list[Random.Range(0, list.Count)];
                source.pitch = 1 + Random.Range(-player.settings.climbingPitchRange / 2, player.settings.climbingPitchRange / 2);
                source.volume = volume;
                source.Play();
            }
        }

        void FixedUpdate()
        {
            UpdateFootstepsValues();
            UpdateClimbingValues();
            UpdateWindValues();
        }

        void UpdateFootstepsValues()
        {
            var horizontalSpeed = Vector3.ProjectOnPlane(player.controller.velocity, Vector3.up).magnitude;

            // Interpolate ground factor
            groundFactor = Mathf.Lerp(
                groundFactor,
                player.IsGrounded ? 1 : 0,
                Time.deltaTime * groundFactorInterpolationSpeed
            );

            footstepsVolume = horizontalSpeed
                * player.settings.footstepsVolumeMultiplier
                * groundFactor
                * player.movementFactor;

            footstepsVolume = Mathf.Min(1, footstepsVolume);

            footstepsInterval = Mathf.Clamp
            (
                1 / (horizontalSpeed * player.settings.footstepsIntervalMultiplier),
                player.settings.footstepsMinIntervalLimit,
                player.settings.footstepsMaxIntervalLimit
            );

            previousVelocityY = player.controller.velocity.y;
        }

        void UpdateClimbingValues()
        {
            var verticalSpeed = Mathf.Abs(player.controller.velocity.y);

            climbingVolume = verticalSpeed
                * player.settings.climbingVolumeMultiplier
                * player.movementFactor;

            climbingInterval = Mathf.Clamp
            (
                1 / (verticalSpeed * player.settings.climbingIntervalMultiplier),
                player.settings.climbingMinIntervalLimit,
                player.settings.climbingMaxIntervalLimit
            );
        }

        void UpdateWindValues()
        {
            var speed = player.groundObjectRigidbody != null
                ? player.groundVelocity.magnitude
                : player.controller.velocity.magnitude;

            windSource.volume = Util.MapRange
            (
                speed,
                player.settings.windVolumeMinSpeed,
                player.settings.windVolumeMaxSpeed,
                0,
                player.settings.windVolumeMaxLimit
            );
        }
    }
}