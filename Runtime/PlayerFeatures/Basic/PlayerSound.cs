using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FirstPersonController
{
    [RequireComponent(typeof(Player))]
    public class PlayerSound : MonoBehaviour
    {
        List<AudioClip> defaultFootstepSounds;
        AudioSource source;
        AudioSource bodySource;
        AudioSource windSource;

        float footstepsInterval;
        float footstepsVolume;
        float previousVelocityY;

        float groundFactor = 0f;
        float groundFactorInterpolationSpeed = 30f;

        PlayerGrabbing playerGrabbing;

        IEnumerator footstepsCoroutine;
        IEnumerator ladderCoroutine;

        Player player;

        void Awake()
        {
            player = GetComponent<Player>();
            source = gameObject.AddComponent<AudioSource>();
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

            if (gameObject.TryGetComponent<PlayerGrabbing>(out playerGrabbing))
            {
                playerGrabbing.onGameObjectGrabbed.AddListener(OnGameObjectGrabbed);
            }
        }

        void OnDisable()
        {
            player.onGroundStateChange.RemoveListener(OnGroundStateChange);
            if (playerGrabbing != null)
            {
                playerGrabbing.onGameObjectGrabbed.RemoveListener(OnGameObjectGrabbed);
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
            }
            else
            {
                if (footstepsCoroutine != null)
                {
                    StopCoroutine(footstepsCoroutine);
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
                source.clip = list[Random.Range(0, list.Count)];
                source.pitch = 1 + Random.Range(-player.settings.footstepsPitchRange / 2, player.settings.footstepsPitchRange / 2);
                source.volume = volume;
                source.Play();
            }
        }

        void FixedUpdate()
        {
            var horizontalSpeed = Vector3.ProjectOnPlane(player.controller.velocity, Vector3.up).magnitude;

            // Interpolate ground factor
            groundFactor = Mathf.Lerp(
                groundFactor,
                player.IsGrounded ? 1 : 0,
                Time.deltaTime * groundFactorInterpolationSpeed
            );

            // Detect landing
            if (
                player.IsGrounded
                && Mathf.Abs(player.controller.velocity.y) < .2f
                && previousVelocityY < -player.settings.landingVelocityThreshold
            )
            {
                var vol = Util.MapRange(previousVelocityY, 0, -4f, 0, player.settings.landingMaxVolume);
                PlayFootstepSound(vol);
            }

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

            previousVelocityY = player.controller.velocity.y;
        }
    }
}