using UnityEngine;

namespace FirstPersonController
{
    public class PlayerSettingsVolume : MonoBehaviour
    {
        public bool global;
        [Tooltip("Player settings volumes component if this volume is global")]
        public PlayerSettingsVolumes playerSettingsVolumes;
        public PlayerSettings settings;

        void OnEnable()
        {
            if (global)
            {
                playerSettingsVolumes.PlayerEnterSettingsVolume(gameObject, this);
            }
        }

        void OnDisable()
        {
            if (global)
            {
                playerSettingsVolumes.PlayerExitSettingsVolume(gameObject, this);
            }
        }

        void OnTriggerEnter(Collider collider)
        {
            if (collider.gameObject.TryGetComponent<PlayerSettingsVolumes>(out var volumesComponent))
            {
                volumesComponent.PlayerEnterSettingsVolume(gameObject, this);
            }
        }

        void OnTriggerExit(Collider collider)
        {
            if (collider.gameObject.TryGetComponent<PlayerSettingsVolumes>(out var volumesComponent))
            {
                volumesComponent.PlayerExitSettingsVolume(gameObject, this);
            }
        }
    }
}