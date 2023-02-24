using System.Collections.Generic;
using UnityEngine;

namespace FirstPersonController
{
    [RequireComponent(typeof(Player))]
    public class PlayerStateVolumes : MonoBehaviour
    {
        Player player;
        List<PlayerStateVolume> volumes = new();

        PlayerState initialState;

        void Awake()
        {
            player = GetComponent<Player>();
        }

        void Start()
        {
            initialState = player.GetState();
        }

        public void PlayerEnterStateVolume(GameObject gameObject, PlayerStateVolume volume)
        {
            volumes.Add(volume);
            player.SetState(volume.playerState);
        }

        public void PlayerExitStateVolume(GameObject gameObject, PlayerStateVolume volume)
        {
            volumes.Remove(volume);
            player.SetState(volumes.Count > 0
                ? volumes[volumes.Count - 1].playerState
                : initialState
            );
        }
    }
}