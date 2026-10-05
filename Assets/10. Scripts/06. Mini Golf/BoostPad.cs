using UnityEngine;

namespace Study3D.MiniGolf
{
    public class BoostPad : MonoBehaviour
    {
        public Vector3 direction = Vector3.forward;
        public float acceleration = 20f;

        private void OnTriggerStay(Collider other)
        {
            Rigidbody body = other.attachedRigidbody;
            if (body != null && !body.isKinematic)
                body.AddForce(transform.TransformDirection(direction.normalized) * acceleration, ForceMode.Acceleration);
        }
    }
}
