using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Study3D.MiniGolf
{
    [RequireComponent(typeof(SphereCollider))]
    public class SnowHeatAttack : MonoBehaviour
    {
        public SnowStageManager stageManager;
        public SnowBoss boss;
        public BoxCollider arenaBounds;
        public Transform[] targetPositions;
        public Transform warningVisual;
        public float radius = 3.2f;
        public float interval = 12f;
        public float warningDuration = 2f;
        public float activeDuration = 3f;

        private readonly HashSet<SnowballPlayer> hitPlayers = new HashSet<SnowballPlayer>();
        private SphereCollider[] attackColliders;
        private Transform[] warningVisuals;
        private bool isHot;
        private int queuedExtraAttacks;

        private void Awake()
        {
            attackColliders = new SphereCollider[targetPositions.Length];
            warningVisuals = new Transform[targetPositions.Length];
            attackColliders[0] = GetComponent<SphereCollider>();
            warningVisuals[0] = warningVisual;

            for (int i = 0; i < targetPositions.Length; i++)
            {
                if (i > 0)
                {
                    attackColliders[i] = gameObject.AddComponent<SphereCollider>();
                    warningVisuals[i] = Instantiate(warningVisual, warningVisual.position, warningVisual.rotation, warningVisual.parent);
                }

                attackColliders[i].isTrigger = true;
                attackColliders[i].radius = radius;
                attackColliders[i].enabled = false;
                warningVisuals[i].gameObject.SetActive(false);
            }
        }

        public void StartPattern()
        {
            StopAllCoroutines();
            StartCoroutine(Pattern());
        }

        public void StopPattern()
        {
            StopAllCoroutines();
            isHot = false;
            queuedExtraAttacks = 0;
            foreach (SphereCollider attackCollider in attackColliders)
                attackCollider.enabled = false;
            if (boss != null) boss.SetPatternAttacking(false);
            foreach (Transform visual in warningVisuals)
                visual.gameObject.SetActive(false);
            if (stageManager != null) stageManager.SetHeatCue(this, 0f);
        }

        private IEnumerator Pattern()
        {
            while (true)
            {
                float remaining = interval;
                while (remaining > 0f && queuedExtraAttacks == 0)
                {
                    remaining -= Time.deltaTime;
                    yield return null;
                }

                if (queuedExtraAttacks > 0)
                    queuedExtraAttacks--;

                yield return AttackSequence();
            }
        }

        private IEnumerator AttackSequence()
        {
            hitPlayers.Clear();

            Bounds bounds = arenaBounds.bounds;
            for (int i = 0; i < targetPositions.Length; i++)
            {
                Vector3 targetPosition = targetPositions[i].position;
                targetPosition.x = Random.Range(bounds.min.x + radius, bounds.max.x - radius);
                targetPosition.z = Random.Range(bounds.min.z + radius, bounds.max.z - radius);
                targetPositions[i].position = targetPosition;

                attackColliders[i].center = transform.InverseTransformPoint(targetPositions[i].position);
                attackColliders[i].radius = radius;
                warningVisuals[i].position = targetPositions[i].position;
                warningVisuals[i].localScale = new Vector3(radius * 2f, warningVisuals[i].localScale.y, radius * 2f);
                warningVisuals[i].gameObject.SetActive(true);
            }

            float remaining = warningDuration;
            while (remaining > 0f)
            {
                if (stageManager != null) stageManager.SetHeatCue(this, remaining);
                remaining -= Time.deltaTime;
                yield return null;
            }

            if (stageManager != null) stageManager.SetHeatCue(this, 0f);
            isHot = true;
            foreach (SphereCollider attackCollider in attackColliders)
                attackCollider.enabled = true;
            if (boss != null) boss.SetPatternAttacking(true);
            yield return new WaitForSeconds(activeDuration);
            isHot = false;
            foreach (SphereCollider attackCollider in attackColliders)
                attackCollider.enabled = false;
            if (boss != null) boss.SetPatternAttacking(false);
            foreach (Transform visual in warningVisuals)
                visual.gameObject.SetActive(false);
        }

        public void TriggerExtraAttack()
        {
            queuedExtraAttacks++;
        }

        private void OnTriggerStay(Collider other)
        {
            if (!isHot) return;
            SnowballPlayer player = other.GetComponentInParent<SnowballPlayer>();
            if (player != null && hitPlayers.Add(player) && stageManager != null)
                stageManager.HitByHeat(player);
        }

        public void ResetPattern()
        {
            StopPattern();
        }
    }
}
