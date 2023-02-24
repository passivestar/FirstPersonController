#if USE_CINEMACHINE
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

namespace FirstPersonController
{
    [RequireComponent(typeof(Player))]
    public class PlayerCameraShakeVolumes : MonoBehaviour
    {
        [SerializeField] float transitionSpeed = 10f;
        Player player;
        List<PlayerCameraShakeVolume> volumes = new();
        PlayerCameraShakeSettings initialSettings = new();
        CinemachineBasicMultiChannelPerlin cameraShake;
        PlayerCameraShakeSettings currentSettings = new();

        void Awake()
        {
            player = GetComponent<Player>();
        }

        void Start()
        {
            var camera = player.cameraObject.GetComponent<CinemachineVirtualCamera>();
            cameraShake = camera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

            if (cameraShake == null)
            {
                Debug.LogError("PlayerCameraShakeVolumes: CinemachineBasicMultiChannelPerlin component not found on camera. Please add noise to the camera and select a noise profile.");
                return;
            }

            initialSettings.shakeAmplitude = cameraShake.m_AmplitudeGain;
            initialSettings.shakeFrequency = cameraShake.m_FrequencyGain;
        }

        public void PlayerEnterCameraShakeVolume(GameObject gameObject, PlayerCameraShakeVolume volume)
        {
            volumes.Add(volume);
        }

        public void PlayerExitCameraShakeVolume(GameObject gameObject, PlayerCameraShakeVolume volume)
        {
            volumes.Remove(volume);
        }

        void Update()
        {
            if (volumes.Count > 0)
            {
                currentSettings.LerpTo(volumes[volumes.Count - 1].settings, transitionSpeed);
            }
            else
            {
                currentSettings.LerpTo(initialSettings, transitionSpeed);
            }

            cameraShake.m_AmplitudeGain = currentSettings.shakeAmplitude;
            cameraShake.m_FrequencyGain = currentSettings.shakeFrequency;
        }
    }
}
#endif