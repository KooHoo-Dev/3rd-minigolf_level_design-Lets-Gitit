using System.Collections;
using UnityEngine;

namespace Study3D.MiniGolf
{
    public class SnowBoss : MonoBehaviour
    {
        public SnowStageManager stageManager;
        public SnowSupply hitSupply;
        public float maximumHealth = 100f;
        public float damagePerSize = 20f;
        public float hitReactionDuration = 0.25f;
        public float hitKnockbackSpeed = 24f;
        public float defeatedColorValue = 0.3f;

        private float health;
        private int lastDamagingShot = -1;
        private bool defeated;
        private Animator animator;
        private Rigidbody body;
        private Renderer[] renderers;
        private Color[][] originalColors;
        private Vector3 initialPosition;
        private Quaternion initialRotation;

        public float Health => health;
        public float HealthRatio => maximumHealth <= 0f ? 0f : health / maximumHealth;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            body = GetComponent<Rigidbody>();
            if (body == null)
                body = gameObject.AddComponent<Rigidbody>();
            initialPosition = transform.localPosition;
            initialRotation = transform.localRotation;
            renderers = GetComponentsInChildren<Renderer>();
            originalColors = new Color[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] materials = renderers[i].sharedMaterials;
                originalColors[i] = new Color[materials.Length];
                for (int j = 0; j < materials.Length; j++)
                    originalColors[i][j] = materials[j].HasProperty("_BaseColor") ? materials[j].GetColor("_BaseColor") : Color.white;
            }
            ResetBoss();
        }

        public void ResetBoss()
        {
            health = maximumHealth;
            defeated = false;
            lastDamagingShot = -1;
            body.isKinematic = false;
            body.useGravity = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            transform.localPosition = initialPosition;
            transform.localRotation = initialRotation;
            body.isKinematic = true;
            SetPatternAttacking(false);
            SetColor(0.45f);
            foreach (Collider bossCollider in GetComponentsInChildren<Collider>())
                bossCollider.enabled = true;
        }

        public void BeginBattle()
        {
            SetColor(1f);
        }

        public void SetPatternAttacking(bool attacking)
        {
            if (attacking && defeated) return;

            animator.ResetTrigger("Idle2");
            animator.ResetTrigger("Jump");
            animator.SetTrigger(attacking ? "Jump" : "Idle2");
        }

        public void TryHit(SnowballPlayer player)
        {
            if (defeated || stageManager == null || !stageManager.CanShoot || !player.IsMoving) return;
            int shot = stageManager.CurrentShotCount;
            if (shot == lastDamagingShot) return;
            lastDamagingShot = shot;

            health = Mathf.Max(0f, health - player.Size * damagePerSize);
            player.ChangeSize(player.InitialSize - player.Size);
            Vector3 knockbackDirection = player.Body.position - transform.position;
            knockbackDirection.y = 0f;
            player.Body.linearVelocity = knockbackDirection.normalized * hitKnockbackSpeed;

            if (hitSupply != null)
                hitSupply.ThrowAfterBossHit();

            StopAllCoroutines();
            StartCoroutine(HitReaction());
            if (health <= 0f)
            {
                defeated = true;
                Vector3 launchDirection = -knockbackDirection.normalized;
                if (launchDirection.sqrMagnitude < 0.01f)
                    launchDirection = transform.forward;
                stageManager.DefeatBoss(this, launchDirection);
            }
        }

        private IEnumerator HitReaction()
        {
            transform.localPosition = initialPosition + Vector3.up * 0.15f;
            yield return new WaitForSeconds(hitReactionDuration);
            transform.localPosition = initialPosition;
        }

        private void SetColor(float multiplier)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                for (int j = 0; j < originalColors[i].Length; j++)
                {
                    renderers[i].GetPropertyBlock(block, j);
                    block.SetColor("_BaseColor", originalColors[i][j] * multiplier);
                    renderers[i].SetPropertyBlock(block, j);
                }
            }
        }

        public void Defeat(Vector3 launchDirection)
        {
            defeated = true;
            StopAllCoroutines();
            SetColor(defeatedColorValue);
            foreach (Collider bossCollider in GetComponentsInChildren<Collider>())
                bossCollider.enabled = false;

            animator.Play("Jump_Emperor", 0, 0f);
            body.isKinematic = false;
            body.useGravity = true;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.AddForce(launchDirection * 20f + Vector3.up * 14f, ForceMode.VelocityChange);
            body.AddTorque(new Vector3(12f, 0f, 18f), ForceMode.VelocityChange);
        }
    }
}
