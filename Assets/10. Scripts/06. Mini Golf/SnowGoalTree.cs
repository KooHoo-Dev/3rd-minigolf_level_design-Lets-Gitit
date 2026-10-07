using System.Collections;
using UnityEngine;

namespace Study3D.MiniGolf
{
    public class SnowGoalTree : MonoBehaviour
    {
        public SnowStageManager stageManager;
        public Transform[] companions;
        public Transform[] rescuePositions;
        public float rescueDuration = 1.5f;

        private Vector3[] initialPositions;
        private Vector3 initialPenguinPosition;
        private Quaternion initialPenguinRotation;
        private Vector3 launchDirection;
        private Animator penguinAnimator;
        private Rigidbody penguinBody;
        private bool rescued;

        private void Awake()
        {
            penguinAnimator = GetComponentInChildren<Animator>(true);
            penguinBody = GetComponentInChildren<Rigidbody>(true);
            if (penguinBody != null)
            {
                initialPenguinPosition = penguinBody.transform.localPosition;
                initialPenguinRotation = penguinBody.transform.localRotation;
                penguinBody.isKinematic = true;
                penguinBody.useGravity = false;
            }

            launchDirection = transform.forward;
            initialPositions = new Vector3[companions.Length];
            for (int i = 0; i < companions.Length; i++)
                initialPositions[i] = companions[i].position;
        }

        public void TryRescue(SnowballPlayer player)
        {
            if (rescued || !player.IsMoving || stageManager == null || !stageManager.CanShoot) return;

            if (penguinBody != null)
            {
                launchDirection = penguinBody.position - player.Body.position;
                launchDirection.y = 0f;
                if (launchDirection.sqrMagnitude > 0.01f)
                    launchDirection.Normalize();
                else
                    launchDirection = transform.forward;
            }

            stageManager.RescueGoal(this);
        }

        public IEnumerator Rescue()
        {
            rescued = true;
            if (penguinAnimator != null)
            {
                penguinAnimator.ResetTrigger("Idle2");
                penguinAnimator.SetTrigger("Jump");
            }

            if (penguinBody != null)
            {
                penguinBody.isKinematic = false;
                penguinBody.useGravity = true;
                penguinBody.AddForce(launchDirection * 20f + Vector3.up * 14f, ForceMode.VelocityChange);
                penguinBody.AddTorque(new Vector3(12f, 0f, 18f), ForceMode.VelocityChange);
            }

            Vector3[] starts = new Vector3[companions.Length];
            Vector3[] targets = new Vector3[companions.Length];

            for (int i = 0; i < companions.Length; i++)
            {
                starts[i] = companions[i].position;
                targets[i] = i < rescuePositions.Length && rescuePositions[i] != null
                    ? rescuePositions[i].position
                    : starts[i] + Vector3.down;
            }

            float elapsed = 0f;
            while (elapsed < rescueDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / rescueDuration);
                for (int i = 0; i < companions.Length; i++)
                    companions[i].position = Vector3.Lerp(starts[i], targets[i], progress);
                yield return null;
            }
        }

        public void ResetGoal()
        {
            rescued = false;
            launchDirection = transform.forward;
            if (penguinBody != null)
            {
                penguinBody.isKinematic = true;
                penguinBody.useGravity = false;
                penguinBody.linearVelocity = Vector3.zero;
                penguinBody.angularVelocity = Vector3.zero;
                penguinBody.transform.localPosition = initialPenguinPosition;
                penguinBody.transform.localRotation = initialPenguinRotation;
            }

            if (penguinAnimator != null)
            {
                penguinAnimator.ResetTrigger("Jump");
                penguinAnimator.SetTrigger("Idle2");
            }

            for (int i = 0; i < companions.Length; i++)
                companions[i].position = initialPositions[i];
        }
    }
}
