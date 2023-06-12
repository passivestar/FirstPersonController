#if USE_CINEMACHINE
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstPersonController
{
    [Serializable]
    public struct PlayerCameraShakeSettings
    {
        public float shakeAmplitude;
        public float shakeFrequency;

        public void LerpTo(PlayerCameraShakeSettings target, float speed)
        {
            shakeAmplitude = Mathf.Lerp(shakeAmplitude, target.shakeAmplitude, Time.deltaTime * speed);
            shakeFrequency = Mathf.Lerp(shakeFrequency, target.shakeFrequency, Time.deltaTime * speed);
        }
    }

    public class PlayerCameraShakeVolume : MonoBehaviour
    {
        public bool global;
        [Tooltip("Player camera shake volumes component if this volume is global")]
        public PlayerCameraShakeVolumes playerCameraShakeVolumes;
        public PlayerCameraShakeSettings settings = new();

        // We have to store a list of PlayerCameraShakeVolumes components because
        // OnTriggerExit is not called when this volume is destroyed
        List<PlayerCameraShakeVolumes> playerCameraShakeVolumesList = new();

        void OnEnable()
        {
            if (global)
            {
                playerCameraShakeVolumes.PlayerEnterCameraShakeVolume(gameObject, this);
            }

            foreach (var volumesComponent in playerCameraShakeVolumesList)
            {
                volumesComponent.PlayerEnterCameraShakeVolume(gameObject, this);
            }
        }

        void OnDisable()
        {
            if (global)
            {
                playerCameraShakeVolumes.PlayerExitCameraShakeVolume(gameObject, this);
            }

            foreach (var volumesComponent in playerCameraShakeVolumesList)
            {
                volumesComponent.PlayerExitCameraShakeVolume(gameObject, this);
            }
        }

        void OnTriggerEnter(Collider collider)
        {
            if (collider.gameObject.TryGetComponent<PlayerCameraShakeVolumes>(out var volumesComponent))
            {
                playerCameraShakeVolumesList.Add(volumesComponent);
                volumesComponent.PlayerEnterCameraShakeVolume(gameObject, this);
            }
        }

        void OnTriggerExit(Collider collider)
        {
            if (collider.gameObject.TryGetComponent<PlayerCameraShakeVolumes>(out var volumesComponent))
            {
                playerCameraShakeVolumesList.Remove(volumesComponent);
                volumesComponent.PlayerExitCameraShakeVolume(gameObject, this);
            }
        }
    }
}
#endif