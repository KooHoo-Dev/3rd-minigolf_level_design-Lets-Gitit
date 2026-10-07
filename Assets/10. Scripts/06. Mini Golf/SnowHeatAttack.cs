using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Study3D.MiniGolf
{
    [RequireComponent(typeof(SphereCollider))]
    public class SnowHeatAttack : MonoBehaviour
    {
        public SnowStageManager stageManager;
        public Transform[] targetPositions;
        public Transform warningVisual;
        public float radius = 2f;
        public float interval = 12f;
        public float warningDuration = 2f;
        public float activeDuration = 3f;
        public float sizeLoss = 0.35f;

        private readonly HashSet<SnowballPlayer> hitPlayers = new HashSet<SnowballPlayer>();
        private SphereCollider attackCollider;
        private bool isHot;
        private int nextPosition;

        private void Awake()
        {
            attackCollider = GetComponent<SphereCollider>();
            attackCollider.isTrigger = true;
            attackCollider.radius = radius;
            attackCollider.enabled = false;
            if (warningVisual != null) warningVisual.gameObject.SetActive(false);
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
            attackCollider.enabled = false;
            if (warningVisual != null) warningVisual.gameObject.SetActive(false);
            if (stageManager != null) stageManager.SetHeatCue(this, 0f);
        }

        private IEnumerator Pattern()
        {
            while (true)
            {
                yield return new WaitForSeconds(interval);
                Transform target = targetPositions[nextPosition % targetPositions.Length];
                nextPosition++;
                transform.position = target.position;
                attackCollider.radius = radius;
                hitPlayers.Clear();

                if (warningVisual != null)
                {
                    warningVisual.position = target.position;
                    warningVisual.localScale = new Vector3(radius * 2f, warningVisual.localScale.y, radius * 2f);
                    warningVisual.gameObject.SetActive(true);
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
                attackCollider.enabled = true;
                yield return new WaitForSeconds(activeDuration);
                isHot = false;
                attackCollider.enabled = false;
                if (warningVisual != null) warningVisual.gameObject.SetActive(false);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (!isHot) return;
            SnowballPlayer player = other.GetComponentInParent<SnowballPlayer>();
            if (player != null && hitPlayers.Add(player) && stageManager != null)
                stageManager.HitByHeat(player, sizeLoss);
        }

        public void ResetPattern()
        {
            StopPattern();
            nextPosition = 0;
        }
    }
}
