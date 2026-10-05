using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Study3D.MiniGolf
{
    public class SnowSupply : MonoBehaviour
    {
        public SnowStageManager stageManager;
        public GameObject snowballPrefab;
        public Transform launchPoint;
        public Transform[] landingPoints;
        public GameObject landingMarkerPrefab;
        public bool dropPeriodically;
        public float interval = 14f;
        public float warningDuration = 2f;
        public float flightDuration = 1.5f;
        public float dropHeight = 8f;
        public float growthAmount = 0.8f;
        public int maximumActiveSnowballs = 5;

        private readonly List<SnowPickup> activeSnowballs = new List<SnowPickup>();
        private int nextLandingPoint;

        public void Begin()
        {
            StopAllCoroutines();
            if (dropPeriodically)
                StartCoroutine(DropLoop());
        }

        private IEnumerator DropLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(interval);
                yield return DropSequence(ChooseLandingPoint());
            }
        }

        public void ThrowAfterBossHit()
        {
            StartCoroutine(DropSequence(ChooseLandingPoint()));
        }

        private IEnumerator DropSequence(Transform landingPoint)
        {
            if (landingMarkerPrefab != null)
            {
                GameObject marker = Instantiate(landingMarkerPrefab, landingPoint.position, landingPoint.rotation);
                yield return new WaitForSeconds(warningDuration);
                Destroy(marker);
            }
            else
            {
                yield return new WaitForSeconds(warningDuration);
            }

            activeSnowballs.RemoveAll(snowball => snowball == null);
            while (activeSnowballs.Count >= maximumActiveSnowballs)
            {
                Destroy(activeSnowballs[0].gameObject);
                activeSnowballs.RemoveAt(0);
            }

            Vector3 start = (launchPoint != null ? launchPoint.position : landingPoint.position + Vector3.up * dropHeight);
            GameObject instance = Instantiate(snowballPrefab, start, Quaternion.identity);
            SnowPickup pickup = instance.GetComponent<SnowPickup>();
            pickup.growthAmount = growthAmount;
            pickup.PrepareToFall(GetFlightVelocity(start, landingPoint.position));
            activeSnowballs.Add(pickup);
        }

        private Vector3 GetFlightVelocity(Vector3 start, Vector3 target)
        {
            float duration = flightDuration;
            Vector3 displacement = target - start;
            Vector3 velocity = new Vector3(displacement.x / duration, displacement.y / duration, displacement.z / duration);
            velocity.y += Physics.gravity.magnitude * duration * 0.5f;
            return velocity;
        }

        private Transform ChooseLandingPoint()
        {
            Transform target = landingPoints[nextLandingPoint % landingPoints.Length];
            nextLandingPoint++;
            return target;
        }

        public void ResetSupply()
        {
            StopAllCoroutines();
            foreach (SnowPickup snowball in activeSnowballs)
                if (snowball != null) Destroy(snowball.gameObject);
            activeSnowballs.Clear();
            nextLandingPoint = 0;
        }
    }
}
