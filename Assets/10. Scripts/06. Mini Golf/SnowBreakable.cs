using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Study3D.MiniGolf
{
    public class SnowBreakable : MonoBehaviour
    {
        public bool canBreak = true;
        public bool preserveSpeedOnBreak;
        public float requiredSize = 0.75f;
        public float minimumImpactSpeed = 3f;
        public float sizeCost;
        public GameObject breakEffect;

        private Collider[] obstacleColliders;
        private bool[] initialColliderStates;
        private Renderer[] obstacleRenderers;
        private bool[] initialRendererStates;
        private bool broken;
        private Vector3 initialScale;
        private Quaternion initialLocalRotation;
        private Coroutine breakRoutine;
        private readonly List<BreakDebris> breakDebris = new List<BreakDebris>();

        private sealed class BreakDebris
        {
            public Transform transform;
            public Vector3 velocity;
            public Vector3 rotationSpeed;
            public Vector3 initialScale;
        }

        private void Awake()
        {
            initialScale = transform.localScale;
            initialLocalRotation = transform.localRotation;

            obstacleColliders = GetComponentsInChildren<Collider>(true);
            initialColliderStates = new bool[obstacleColliders.Length];
            for (int i = 0; i < obstacleColliders.Length; i++)
                initialColliderStates[i] = obstacleColliders[i].enabled;

            obstacleRenderers = GetComponentsInChildren<Renderer>(true);
            initialRendererStates = new bool[obstacleRenderers.Length];
            for (int i = 0; i < obstacleRenderers.Length; i++)
            {
                initialRendererStates[i] = obstacleRenderers[i].enabled;
                obstacleRenderers[i].material.color = preserveSpeedOnBreak
                    ? new Color(1f, 0.78f, 0.05f)
                    : new Color(0.45f, 0.23f, 0.1f);
            }
        }

        public void TryBreak(SnowballPlayer player, Vector3 velocityBeforeCollision)
        {
            Vector3 impactVelocity = Vector3.ProjectOnPlane(velocityBeforeCollision, Vector3.up);
            if (broken || !canBreak || player.Size < requiredSize || impactVelocity.magnitude < minimumImpactSpeed) return;

            broken = true;
            player.ChangeSize(-sizeCost);
            if (preserveSpeedOnBreak)
                player.Body.linearVelocity = impactVelocity;

            foreach (Collider obstacleCollider in obstacleColliders)
                obstacleCollider.enabled = false;

            SpawnBreakDebris(velocityBeforeCollision);

            if (breakEffect != null)
                Instantiate(breakEffect, transform.position, transform.rotation);

            breakRoutine = StartCoroutine(AnimateBreak());
        }

        public void ResetObstacle()
        {
            StopBreakAnimation();
            broken = false;
            transform.localScale = initialScale;
            transform.localRotation = initialLocalRotation;
            for (int i = 0; i < obstacleColliders.Length; i++)
                obstacleColliders[i].enabled = initialColliderStates[i];
            for (int i = 0; i < obstacleRenderers.Length; i++)
                obstacleRenderers[i].enabled = initialRendererStates[i];
        }

        private void OnDisable()
        {
            StopBreakAnimation();
        }

        private void SpawnBreakDebris(Vector3 incomingVelocity)
        {
            Renderer materialSource = null;
            Bounds bounds = default;
            bool hasBounds = false;

            foreach (Renderer obstacleRenderer in obstacleRenderers)
            {
                if (!obstacleRenderer) continue;

                if (!hasBounds)
                {
                    bounds = obstacleRenderer.bounds;
                    hasBounds = true;
                }
                else
                    bounds.Encapsulate(obstacleRenderer.bounds);

                if (materialSource == null && obstacleRenderer.enabled && obstacleRenderer.sharedMaterial != null)
                    materialSource = obstacleRenderer;
            }

            if (!hasBounds) return;

            Material debrisMaterial = materialSource != null ? materialSource.sharedMaterial : null;
            float objectSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            float chipSize = Mathf.Clamp(objectSize * 0.09f, 0.015f, 0.35f);
            float burstSpeed = Mathf.Clamp(objectSize * 1.25f, 0.7f, 3f);
            Vector3 impactDirection = Vector3.ProjectOnPlane(incomingVelocity, Vector3.up).normalized;

            for (int i = 0; i < 7; i++)
            {
                float angle = Mathf.PI * 2f * i / 7f + Random.Range(-0.25f, 0.25f);
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                direction = (direction + impactDirection * 0.25f).normalized;

                GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                piece.name = "Break Debris";
                piece.transform.position = bounds.center + new Vector3(
                    Random.Range(-bounds.extents.x * 0.12f, bounds.extents.x * 0.12f),
                    Random.Range(-bounds.extents.y * 0.12f, bounds.extents.y * 0.12f),
                    Random.Range(-bounds.extents.z * 0.12f, bounds.extents.z * 0.12f));
                piece.transform.rotation = Random.rotation;
                piece.transform.localScale = new Vector3(
                    chipSize * Random.Range(0.65f, 1.35f),
                    chipSize * Random.Range(0.65f, 1.35f),
                    chipSize * Random.Range(0.65f, 1.35f));

                Collider pieceCollider = piece.GetComponent<Collider>();
                if (pieceCollider != null) pieceCollider.enabled = false;

                Renderer pieceRenderer = piece.GetComponent<Renderer>();
                if (debrisMaterial != null) pieceRenderer.sharedMaterial = debrisMaterial;

                breakDebris.Add(new BreakDebris
                {
                    transform = piece.transform,
                    velocity = direction * Random.Range(burstSpeed * 0.45f, burstSpeed)
                        + Vector3.up * Random.Range(burstSpeed * 0.5f, burstSpeed),
                    rotationSpeed = Random.insideUnitSphere * 540f,
                    initialScale = piece.transform.localScale
                });
            }
        }

        private IEnumerator AnimateBreak()
        {
            const float shakeDuration = 0.12f;
            const float shrinkDuration = 0.22f;
            const float debrisDuration = 0.45f;
            float elapsed = 0f;

            while (elapsed < debrisDuration)
            {
                float deltaTime = Time.deltaTime;
                elapsed += deltaTime;

                if (elapsed < shakeDuration)
                {
                    float shake = Mathf.Sin(elapsed * 75f) * 8f;
                    transform.localRotation = initialLocalRotation * Quaternion.Euler(shake, 0f, -shake);
                }
                else
                {
                    transform.localRotation = initialLocalRotation;
                    float shrink = Mathf.Clamp01((elapsed - shakeDuration) / shrinkDuration);
                    transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, shrink);
                }

                float debrisFade = Mathf.Clamp01((elapsed - 0.2f) / (debrisDuration - 0.2f));
                foreach (BreakDebris piece in breakDebris)
                {
                    if (piece.transform == null) continue;

                    piece.velocity += Vector3.down * 8f * deltaTime;
                    piece.transform.position += piece.velocity * deltaTime;
                    piece.transform.Rotate(piece.rotationSpeed * deltaTime, Space.Self);
                    piece.transform.localScale = Vector3.Lerp(piece.initialScale, Vector3.zero, debrisFade);
                }

                yield return null;
            }

            foreach (Renderer obstacleRenderer in obstacleRenderers)
                obstacleRenderer.enabled = false;

            breakRoutine = null;
            ClearBreakDebris();
        }

        private void StopBreakAnimation()
        {
            if (breakRoutine != null)
            {
                StopCoroutine(breakRoutine);
                breakRoutine = null;
            }

            ClearBreakDebris();
        }

        private void ClearBreakDebris()
        {
            foreach (BreakDebris piece in breakDebris)
                if (piece.transform != null) Destroy(piece.transform.gameObject);
            breakDebris.Clear();
        }
    }
}
