using System;
using UnityEngine;

namespace FirstPersonController
{
    [RequireComponent(typeof(Rigidbody))]
    public class CollisionEvents : MonoBehaviour
    {
        public event Action<Collision> onCollisionEnter;
        public event Action<Collision> onCollisionExit;
        public event Action<Collision> onCollisionStay;

        void OnCollisionEnter(Collision collision) => onCollisionEnter?.Invoke(collision);
        void OnCollisionExit(Collision collision) => onCollisionExit?.Invoke(collision);
        void OnCollisionStay(Collision collision) => onCollisionStay?.Invoke(collision);
    }
}