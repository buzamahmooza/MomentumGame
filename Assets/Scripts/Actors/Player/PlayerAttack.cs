using System;
using InControl;
using JetBrains.Annotations;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace Actors.Player
{
    [RequireComponent(typeof(PlayerMove))]
    public partial class PlayerAttack : MonoBehaviour
    {
        public ComboInstance CurrentComboInstance;
        [HideInInspector] public bool HasReachedSlamPeak;

        // components
        private Animator m_anim;

        private float m_animSpeed = 1;
        [SerializeField] [Range(0, 50)] private float m_attackLunge = 1f;
        [SerializeField] private AudioClip m_dashAttackSound;
        [SerializeField] private float m_dashAttackSpeedFactor = 20f;

        [SerializeField] private LayerMask m_explosionMask; // enemy, object

        [Range(0.5f, 10f)] [SerializeField] private float m_explosionRadius = 2f;

        [SerializeField] [Header("Auto: initializes using the triggers in the first child object 'Hitboxes'")]
        private Hitboxes m_hitboxes;

        [SerializeField] private float m_hitSettleMult = 5f;

        /// <summary>
        ///     The combo timer, combos are canceled if there are no attacks for longer than this time in seconds
        /// </summary>
        [SerializeField] [Range(0, 5f)] private float m_maxTimeBetweenAttacks = 3f;

        private PlayerMove m_playerMove;

        /// these booleans control
        private bool m_slam,
            m_punch,
            m_uppercut;

        [SerializeField] private AudioClip m_slamAttackSound;
        [SerializeField] private GameObject m_slamExplosion;
        [SerializeField] private float m_uppercutJumpForce = 1f;

        [Range(0f, 10f)] [SerializeField] private float m_upwardModifier = 1.7f;

        private static Vector2 InputRaw => new Vector2(CrossPlatformInputManager.GetAxisRaw("Horizontal"),
            CrossPlatformInputManager.GetAxisRaw("Vertical"));

        private static bool AttackInput => Input.GetButton("Fire1") || Input.GetKey(KeyCode.Joystick1Button18) ||
                                           InputManager.ActiveDevice.Action3.IsPressed;


        private void Awake()
        {
            m_playerMove = GetComponent<PlayerMove>();
            GetComponent<AudioSource>();
            m_anim = GetComponent<Animator>();

            if (m_explosionMask == 0) m_explosionMask = LayerMask.GetMask("Enemy", "Object");

            // init hitboxes
            var hitboxes = transform.Find("Hitboxes");
            if (m_hitboxes.SlamHitbox == null)
                m_hitboxes.SlamHitbox = hitboxes.Find("SlamHitbox").GetComponent<Hitbox>();
            if (m_hitboxes.DashAttackHitbox == null)
                m_hitboxes.DashAttackHitbox = hitboxes.Find("DashAttackHitbox").GetComponent<Hitbox>();
            if (m_hitboxes.PunchHitbox == null)
                m_hitboxes.PunchHitbox = hitboxes.Find("PunchHitbox").GetComponent<Hitbox>();
            if (m_hitboxes.UppercutHitbox == null)
                m_hitboxes.UppercutHitbox = hitboxes.Find("UppercutHitbox").GetComponent<Hitbox>();
        }

        private void Start()
        {
            m_hitboxes.SlamHitbox.enabled = false;
            m_hitboxes.DashAttackHitbox.enabled = false;
            m_hitboxes.PunchHitbox.enabled = false;
            m_hitboxes.UppercutHitbox.enabled = false;

            m_animSpeed = m_anim.speed;
            Debug.Assert(m_hitboxes.SlamHitbox && m_hitboxes.PunchHitbox);

            SubscribeToHitEvents();
        }

        // TODO: call the effects (screenshake, hitStop and slomo) here rather than having it in Hitbox
        private void SubscribeToHitEvents()
        {
            m_hitboxes.UppercutHitbox.OnHitEvent += OnHitHandler;
            m_hitboxes.DashAttackHitbox.OnHitEvent += OnHitHandler;
            m_hitboxes.PunchHitbox.OnHitEvent += OnHitHandler;
            m_hitboxes.SlamHitbox.OnHitEvent += OnHitHandler;
        }

        private void OnHitHandler(GameObject go, float speedMult, bool isFinalBlow)
        {
            if (CurrentComboInstance == null || CurrentComboInstance.HasEnded)
                CurrentComboInstance = new ComboInstance(m_maxTimeBetweenAttacks);

            CurrentComboInstance.IncrementCount();


            FreezeFall();
        }


        private void Update()
        {
            if (CurrentComboInstance != null)
            {
                // display combo text
                GameComponents.ComboManager.DisplayCombo(CurrentComboInstance.HasEnded
                    ? 0
                    : CurrentComboInstance.Count);
                GameComponents.ComboManager.ComboText.text +=
                    "\nCombo time left: " + CurrentComboInstance.TimeRemainingBeforeTimeout;

                CurrentComboInstance.AddTime(Time.deltaTime);
            }


            // Get input if there is no action to disturb
            if (!(m_uppercut || m_punch || m_slam || m_anim.GetBool("DashAttack"))) CheckForAttackInput();

            //When landing a slam on the ground, go back to normal animation speed
            if (m_anim.GetBool("Slamming") && m_playerMove.Grounded) m_anim.speed = m_animSpeed;
        }

        private void CheckForAttackInput()
        {
            var input = InputRaw;
            var isAttackInputDown = Input.GetKeyDown(KeyCode.F) || InputManager.ActiveDevice.Action3.IsPressed;


            // SlamAttack:
            // If airborn and pressing down
            if (AttackInput && input.y <= -0.5f && Mathf.Abs(input.x) <= 0.5)
            {
                if (m_playerMove.Grounded) // if not in the air, Jump()
                    m_playerMove.Jump();
                m_slam = true;
                UpdateAnimatorParams();
            }
            // DashAttack
            // If DashAttack conditions are met
            else if (isAttackInputDown && m_playerMove.Grounded && m_playerMove.CanDashAttack &&
                     Mathf.Abs(input.x) > 0.1f && !m_anim.GetBool("DashAttack"))
            {
                m_playerMove.Rb.AddForce(
                    Vector2.right * m_playerMove.Rb.velocity.x * Time.deltaTime * m_dashAttackSpeedFactor,
                    ForceMode2D.Impulse);
                m_anim.SetBool("DashAttack", true);
                // Block input for DashAttack animation length
                StartCoroutine(m_playerMove.BlockMoveInput(m_anim.GetCurrentAnimatorClipInfo(0).GetLength(0)));
                UpdateAnimatorParams();

                if (m_playerMove.Rb.velocity.y < 1)
                    FreezeFall();
            }
            // Uppercut
            else if (AttackInput && input.y >= 0.5f && Mathf.Abs(input.x) <= 0.5)
            {
                m_uppercut = true;
                m_anim.SetTrigger("Uppercut");
                UpdateAnimatorParams();

                FreezeFall();
                m_playerMove.Rb.AddForce(Vector2.up);
            }
            // otherwise just do a boring-ass punch...
            else if (isAttackInputDown)
            {
                m_punch = true;
                StartCoroutine(m_playerMove.BlockMoveInput(1f, 0.5f));
                UpdateAnimatorParams();

                if (m_playerMove.Rb.velocity.y < 1)
                {
//                        FreezeFall();
//                        Invoke("UnfreezeFall", 1f);
                }
            }
        }

        /// stops the player from falling and floats in the air for a bit
        private void FreezeFall()
        {
            // stop the player from falling, (just like dishwasher vampire and Salt&Sanctuary)
            m_playerMove.Rb.gravityScale = 0;
            m_playerMove.Rb.velocity = new Vector2(m_playerMove.Rb.velocity.x, 0);
        }

        private void UnfreezeFall()
        {
            m_playerMove.Rb.gravityScale = 1.2f; // todo: don't hardcode
            m_playerMove.Rb.velocity = new Vector2(m_playerMove.Rb.velocity.x, 0);
        }

        public void CreateSlamExplosion()
        {
            Instantiate(m_slamExplosion, m_hitboxes.SlamHitbox.Collider2D.bounds.center + Vector3.back,
                Quaternion.identity);
            var landingSpeed = Mathf.Abs(m_playerMove.Rb.velocity.y);
            GameComponents.CameraShake.DoJitter(0.2f, 0.4f + landingSpeed * 0.3f);

            var explosionPos = transform.position;
            foreach (var hit in Physics2D.OverlapCircleAll(explosionPos, m_explosionRadius, m_explosionMask))
            {
                var otherHealth = hit.gameObject.GetComponent<Health>();
                if (!otherHealth)
                {
                    Debug.LogWarning("enemyHealth not found on " + hit.name);
                    continue;
                }

                otherHealth.Stun(2);

                var distance = Vector2.Distance(transform.position, hit.gameObject.transform.position);
                var otherRb = hit.GetComponent<Rigidbody2D>();

                if (otherRb)
                    otherRb.AddForce(
                        new Vector2(m_playerMove.FacingSign, m_upwardModifier) * (landingSpeed / distance),
                        ForceMode2D.Impulse
                    );
            }
        }


        private void CheckIfLandedAndEndSlam()
        {
            if (m_playerMove.Grounded)
            {
                Attack_Slam();
                CreateSlamExplosion();
                SlamEnded();
            }
            else
            {
                Invoke(nameof(CheckIfLandedAndEndSlam), 0.5f);
            }
        }

        private void UpdateAnimatorParams()
        {
            m_anim.SetBool("Punching", m_punch);
            m_anim.SetBool("Slamming", m_slam);
        }

        // This is just to make the members collapsable in the inspector
        [Serializable]
        private struct Hitboxes
        {
            public Hitbox PunchHitbox;
            public Hitbox SlamHitbox;
            public Hitbox DashAttackHitbox;
            public Hitbox UppercutHitbox;
        }
    }

    // animation-invoked functions
    public partial class PlayerAttack
    {
        // TODO: cleanup the names, make them consistent across attacks, maybe even make a class for it
        //    class Attack { [NotNull] private string name; void OpenHitbox() { } void CloseHitbox() { } }
        
        /// Opens the punch interval (the active frames where the hitbox is on)

        #region Punch

        public void PunchOpenHitbox()
        {
            m_hitboxes.PunchHitbox.enabled = true;
            //todo: fix this, playermove Move() is overriding the speed
            m_playerMove.Rb.AddForce(Vector2.right * m_playerMove.FacingSign * m_attackLunge,
                ForceMode2D.Impulse);
        }

        public void PunchCloseHitbox()
        {
            m_punch = false;
            m_hitboxes.PunchHitbox.enabled = false;
        }

        public void PunchCompleted()
        {
            m_punch = false;
            UpdateAnimatorParams();
        }

        #endregion


        // uppercut

        #region Uppercut

        public void UppercutStart()
        {
            m_uppercut = true;
            m_hitboxes.UppercutHitbox.enabled = true;
            Invoke(nameof(UppercutCompleted), 1f);
            m_playerMove.Rb.AddForce(Vector2.up * m_uppercutJumpForce, ForceMode2D.Impulse);
        }

        public void UppercutCompleted()
        {
            print("UppercutCompleted()");
            m_uppercut = false;
            m_hitboxes.UppercutHitbox.enabled = false;
        }

        #endregion


        #region Slam

        public void ReachedSlamPeak()
        {
            HasReachedSlamPeak = true;
            m_anim.speed = 0;
            gameObject.layer = LayerMask.NameToLayer("PlayerIgnore");

            CheckIfLandedAndEndSlam();
        }

        public void Attack_Slam()
        {
            m_hitboxes.SlamHitbox.enabled = true;
        }

        public void SlamEnded()
        {
            m_slam = false;
            m_anim.speed = m_animSpeed;
            m_hitboxes.SlamHitbox.enabled = false;
            gameObject.layer = LayerMask.NameToLayer("Player");
            UpdateAnimatorParams();
        }

        #endregion


        #region DashAttack

        public void DashAttackOpen()
        {
            m_hitboxes.DashAttackHitbox.enabled = true;
        }

        public void DashAttackClose()
        {
            m_hitboxes.DashAttackHitbox.enabled = false;
            m_anim.SetBool("DashAttack", false);
            m_punch = false;
            m_playerMove.IsMoveInputBlocked = false;
        }

        #endregion
    }
}
