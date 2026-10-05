using UnityEngine;

namespace Study3D.MiniGolf
{
    [RequireComponent(typeof(Collider))]
    public class SnowFallTrigger : MonoBehaviour
    {
        public SnowStageManager stageManager;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            SnowballPlayer player = other.GetComponentInParent<SnowballPlayer>();
            if (player != null && stageManager != null)
                stageManager.ReturnToLastShot();
        }
    }
}
