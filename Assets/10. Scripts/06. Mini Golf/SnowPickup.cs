using UnityEngine;

namespace Study3D.MiniGolf
{
    [RequireComponent(typeof(Collider))]
    public class SnowPickup : MonoBehaviour
    {
        public float growthAmount = 0.375f;
        public bool canAbsorb = true;

        private Collider pickupCollider;
        private Rigidbody body;
        private Vector3 landingPosition;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private bool initiallyActive;

        public bool CanAbsorb => canAbsorb;

        private void Awake()
        {
            pickupCollider = GetComponent<Collider>();
            body = GetComponent<Rigidbody>();
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            initiallyActive = gameObject.activeSelf;
        }

        private void OnTriggerEnter(Collider other)
        {
            SnowballPlayer player = other.GetComponentInParent<SnowballPlayer>();
            if (player != null)
                player.Absorb(this);
        }

        private void OnCollisionStay(Collision collision)
        {
            if (canAbsorb || body == null) return;

            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y > 0.5f)
                {
                    Land();
                    return;
                }
            }
        }

        public void PrepareToFall(Vector3 velocity, Vector3 targetPosition)
        {
            canAbsorb = false;
            landingPosition = targetPosition;
            pickupCollider.isTrigger = false;
            body.isKinematic = false;
            body.useGravity = true;
            body.linearDamping = 0f;
            body.linearVelocity = velocity;
            body.angularVelocity = Vector3.zero;

            SnowBoss boss = FindFirstObjectByType<SnowBoss>();
            if (boss != null)
            {
                foreach (Collider bossCollider in boss.GetComponentsInChildren<Collider>())
                    Physics.IgnoreCollision(pickupCollider, bossCollider);
            }
        }

        private void Land()
        {
            Vector3 position = body.position;
            position.x = landingPosition.x;
            position.z = landingPosition.z;
            body.position = position;
            canAbsorb = true;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            pickupCollider.isTrigger = true;
        }

        public void Absorb()
        {
            gameObject.SetActive(false);
        }

        public void ResetPickup()
        {
            gameObject.SetActive(initiallyActive);
            transform.SetPositionAndRotation(initialPosition, initialRotation);
            canAbsorb = true;
            pickupCollider.isTrigger = true;
            if (body != null)
            {
                body.isKinematic = true;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }
    }
}
