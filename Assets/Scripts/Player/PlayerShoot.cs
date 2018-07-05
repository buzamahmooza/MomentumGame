using Pathfinding.Util;
using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
    [SerializeField] GameObject arrow;
    /// <summary>
    /// A the amount force of the kickback
    /// this times the mass will be the force of the kickback
    /// </summary>
    [SerializeField] float kickbackForceMplier = 10;

    [SerializeField] AimInput aimInput;
    [SerializeField]
    WeaponStats CurrentWeaponStats = new WeaponStats(
        projectilePrefab: null,
        projectileSpeed: 15,
        rpm: 50,
        cameraKickback: 25,
        wiggleShootOffset: 0.12f,
        randomShootAngle: 15,
        damage: 7,
        shootSound: null
    );
    [SerializeField] Transform shootTransform;

    float m_TimeBetweenShots;
    float m_TimeSinceLastShot = 0;
    AudioSource audioSource;
    PlayerMove playerMove;
    CameraKickback cameraKickback;
    Rigidbody2D rb;

    [System.Serializable]
    public struct WeaponStats
    {
        public GameObject projectilePrefab;
        public float projectileSpeed; //15
        [Range(0, 1000)]
        public float rpm; //50
        public int damage; //25
        [Range(0, 0.5f)]
        public float wiggleShootOffset; //0.12
        [Range(0, 90)]
        public float randomShootAngle;  // (in degrees) 10
        [Range(0, 1)]
        public float cameraKickback; // 0.1
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
        ) {
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

    private void Awake() {
        playerMove = GetComponent<PlayerMove>();
        audioSource = GetComponent<AudioSource>();
        if (!aimInput) aimInput = GetComponent<AimInput>();
        rb = GetComponent<Rigidbody2D>();
        cameraKickback = Camera.main.GetComponent<CameraKickback>();
        if (arrow == null) arrow = transform.Find("Arrow Parent").gameObject;

    }
    private void Update() {
        if (Input.GetMouseButton(0) || Input.GetAxisRaw("RightTrigger") > 0.5f || Input.GetKey(KeyCode.LeftControl) || AimInput.RightJoystick.magnitude > 0.1f) {
            Vector2 shootDirection = aimInput.AimDirection;
            ShootIfAllowed(shootDirection);
        }
    }

    public void ShootIfAllowed(Vector2 shootDirection) {
        if (m_TimeSinceLastShot >= m_TimeBetweenShots) {
            m_TimeSinceLastShot = 0; // reset shoot timer
            Shoot(shootDirection);
        }
    }

    private void FixedUpdate() {
        FixShootTiming();
    }

    private void LateUpdate() {
        RotateArrow();
    }

    private void RotateArrow() {
        Vector2 d = aimInput.AimDirection * playerMove.FacingSign;
        var angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        arrow.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    private void FixShootTiming() {
        m_TimeBetweenShots = Time.fixedDeltaTime * 360.0f / CurrentWeaponStats.rpm;
        m_TimeSinceLastShot += Time.fixedDeltaTime;
    }

    private void Shoot(Vector2 shootDirection) {
        float randomOffset = Random.Range(-CurrentWeaponStats.wiggleShootOffset, CurrentWeaponStats.wiggleShootOffset);
        Vector2 positionWithWiggle = (new Vector2(-shootDirection.y, shootDirection.x)).normalized * randomOffset;
        Vector2 shootPosition = (Vector2)(shootTransform.position) + positionWithWiggle;

        audioSource.PlayOneShot(CurrentWeaponStats.shootSound, Random.Range(0.7f, 1.0f));

        // Create simple rotation which looks where the player is aiming in addition to a wiggle amount of euler angles
        // How? IDK, just leave it, it works
        float randomRotation = Random.Range(-CurrentWeaponStats.randomShootAngle, CurrentWeaponStats.randomShootAngle);
        var bulletRotation = Quaternion.LookRotation(Quaternion.Euler(0, 0, randomRotation) * shootDirection);
        var projectile = Instantiate(CurrentWeaponStats.projectilePrefab, shootPosition, bulletRotation, this.transform); // Create bullet

        // Set bullet damage
        var bulletScript = projectile.GetComponent<BulletScript>();
        bulletScript.damageAmount = Mathf.RoundToInt(CurrentWeaponStats.damage * Random.Range(0.8f, 1.2f));
        // Set bullet movement
        var bulletRb = projectile.GetComponent<Rigidbody2D>();
        bulletRb.velocity = Quaternion.Euler(0, 0, randomRotation) * shootDirection * CurrentWeaponStats.projectileSpeed;

        rb.AddForce(-shootDirection * kickbackForceMplier * rb.mass * Time.fixedDeltaTime, ForceMode2D.Impulse);
        if (cameraKickback) cameraKickback.DoKickback(-shootDirection * CurrentWeaponStats.cameraKickback);
    }

    private void SetWeaponStats(WeaponStats newWeaponStats) {
        CurrentWeaponStats = newWeaponStats;
    }

    private void OnDrawGizmos() {
        // draw the randomShootAngle lines
        if (!aimInput) return;
        var shootDirection = aimInput.AimDirection;
        var positionWithWiggle = new Vector3(-shootDirection.y, shootDirection.x).normalized * CurrentWeaponStats.wiggleShootOffset;
        Vector3 shootPos = shootTransform.position;
        Gizmos.DrawLine(shootPos - positionWithWiggle, Quaternion.Euler(0, 0, -CurrentWeaponStats.randomShootAngle) * shootDirection + shootPos - positionWithWiggle);
        Gizmos.DrawLine(shootPos + positionWithWiggle, Quaternion.Euler(0, 0, CurrentWeaponStats.randomShootAngle) * shootDirection + shootPos + positionWithWiggle);
    }

}
