using UnityEngine;

namespace Study3D.MiniGolf
{
    [RequireComponent(typeof(Rigidbody))]
    public class KinematicShuttle : MonoBehaviour
    {
        public Vector3 offset = Vector3.right;
        public float cycleTime = 3f;

        private Rigidbody body;
        private Vector3 startPosition;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            startPosition = body.position;
        }

        private void FixedUpdate()
        {
            float progress = Mathf.PingPong(Time.time * 2f / cycleTime, 1f);
            body.MovePosition(startPosition + offset * progress);
        }
    }
}
