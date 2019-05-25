using UnityEngine;
using UnityEngine.Serialization;

namespace Actors.Enemy
{
    [RequireComponent(typeof(Collider2D))]
    public class EnemyHitbox : MonoBehaviour
    {
        [FormerlySerializedAs("damageAmount")] [SerializeField] [Range(0, 1000)] public int DamageAmount = 20;
        [FormerlySerializedAs("hitSound")] [SerializeField] private AudioClip m_hitSound;
        [HideInInspector] public Collider2D Collider2D;
        private AudioSource m_audioSource;
        private void OnEnable() { Collider2D.enabled = true; }
        private void OnDisable() { Collider2D.enabled = false; }

        private void Awake()
        {
            m_audioSource = GetComponent<AudioSource>();
            if (!m_audioSource) m_audioSource = GetComponentInParent<AudioSource>();
            Collider2D = GetComponent<Collider2D>();
            Collider2D.enabled = false;
        }
        private void Start()
        {
            if (!transform.parent)
                return;
        }
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (transform.IsChildOf(other.gameObject.transform)) // prevent damaging the attacker
                return;

            Health otherHealth = other.gameObject.GetComponent<Health>();
            // if it's NOT the same type as the attacker, do damage (so enemies can't damage eachother)
            if (otherHealth && !other.CompareTag(transform.parent.tag))
            {
                otherHealth.TakeDamage(DamageAmount);
                if (m_hitSound) m_audioSource.PlayOneShot(m_hitSound);
            }
        }
    }
}
