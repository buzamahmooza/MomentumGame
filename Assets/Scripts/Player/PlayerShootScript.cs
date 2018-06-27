using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InControl;

public class PlayerShootScript : MonoBehaviour
{
    public GameObject arrow;
    //public Transform shootTransform;
    public WeaponStats CurrentWeaponStats;
    public float kickbackForce = 10;

    private float m_TimeBetweenShots;
    private float m_TimeSinceLastShot = 0;
    private AudioSource audioSource;
    private PlayerMove playerMove;
    private AimInput aimInput;
    private CameraKickback cameraKickback;
    private Rigidbody2D rb;
    [SerializeField] private Transform shootTransform;

    [System.Serializable]
    public struct WeaponStats
    {
        public GameObject projectilePrefab;
        public float
            projectileSpeed, //50
            rpm; //70
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
    private void FixedUpdate() {
        if (Input.GetMouseButton(0) || Input.GetAxisRaw("RightTrigger") > 0.5f || Input.GetKey(KeyCode.LeftAlt)) {
            Shoot(aimInput.AimDirection);
        }
        RotateArrow();
        FixShootTiming();
    }

    private void RotateArrow() {
        float angle = Vector2.Angle(aimInput.AimDirection, Vector2.right);
        Vector2 d = aimInput.AimDirection * FacingSign;
        angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        arrow.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    private void FixShootTiming() {
        m_TimeBetweenShots = Time.deltaTime * 360.0f / CurrentWeaponStats.rpm;
        m_TimeSinceLastShot += Time.deltaTime;
    }

    private void Shoot(Vector2 shootDirection) {
        if (m_TimeSinceLastShot >= m_TimeBetweenShots) {
            m_TimeSinceLastShot = 0; // reset shoot timer

            Vector2 positionWiggler = (new Vector2(-shootDirection.y, shootDirection.x)).normalized * Random.Range(-CurrentWeaponStats.wiggleShootOffset, CurrentWeaponStats.wiggleShootOffset);
            Vector2 shootPosition = 
                //(shootTransform ? (Vector2)(shootTransform.position) : shootDirection * 0.1f + (Vector2)(transform.position)) +
                           (Vector2)(shootTransform.position)+positionWiggler;

            audioSource.PlayOneShot(CurrentWeaponStats.shootSound, Random.Range(0.7f, 1f));

            Quaternion rotation = Quaternion.LookRotation(Quaternion.Euler(0, 0, Random.Range(-CurrentWeaponStats.randomShootAngle, CurrentWeaponStats.randomShootAngle)) * shootDirection);
            GameObject projectile = Instantiate(CurrentWeaponStats.projectilePrefab, shootPosition, rotation, this.transform) as GameObject;
            projectile.GetComponent<BulletScript>().damageAmount = Mathf.RoundToInt(CurrentWeaponStats.damage*Random.Range(0.85f, 1.2f));
            projectile.GetComponent<Rigidbody2D>().velocity = shootDirection * CurrentWeaponStats.projectileSpeed;

            rb.AddForce(-shootDirection * kickbackForce * Time.fixedDeltaTime, ForceMode2D.Impulse);
            if (cameraKickback) cameraKickback.DoKickback(-shootDirection * CurrentWeaponStats.cameraKickback);
        }
    }

    private InputDevice Device { get { return InputManager.ActiveDevice; } }

    public int FacingSign { get { if (playerMove.m_FacingRight) return 1; else return -1; } }

    private void SetWeaponStats(WeaponStats newWeaponStats) {
        CurrentWeaponStats = newWeaponStats;
    }

}
