using System.Collections;
using UnityEngine;

namespace Study3D.MiniGolf
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider), typeof(GolfBall))]
    public class SnowballPlayer : MonoBehaviour
    {
        [Header("Size")]
        public float initialSize = 0.5f;
        public float minimumSize = 0.5f;
        public float rollingGrowthLimit = 1.5f;
        public float maximumSize = 4f;
        public float growthPerMeter = 0.05f;
        public SnowStageManager stageManager;

        private Rigidbody body;
        private SphereCollider sphere;
        private GolfBall golfBall;
        private Vector3 initialScale;
        private Vector3 previousPosition;
        private Vector3 velocityBeforeCollision;
        private Vector3 lastShotPosition;
        private float lastShotSize;
        private float size;
        private float baseRadius;
        private bool touchedSnow;
        private Coroutine pickupGrowthRoutine;
        private float pickupGrowthTarget;

        public float Size => size;
        public float InitialSize => initialSize;
        public float MinimumSize => minimumSize;
        public float MaximumSize => maximumSize;
        public Rigidbody Body => body;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            sphere = GetComponent<SphereCollider>();
            golfBall = GetComponent<GolfBall>();
            initialScale = transform.localScale;
            baseRadius = sphere.radius * Mathf.Max(initialScale.x, initialScale.z);
            previousPosition = transform.position;
            ResetAt(transform.position);
        }

        private void FixedUpdate()
        {
            if (touchedSnow && body.linearVelocity.sqrMagnitude > 0.01f && size < rollingGrowthLimit)
            {
                float distance = Vector3.Distance(previousPosition, body.position);
                float rollingGrowth = distance * growthPerMeter;
                if (pickupGrowthRoutine != null)
                {
                    if (pickupGrowthTarget < rollingGrowthLimit)
                        pickupGrowthTarget = Mathf.Min(pickupGrowthTarget + rollingGrowth, rollingGrowthLimit);
                }
                else
                    SetSize(Mathf.Min(size + rollingGrowth, rollingGrowthLimit), true);
            }

            touchedSnow = false;
            previousPosition = body.position;
            velocityBeforeCollision = body.linearVelocity;
        }

        private void OnCollisionStay(Collision collision)
        {
            KeepBallOnGround(collision);

            if (collision.collider.GetComponentInParent<SnowGrowthSurface>() != null)
                touchedSnow = true;

            SnowBreakable obstacle = collision.collider.GetComponentInParent<SnowBreakable>();
            if (obstacle != null)
                obstacle.TryBreak(this, velocityBeforeCollision);
        }

        private void OnCollisionEnter(Collision collision)
        {
            KeepBallOnGround(collision);

            SnowBreakable obstacle = collision.collider.GetComponentInParent<SnowBreakable>();
            if (obstacle != null)
                obstacle.TryBreak(this, velocityBeforeCollision);

            SnowBoss boss = collision.collider.GetComponentInParent<SnowBoss>();
            if (boss != null)
                boss.TryHit(this);

            SnowGoalTree goal = collision.collider.GetComponentInParent<SnowGoalTree>();
            if (goal != null)
                goal.TryRescue(this);
        }

        private void KeepBallOnGround(Collision collision)
        {
            if (body.linearVelocity.y <= 0f) return;

            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y > 0.5f)
                {
                    Vector3 velocity = body.linearVelocity;
                    velocity.y = 0f;
                    body.linearVelocity = velocity;
                    return;
                }
            }
        }

        public void CaptureLastShot()
        {
            lastShotPosition = body.position;
            lastShotSize = size;
        }

        public void ChangeSize(float amount)
        {
            StopPickupGrowth();
            SetSize(size + amount, true);
        }

        public void SetSize(float value, bool preserveGroundHeight)
        {
            float nextSize = Mathf.Clamp(value, minimumSize, maximumSize);
            if (Mathf.Approximately(size, nextSize)) return;

            Vector3 velocity = body.linearVelocity;
            Vector3 angularVelocity = body.angularVelocity;
            float previousRadius = baseRadius * size;
            size = nextSize;
            transform.localScale = initialScale * size;

            if (preserveGroundHeight)
            {
                Vector3 position = body.position + Vector3.up * (baseRadius * size - previousRadius);
                body.position = position;
                body.angularVelocity = Vector3.ClampMagnitude(angularVelocity * previousRadius / (baseRadius * size), golfBall.maxAngularSpeed);
            }

            body.linearVelocity = velocity;
            previousPosition = body.position;
        }

        public void ResetAt(Vector3 position)
        {
            StopPickupGrowth();
            size = 0f;
            SetSize(initialSize, false);
            golfBall.PlaceAt(position);
            CaptureLastShot();
            previousPosition = position;
            touchedSnow = false;
        }

        public void RestoreLastShot()
        {
            StopPickupGrowth();
            size = 0f;
            SetSize(lastShotSize, false);
            golfBall.PlaceAt(lastShotPosition);
            CaptureLastShot();
            previousPosition = body.position;
            touchedSnow = false;
        }

        public bool IsMoving => body.linearVelocity.sqrMagnitude > golfBall.stopSpeed * golfBall.stopSpeed;

        public void Absorb(SnowPickup pickup)
        {
            if (!pickup.CanAbsorb) return;
            if (stageManager != null && !stageManager.CanShoot) return;

            float currentTarget = pickupGrowthRoutine != null ? pickupGrowthTarget : size;
            float nextTarget = Mathf.Clamp(currentTarget + pickup.growthAmount, minimumSize, maximumSize);
            StopPickupGrowth();
            pickupGrowthTarget = nextTarget;
            pickupGrowthRoutine = StartCoroutine(AnimatePickupGrowth());
            pickup.Absorb();
        }

        private IEnumerator AnimatePickupGrowth()
        {
            const float duration = 0.5f;
            const float maxScaleStep = 0.1f;
            float startingSize = size;
            float elapsed = 0f;

            while (true)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float nextSize = Mathf.Lerp(startingSize, pickupGrowthTarget, progress);
                SetSize(Mathf.MoveTowards(size, nextSize, maxScaleStep), true);

                if (progress >= 1f && Mathf.Approximately(size, pickupGrowthTarget))
                    break;

                yield return null;
            }

            pickupGrowthRoutine = null;
        }

        private void StopPickupGrowth()
        {
            if (pickupGrowthRoutine == null) return;

            StopCoroutine(pickupGrowthRoutine);
            pickupGrowthRoutine = null;
            pickupGrowthTarget = size;
        }
    }
}
