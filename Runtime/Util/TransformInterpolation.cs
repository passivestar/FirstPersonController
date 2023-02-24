// Some transforms have to update on FixedUpdate in Unity
// This script interpolates between fixed update transforms
// on every frame.

using UnityEngine;

namespace FirstPersonController
{
    public class TransformInterpolation : MonoBehaviour
    {
        public Transform target;
        public float maxDistance = 1f;

        (Vector3, Quaternion) oldTransform, newTransform;

        void Start()
        {
            // If the target is not specified move mesh renderer and filter
            // to a new gameobject and interpolate it to current object
            if (target == null)
            {
                var meshRenderer = GetComponent<MeshRenderer>();
                var meshFilter = GetComponent<MeshFilter>();
                var collider = GetComponent<Collider>();

                var newObject = new GameObject();
                newObject.transform.localScale = transform.localScale;

                var interpolation = newObject.AddComponent<TransformInterpolation>();
                interpolation.target = transform;

                Util.CopyComponent(meshRenderer, newObject);
                Util.CopyComponent(meshFilter, newObject);
                newObject.name = $"_TransformInterpolation_{name}";

                Destroy(meshRenderer);
                Destroy(meshFilter);
                Destroy(this);
            }
            else
            {
                oldTransform = newTransform = Util.GetTransformData(target.transform);
            }
        }

        void FixedUpdate()
        {
            oldTransform = newTransform;
            newTransform = Util.GetTransformData(target.transform);
        }

        void Update()
        {
            // See how far into the frame we are
            var fraction = (Time.time - Time.fixedTime) / Time.fixedDeltaTime;
            var (oldPosition, oldRotation) = oldTransform;
            var (newPosition, newRotation) = newTransform;
            var distance = Vector3.Distance(oldPosition, newPosition);
            if (distance < maxDistance)
            {
                transform.position = Vector3.Lerp(oldPosition, newPosition, fraction);
                transform.rotation = Quaternion.Slerp(oldRotation, newRotation, fraction);
            }
            else
            {
                transform.position = newPosition;
                transform.rotation = newRotation;
            }
        }
    }
}