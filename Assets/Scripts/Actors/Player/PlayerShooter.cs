using UnityEngine;

namespace Actors.Player
{
    public class PlayerShooter : Shooter
    {
        AimInput m_aimInput;
        CameraKickback m_cameraKickback;

        protected override void Awake()
        {
            base.Awake();
            m_cameraKickback = Camera.main.GetComponent<CameraKickback>();
            if (m_aimInput == null)
                m_aimInput = GetComponent<AimInput>();
        }

        public override GameObject Shoot(Vector2 shootDirection)
        {
            if (m_cameraKickback)
                m_cameraKickback.DoKickback(-shootDirection * CurrentWeaponStats.cameraKickback);

            return base.Shoot(shootDirection);
        }

        private void Update()
        {
            // disable shooting for mobile
#if !(UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_IPHONE)
            if (Input.GetMouseButton(0) || Input.GetAxisRaw("RightTrigger") > 0.5f ||
                Input.GetKey(KeyCode.LeftControl) ||
                AimInput.RightJoystick.magnitude > 0.1f)
            {
                ShootIfAllowed(m_aimInput.AimDirection);
            }
#endif
        }
    }
}