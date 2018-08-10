using UnityEngine;

public class PlayerShooter : Shooter
{
    [SerializeField] AimInput aimInput;
    CameraKickback cameraKickback;

    protected override void Awake()
    {
        base.Awake();
        cameraKickback = Camera.main.GetComponent<CameraKickback>();
        if (!aimInput) aimInput = GetComponent<AimInput>();
    }

    public override void Shoot(Vector2 shootDirection)
    {
        base.Shoot(shootDirection);
        if (cameraKickback) cameraKickback.DoKickback(-shootDirection * CurrentWeaponStats.cameraKickback);
    }
    private void Update()
    {
        // disable shooting for mobile
#if !(UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_IPHONE)
        if (Input.GetMouseButton(0) || Input.GetAxisRaw("RightTrigger") > 0.5f || Input.GetKey(KeyCode.LeftControl) || AimInput.RightJoystick.magnitude > 0.1f)
        {
            ShootIfAllowed(aimInput.AimDirection);
        }
#endif
    }
}
