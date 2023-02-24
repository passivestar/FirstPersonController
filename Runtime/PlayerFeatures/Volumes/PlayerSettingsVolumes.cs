using System.Collections.Generic;
using UnityEngine;

namespace FirstPersonController
{
    [RequireComponent(typeof(Player))]
    public class PlayerSettingsVolumes : MonoBehaviour
    {
        Player player;
        List<PlayerSettingsVolume> volumes = new();

        PlayerSettings initialSettings;

        void Awake()
        {
            player = GetComponent<Player>();
        }

        void Start()
        {
            initialSettings = player.settings;
        }

        public void PlayerEnterSettingsVolume(GameObject gameObject, PlayerSettingsVolume volume)
        {
            volumes.Add(volume);
            player.settings = volume.settings;
        }

        public void PlayerExitSettingsVolume(GameObject gameObject, PlayerSettingsVolume volume)
        {
            volumes.Remove(volume);
            player.settings = volumes.Count > 0
                ? volumes[volumes.Count - 1].settings
                : initialSettings;
        }
    }
}