using System.Linq;
using UnityEngine;

namespace Actors
{
    /// <summary>
    ///     assuming the rocket direction is facing right (the positive x axis)
    /// </summary>
    public class Missile : BulletScript
    {
        public bool IsArmed = true;
        [SerializeField] private AudioClip m_explosionClip;
        [SerializeField] private float m_explosionForce = 5;
        private GameObject m_objectSpawnedIn;

        [SerializeField] private GameObject m_particleEffects;

        private Rigidbody2D m_rb;
        [SerializeField] private float m_rotationSpeed = 1f;

        [SerializeField] private float m_speed = 10f;
        public Transform Target;


        private void Awake()
        {
            m_rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            var matches = Physics2D.CircleCastAll(transform.position, 0.5f, Vector2.up).Where(hit =>
                !hit.transform.IsChildOf(transform) && !hit.collider.isTrigger
            ).ToArray();
            if (matches.Length > 0)
            {
                m_objectSpawnedIn = matches[0].collider.gameObject;
                if (m_objectSpawnedIn)
                {
                    print("Missile spawned in object: " + m_objectSpawnedIn.name + ", dissarming");
                    IsArmed = false;
                    Invoke(nameof(Arm), 0.1f);
                }
            }
        }

        /// <summary>
        ///     Arms the missile after a delay
        /// </summary>
        /// <returns></returns>
        public void Arm()
        {
            IsArmed = true;
        }

        // Update is called once per frame
        private void FixedUpdate()
        {
            if (Target != null)
            {
                var toTarget = Target.position - transform.position;
                var angle = Vector2.SignedAngle(transform.right, toTarget);
                m_rb.angularVelocity = angle * m_rotationSpeed;
            }

            m_rb.velocity = transform.right * m_speed;
        }

        protected override void OnTriggerEnter2D(Collider2D col)
        {
            if (col.isTrigger)
            {
                Debug.Log(name + " didn't explode cuz usedByEffector || isTrigger");
                return;
            }

            // don't collide with other bullets or missiles
            if (col.gameObject.GetComponent<BulletScript>())
            {
                Debug.Log(name + " didn't explode cuz " + col.name + " has BulletScript");
                return;
            }

            var hitTarget = col.transform == Target;
            var hitExplosionMask = Utils.IsInLayerMask(DestroyMask, col.gameObject.layer);
            if (hitTarget || hitExplosionMask && IsArmed)
            {
                Debug.Log(string.Format(
                    "Missile exploded: ({0})",
                    hitTarget
                        ? $"target was hit: {col.name}"
                        : $"hit explosion mask ({LayerMask.LayerToName(col.gameObject.layer)})"
                ));

                var otherHealth = col.GetComponent<Health>();
                if (otherHealth)
                    otherHealth.TakeDamage(DamageAmount, transform.rotation.eulerAngles.normalized);
                Explode();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.gameObject == m_objectSpawnedIn)
            {
                Debug.Log("Missile left object spawned in");
                IsArmed = true;
            }
        }

        private void Explode()
        {
            Destroy(gameObject); // delay so that the audio will play
            if (m_explosionClip)
                GameComponents.AudioSource.PlayOneShot(m_explosionClip);

            if (m_particleEffects)
                Instantiate(m_particleEffects, transform.position, transform.rotation);

            foreach (var hit in Physics2D.OverlapCircleAll(transform.position, 1, DestroyMask))
            {
                var otherRb = hit.attachedRigidbody;
                var distance = Vector2.Distance(transform.position, hit.gameObject.transform.position);
                if (otherRb)
                    otherRb.AddForce(
                        m_explosionForce * (transform.position - hit.transform.position).normalized / (1 + distance) *
                        5,
                        ForceMode2D.Impulse
                    );

                var otherHealth = hit.gameObject.GetComponent<Health>();
                if (!otherHealth)
                {
                    Debug.LogWarning("Health not found on " + hit.name);
                    continue;
                }

                otherHealth.Stun(2);
                otherHealth.TakeDamage(DamageAmount);
            }
        }
    }
}