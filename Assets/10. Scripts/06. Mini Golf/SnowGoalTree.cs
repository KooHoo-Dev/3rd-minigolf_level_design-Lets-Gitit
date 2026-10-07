using System.Collections;
using UnityEngine;

namespace Study3D.MiniGolf
{
    public class SnowGoalTree : MonoBehaviour
    {
        public SnowStageManager stageManager;
        public float rescueDuration = 1.5f;

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
        }

        public void TryRescue(SnowballPlayer player)
        {
            if (rescued || stageManager == null || !stageManager.CanShoot) return;

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
                penguinAnimator.Play("Jump_Emperor", 0, 0f);
            }

            if (penguinBody != null)
            {
                penguinBody.isKinematic = false;
                penguinBody.useGravity = true;
                penguinBody.AddForce(launchDirection * 20f + Vector3.up * 14f, ForceMode.VelocityChange);
                penguinBody.AddTorque(new Vector3(12f, 0f, 18f), ForceMode.VelocityChange);
            }

            yield return new WaitForSeconds(rescueDuration);
        }

        public void ResetGoal()
        {
            rescued = false;
            launchDirection = transform.forward;
            if (penguinBody != null)
            {
                penguinBody.isKinematic = false;
                penguinBody.useGravity = false;
                penguinBody.linearVelocity = Vector3.zero;
                penguinBody.angularVelocity = Vector3.zero;
                penguinBody.transform.localPosition = initialPenguinPosition;
                penguinBody.transform.localRotation = initialPenguinRotation;
                penguinBody.isKinematic = true;
            }

            if (penguinAnimator != null)
            {
                penguinAnimator.ResetTrigger("Jump");
                penguinAnimator.ResetTrigger("Dead");
                penguinAnimator.ResetTrigger("Idle2");
                penguinAnimator.Play("Idle2_Emperor", 0, 0f);
            }

        }
    }
}
