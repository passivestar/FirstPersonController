using UnityEngine;

namespace FirstPersonController
{
    public class PlayerStateVolume : MonoBehaviour
    {
        public bool global;
        [Tooltip("Player state volumes component if this volume is global")]
        public PlayerStateVolumes playerStateVolumes;

        public enum State
        {
            Walking,
            Flying,
            Swimming,
            Climbing
        }

        public State state = State.Walking;

        [HideInInspector] public PlayerState playerState;

        void Awake()
        {
            playerState = state switch
            {
                State.Walking => PlayerState.Walking,
                State.Flying => PlayerState.Flying,
                State.Swimming => PlayerState.Swimming,
                State.Climbing => PlayerState.Climbing,
                _ => null,
            };
        }

        void OnEnable()
        {
            if (global)
            {
                playerStateVolumes.PlayerEnterStateVolume(gameObject, this);
            }
        }

        void OnDisable()
        {
            if (global)
            {
                playerStateVolumes.PlayerExitStateVolume(gameObject, this);
            }
        }

        void OnTriggerEnter(Collider collider)
        {
            if (collider.gameObject.TryGetComponent<PlayerStateVolumes>(out var volumesComponent))
            {
                volumesComponent.PlayerEnterStateVolume(gameObject, this);
            }
        }

        void OnTriggerExit(Collider collider)
        {
            if (collider.gameObject.TryGetComponent<PlayerStateVolumes>(out var volumesComponent))
            {
                volumesComponent.PlayerExitStateVolume(gameObject, this);
            }
        }
    }
}