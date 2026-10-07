using UnityEngine;

namespace Study3D.MiniGolf
{
    public class SnowBreakable : MonoBehaviour
    {
        public bool canBreak = true;
        public float requiredSize = 1.5f;
        public float sizeCost;
        public GameObject breakEffect;

        private Collider[] obstacleColliders;
        private bool[] initialColliderStates;
        private Renderer[] obstacleRenderers;
        private bool[] initialRendererStates;
        private bool broken;

        private void Awake()
        {
            obstacleColliders = GetComponentsInChildren<Collider>(true);
            initialColliderStates = new bool[obstacleColliders.Length];
            for (int i = 0; i < obstacleColliders.Length; i++)
                initialColliderStates[i] = obstacleColliders[i].enabled;

            obstacleRenderers = GetComponentsInChildren<Renderer>(true);
            initialRendererStates = new bool[obstacleRenderers.Length];
            for (int i = 0; i < obstacleRenderers.Length; i++)
                initialRendererStates[i] = obstacleRenderers[i].enabled;
        }

        public void TryBreak(SnowballPlayer player)
        {
            if (broken || !canBreak || player.Size < requiredSize) return;

            broken = true;
            player.ChangeSize(-sizeCost);
            foreach (Collider obstacleCollider in obstacleColliders)
                obstacleCollider.enabled = false;
            foreach (Renderer obstacleRenderer in obstacleRenderers)
                obstacleRenderer.enabled = false;

            if (breakEffect != null)
                Instantiate(breakEffect, transform.position, transform.rotation);
        }

        public void ResetObstacle()
        {
            broken = false;
            for (int i = 0; i < obstacleColliders.Length; i++)
                obstacleColliders[i].enabled = initialColliderStates[i];
            for (int i = 0; i < obstacleRenderers.Length; i++)
                obstacleRenderers[i].enabled = initialRendererStates[i];
        }
    }
}
