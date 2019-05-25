using UnityEngine;
using UnityEngine.Serialization;

namespace Actors
{
    public class BulletScript : MonoBehaviour
    {
        [FormerlySerializedAs("damageAmount")] [SerializeField]
        public int DamageAmount = 5;

        [FormerlySerializedAs("destroyMask")] [SerializeField]
        protected LayerMask DestroyMask;

        /// <summary> the object that created this bullet, useful for not damaging itself </summary>
        [HideInInspector] public GameObject Shooter;

        [FormerlySerializedAs("_correctRotation")] [SerializeField]
        private bool m_correctRotation;

        /// should the bullet also damage other game objects with the same tag as the shooter (parent)?
        public bool DamageShootersWithSameTag;


        private void Awake()
        {
            if (DestroyMask.value == 0) DestroyMask = LayerMask.GetMask("Everything");
            Destroy(gameObject, 7);
        }

        private void CorrectRotation()
        {
            transform.Rotate(Vector3.up, 90);
            transform.Rotate(Vector3.forward, 90);
        }

        private void Start()
        {
            if (m_correctRotation)
                CorrectRotation();
        }

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            // prevent damaging the attacker
            if (transform.IsChildOf(other.gameObject.transform))
                return;
            if (other.isTrigger)
                return;

            Health otherHealth = other.gameObject.GetComponent<Health>();
            if (otherHealth && other.gameObject)
            {
                // if it's the same type as the shooter, do damage
                if (Shooter == null || !other.gameObject.CompareTag(Shooter.tag) || DamageShootersWithSameTag)
                    otherHealth.TakeDamage(DamageAmount, transform.rotation.eulerAngles.normalized);
            }

            if (Utils.IsInLayerMask(DestroyMask, other.gameObject.layer))
            {
                Destroy(gameObject);
            }
        }

        private void OnBecameInvisible()
        {
            Destroy(gameObject, 3f);
        }
    }
}