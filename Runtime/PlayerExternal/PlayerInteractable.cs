using UnityEngine;
using UnityEngine.Events;

namespace FirstPersonController
{
    public class PlayerInteractable : MonoBehaviour
    {
        public bool lockInputOnInteract = false;
        public UnityEvent<RaycastHit> onLookAt;
        public UnityEvent onLookAway;
        public UnityEvent<RaycastHit> onStarted;
        public UnityEvent<RaycastHit> onPerformed;
        public UnityEvent<RaycastHit, Vector2, Vector2> onDrag;
    }
}