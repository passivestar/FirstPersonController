using UnityEngine;
using UnityEngine.Events;

namespace FirstPersonController
{

    // This just adds grabbing events to the object, all of the objects with
    // rigidbodies are considred grabbable
    public class PlayerGrabbable : MonoBehaviour
    {
        public UnityEvent onLookAt;
        public UnityEvent onLookAway;
        public UnityEvent onGrabbed;
        public UnityEvent onReleased;
    }
}