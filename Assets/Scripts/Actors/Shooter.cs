using UnityEngine;

namespace Actors
{
    public class Shooter : MonoBehaviour
    {
        /// <summary>
        /// A the amount force of the kickback
        /// this times the mass will be the force of the kickback
        /// </summary>
        [SerializeField] float kickbackForceMplier = 5;

        [SerializeField] public WeaponStats CurrentWeaponStats = new WeaponStats(
            projectilePrefab: null,
            projectileSpeed: 7,
            rpm: 300,
            cameraKickback: 0.1f,
            wiggleShootOffset: 0.12f,
            randomShootAngle: 15,
            damage: 7,
            shootSound: null
        );

        [SerializeField] Transform shootTransform;

        float m_TimeBetweenShots;
        float m_TimeSinceLastShot = 0;
        AudioSource audioSource;
        Rigidbody2D rb;
        private Targeting targeting;

        [System.Serializable]
        public struct WeaponStats
        {
            public GameObject projectilePrefab;
            public float projectileSpeed; //15
            [Range(1, 10000)] public float rpm; //50
            public int damage; //25
            [Range(0, 0.5f)] public float wiggleShootOffset; //0.12
            [Range(0, 90)] public float randomShootAngle; // (in degrees) 10
            [Range(0, 1)] public float cameraKickback; // 0.1
            public AudioClip shootSound;

            public WeaponStats(
                GameObject projectilePrefab,
                float projectileSpeed,
                float rpm,
                float cameraKickback,
                float wiggleShootOffset,
                float randomShootAngle,
                int damage,
                AudioClip shootSound
            )
            {
                this.cameraKickback = cameraKickback;
                this.projectilePrefab = projectilePrefab;
                this.projectileSpeed = projectileSpeed;
                this.rpm = rpm;
                this.wiggleShootOffset = wiggleShootOffset;
                this.randomShootAngle = randomShootAngle;
                this.damage = damage;
                this.shootSound = shootSound;
            }
        }

        protected virtual void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            rb = GetComponent<Rigidbody2D>();
            targeting = GetComponent<Targeting>();
        }

        public void ShootIfAllowed(Vector2 shootDirection)
        {
            if (m_TimeSinceLastShot >= m_TimeBetweenShots)
            {
                m_TimeSinceLastShot = 0; // reset shoot timer
                Shoot(shootDirection);
            }
        }

        private void FixedUpdate()
        {
            FixShootTiming();
        }

        void FixShootTiming()
        {
            m_TimeBetweenShots = Time.fixedDeltaTime * 360.0f / CurrentWeaponStats.rpm;
            m_TimeSinceLastShot += Time.fixedDeltaTime;
        }

        public void Shoot(Vector2 direction, GameObject projectilePrefab)
        {
        
        }
        /// <summary>
        /// creates a shot and sets it to the target's direction, if it's a missile, sets it to follow the target.
        /// Returns the projectile
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public GameObject Shoot(Transform target)
        {
            GameObject projectile = CreateProjectile(target.transform.position - transform.position);
            // Set bullet damage
            BulletScript bulletScript = projectile.GetComponent<BulletScript>();
            Missile missile = bulletScript as Missile;
            if (missile != null)
            {
                missile.Target = target;
            }

            return projectile;
        }

        public virtual GameObject Shoot(Vector2 shootDirection)
        {
            GameObject projectile = CreateProjectile(shootDirection);

            BulletScript bulletScript = projectile.GetComponent<BulletScript>();

            rb.AddForce(-shootDirection * kickbackForceMplier * rb.mass * Time.fixedDeltaTime, ForceMode2D.Impulse);
            return projectile;
        }

        private GameObject CreateProjectile(Vector2 shootDirection)
        {
            shootDirection.Normalize();
            float randomOffset = Random.Range(-CurrentWeaponStats.wiggleShootOffset, CurrentWeaponStats.wiggleShootOffset);
            Vector2 positionWithWiggle = new Vector2(-shootDirection.y, shootDirection.x).normalized * randomOffset;
            Vector2 shootPosition = (Vector2) shootTransform.position + positionWithWiggle;

            if (audioSource) audioSource.PlayOneShot(CurrentWeaponStats.shootSound, Random.Range(0.7f, 1.0f));
            else Debug.LogError("Audio source is not assigned!!");

            // Create simple rotation which looks where the player is aiming in addition to a wiggle amount of euler angles
            // How? IDK, just leave it, it works
            float randomRotation = Random.Range(-CurrentWeaponStats.randomShootAngle, CurrentWeaponStats.randomShootAngle);
            Quaternion bulletRotation =
                Quaternion.LookRotation(Quaternion.Euler(0, 0, randomRotation) * shootDirection);
            GameObject projectile = Instantiate(CurrentWeaponStats.projectilePrefab, shootPosition, bulletRotation);

            // Set projectile movement
            Rigidbody2D projectileRigidbody2D = projectile.GetComponent<Rigidbody2D>();
            projectileRigidbody2D.velocity = Quaternion.Euler(0, 0, randomRotation) * shootDirection *
                                             CurrentWeaponStats.projectileSpeed;
            BulletScript bulletScript = projectile.GetComponent<BulletScript>();
            bulletScript.DamageAmount =
                Mathf.RoundToInt(CurrentWeaponStats.damage * Random.Range(0.8f, 1.2f));
            bulletScript.Shooter = this.gameObject;

            return projectile;
        }

        public void SetWeaponStats(WeaponStats newWeaponStats)
        {
            CurrentWeaponStats = newWeaponStats;
        }

        private void OnDrawGizmos()
        {
            // draw the randomShootAngle lines
            if (!targeting) return;
            Vector2 shootDirection = targeting.AimDirection.normalized;
            Vector3 positionWithWiggle = new Vector3(-shootDirection.y, shootDirection.x).normalized *
                                         CurrentWeaponStats.wiggleShootOffset;
            Vector3 shootPos = shootTransform.position;
            Gizmos.DrawLine(shootPos - positionWithWiggle,
                Quaternion.Euler(0, 0, -CurrentWeaponStats.randomShootAngle) * shootDirection + shootPos -
                positionWithWiggle);
            Gizmos.DrawLine(shootPos + positionWithWiggle,
                Quaternion.Euler(0, 0, CurrentWeaponStats.randomShootAngle) * shootDirection + shootPos +
                positionWithWiggle);
        }
    }
}