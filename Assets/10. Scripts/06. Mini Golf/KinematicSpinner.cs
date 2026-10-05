using UnityEngine;

namespace Study3D.MiniGolf
{
    [RequireComponent(typeof(Rigidbody))]
    public class KinematicSpinner : MonoBehaviour
    {
        public Vector3 axis = Vector3.forward;
        public float degreesPerSecond = 60f;

        private Rigidbody body;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
        }

        private void FixedUpdate()
        {
            Quaternion step = Quaternion.AngleAxis(degreesPerSecond * Time.fixedDeltaTime, axis.normalized);
            body.MoveRotation(body.rotation * step);
        }
    }
}
