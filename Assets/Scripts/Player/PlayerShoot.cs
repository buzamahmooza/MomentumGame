using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
    public GameObject arrow;
    /// <summary>
    /// A the amount force of the kickback
    /// this times the mass will be the force of the kickback
    /// </summary>
    public float kickbackForceMplier = 10;

    private float m_TimeBetweenShots;
    private float m_TimeSinceLastShot = 0;
    private AudioSource audioSource;
    private PlayerMove playerMove;
    private AimInput aimInput;
    private CameraKickback cameraKickback;
    private Rigidbody2D rb;
    [SerializeField] private WeaponStats CurrentWeaponStats;
    [SerializeField] private Transform shootTransform;

    [System.Serializable]
    public struct WeaponStats
    {
        public GameObject projectilePrefab;
        public float projectileSpeed; //15
        public float rpm; //50
        public int damage; //25
        [Range(0, 0.5f)] public float wiggleShootOffset; //0.12
        [Range(0, 120)] public float randomShootAngle;  // (in degrees) 10
        [Range(0, 1)] public float cameraKickback; // 0.1
        public AudioClip shootSound;

        public WeaponStats(
            GameObject projectilePrefab,
            float rpm,
            float cameraKickback,
            float projectileSpeed,
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
        aimInput = GetComponent<AimInput>();
        rb = GetComponent<Rigidbody2D>();
        cameraKickback = Camera.main.GetComponent<CameraKickback>();
        if (arrow == null) arrow = transform.Find("Arrow Parent").gameObject;

    }
    private void Update() {
        if (Input.GetMouseButton(0) || Input.GetAxisRaw("RightTrigger") > 0.5f || Input.GetKey(KeyCode.LeftAlt)) {
            Shoot(aimInput.AimDirection);
        }
    }

    private void FixedUpdate() {
        FixShootTiming();
    }

    private void LateUpdate() {
        RotateArrow();
    }

    private void RotateArrow() {
        Vector2 d = aimInput.AimDirection * playerMove.FacingRightSign;
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        arrow.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    private void FixShootTiming() {
        m_TimeBetweenShots = Time.fixedDeltaTime * 360.0f / CurrentWeaponStats.rpm;
        m_TimeSinceLastShot += Time.fixedDeltaTime;
    }

    /// <summary>
    /// Called in FixedUpdate
    /// </summary>
    /// <param name="shootDirection"></param>
    private void Shoot(Vector2 shootDirection) {
        if (!(m_TimeSinceLastShot >= m_TimeBetweenShots)) return;

        m_TimeSinceLastShot = 0; // reset shoot timer

        Vector2 positionWiggler = (new Vector2(-shootDirection.y, shootDirection.x)).normalized * Random.Range(-CurrentWeaponStats.wiggleShootOffset, CurrentWeaponStats.wiggleShootOffset);
        Vector2 shootPosition = (Vector2)(shootTransform.position) + positionWiggler;

        audioSource.PlayOneShot(CurrentWeaponStats.shootSound, Random.Range(0.7f, 1.0f));

        // Create simple rotation which looks where the player is aiming in addition to a wiggle amount of euler angles
        // How? IDK, just leave it, it works
        Quaternion rotation = Quaternion.LookRotation(Quaternion.Euler(0, 0, Random.Range(-CurrentWeaponStats.randomShootAngle, CurrentWeaponStats.randomShootAngle)) * shootDirection);
        GameObject projectile = Instantiate(CurrentWeaponStats.projectilePrefab, shootPosition, rotation, this.transform) as GameObject; // Create bullet

        // Set bullet damage
        projectile.GetComponent<BulletScript>().damageAmount = Mathf.RoundToInt(CurrentWeaponStats.damage * Random.Range(0.8f, 1.2f));
        // Set bullet movement
        projectile.GetComponent<Rigidbody2D>().velocity = shootDirection * CurrentWeaponStats.projectileSpeed;

        rb.AddForce(-shootDirection * kickbackForceMplier * rb.mass * Time.fixedDeltaTime, ForceMode2D.Impulse);
        if (cameraKickback) cameraKickback.DoKickback(-shootDirection * CurrentWeaponStats.cameraKickback);
    }

    private void SetWeaponStats(WeaponStats newWeaponStats) {
        CurrentWeaponStats = newWeaponStats;
    }

}
