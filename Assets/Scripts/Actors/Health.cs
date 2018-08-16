using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

[RequireComponent(typeof(AudioSource))]
public class Health : MonoBehaviour
{
    private const float HealthRegenerationInterval = 1.0f;

    // To be set in inspector
    [SerializeField] protected bool useHealthbar = true;
    [SerializeField] public GameObject healthBar;

    [SerializeField] public GameObject hurtEffects;
    [SerializeField] bool _stickyHurtEffects;
    [SerializeField] public GameObject deathEffects;
    [SerializeField] bool _stickyDeathEffects;

    [SerializeField] Color _damageColor = Color.red;

    [SerializeField] protected AudioClip[] hurtAudioClips,
        deathAudioClips;

    [SerializeField] protected bool destroyOnDeath = false;
    [SerializeField] [Range(0, 1000f)] public int MaxHealth = 100;
    [SerializeField] [Range(0, 1000f)] public int CurrentHealth;
    [SerializeField] public GameObject floatingTextPrefab; // assigned in inspector

    [SerializeField] [Range(0, 100)] protected float fallDamageModifier = 0.5f;
    [Range(0, 100)] float m_fallDamageThreshold = 7;

    [SerializeField] protected bool autoRegenHealth = false;
    [SerializeField] [Range(0, 100)] float regenPercent = 1f;

    /// <summary>
    /// the minimum damage needed to create hurtEffects, any smaller damage value will not cause hurtEffects
    /// </summary>
    [SerializeField] [Range(0, 100)] int _minDamageForHurtEffects = 10;

    /// <summary> the minimum damage needed to cause the enemy to get stunned </summary>
    [SerializeField] [Range(0, 100)] int minDamageForStun = 10;

    [HideInInspector] public bool IsDead = false;

    protected SpriteRenderer SpriteRenderer;
    private Slider m_healthSlider;
    private AudioSource m_audioSource;
    protected Color OriginalColor;
    protected Walker Walker;

    protected Animator Anim;

    public Action OnTakeDamage;
    public Action OnDeath;


    protected virtual void Awake()
    {
        Anim = GetComponent<Animator>();
        Walker = GetComponent<Walker>();
        SpriteRenderer = GetComponent<SpriteRenderer>();
        m_audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        CurrentHealth = MaxHealth;

        if (useHealthbar && healthBar != null)
            InitHealthBar();
        // Initializing the healthbar 
        if (m_healthSlider && useHealthbar)
        {
            m_healthSlider.maxValue = MaxHealth;
            m_healthSlider.value = CurrentHealth;
        }

        OriginalColor = SpriteRenderer.color;
        if (gameObject.layer == LayerMask.NameToLayer("Object"))
            fallDamageModifier = 0;
        if (autoRegenHealth)
            InvokeRepeating("AddHealth", 0, HealthRegenerationInterval); //Periodically regenerate health
    }

    protected virtual void LateUpdate()
    {
        LerpColor(); //In case the color flashes red, it has to go back to normal
    }

    private void CreateFloatingDamage(int damageValue)
    {
        Debug.Assert(this.floatingTextPrefab != null);
        GameObject floatingDamageInstance =
            Instantiate(this.floatingTextPrefab, transform.position, Quaternion.identity);
        FloatingText theFloatingText = floatingDamageInstance.GetComponent<FloatingText>();
        theFloatingText.InitBounceDmg(damageValue);
        theFloatingText.text.color = Color.Lerp(Color.yellow, Color.red, (float) damageValue / MaxHealth);
    }


    public void TakeDamage(int damageAmount)
    {
        TakeDamage(damageAmount, Vector3.back + UnityEngine.Random.insideUnitSphere);
    }

    public void TakeDamage(int damageAmount, Vector3 direction)
    {
        if (IsDead)
            return;

        if (OnTakeDamage != null)
            OnTakeDamage();

        Hurt();
        //Debug.Log("Take damage " + gameObject.name);
        CurrentHealth -= damageAmount;
        UpdateHealthBar();
        CheckHealth();

        if (hurtEffects && damageAmount > _minDamageForHurtEffects)
        {
            GameObject effects = Instantiate(hurtEffects, transform.position, Quaternion.identity,
                _stickyHurtEffects ? transform : null);

            effects.transform.LookAt(direction);
        }

        if (floatingTextPrefab)
            CreateFloatingDamage(damageAmount);

        if (damageAmount > minDamageForStun)
            Stun(1.5f);
    }

    /// <summary> Plays the hurt animation (if any), color flashes red, plays hurt sound. </summary>
    private void Hurt()
    {
        if (hurtAudioClips.Length > 0)
            m_audioSource.PlayOneShot(Utils.GetRandomElement(hurtAudioClips));

        if (Anim != null && !IsDead)
            Anim.SetTrigger("Hurt");
        SpriteRenderer.color = _damageColor;
    }

    public void Stun(float seconds)
    {
        StartCoroutine(EnumStun(seconds));
    }

    protected IEnumerator EnumStun(float seconds)
    {
        if (!Walker || !Walker.BlockMoveInput)
        {
            yield return null;
        }
        else
        {
            Debug.Log(gameObject.name + ":   Oh no! what's going on? I can't see!");
            Walker.BlockMoveInput = false;

            yield return new WaitForSeconds(seconds);
            Debug.Log(gameObject.name + ": Mwahahaha I can see again! Time to die robot!!");
            Walker.BlockMoveInput = true;
        }
    }

    /// <summary>
    /// in charge of updating the healthbar and dying if health is below 0
    /// </summary>
    public void CheckHealth()
    {
        if (CurrentHealth <= 0 || IsDead)
            Die();
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (m_healthSlider != null)
            m_healthSlider.value = CurrentHealth;
        else
            Debug.LogWarning("Healthslider not found on " + gameObject.name);
    }

    /// <summary>
    /// The die method for the gameObject
    /// Instantiates deathEffects, plays audio, sets animator trigger("Die")
    /// </summary>
    public virtual void Die()
    {
        // If this is the first time this method is called
        if (!IsDead)
        {
            if (OnDeath != null)
            {
                print("OnDeath is not null: " + OnDeath);
                OnDeath();
            }

            // Make death effects, sounds, and animation
            if (deathEffects)
            {
                Instantiate(deathEffects, transform.position, Quaternion.identity,
                    _stickyDeathEffects ? transform : null);
            }

            if (Anim)
            {
                Anim.speed = 1f;
                Anim.SetTrigger("Die");
            }

            if (deathAudioClips.Length > 0)
                m_audioSource.PlayOneShot(Utils.GetRandomElement(deathAudioClips));
        }

        IsDead = true;
        if (healthBar) healthBar.SetActive(false);
        CurrentHealth = 0;

        if (destroyOnDeath)
            Destroy(gameObject);
    }

    /// <summary> If not original color, transition back to the original color </summary>
    private void LerpColor()
    {
        if (!SpriteRenderer.color.Equals(OriginalColor))
            SpriteRenderer.color = Color.Lerp(SpriteRenderer.color, OriginalColor, Time.deltaTime * 10);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // if the thing is dead and it falls, just delete it
        if (IsDead && collision.relativeVelocity.y <= 0.1f && gameObject != GameManager.Player)
        {
            Destroy(gameObject, 5f);
        }

        // Skips taking fall damage to be more efficient.
        if (fallDamageModifier > 0)
        {
            Rigidbody2D otherRb = collision.otherRigidbody;
            if (Walker.Rb != null && otherRb != null)
            {
                float fallDamage = collision.relativeVelocity.magnitude * otherRb.mass * Walker.Rb.mass *
                                   fallDamageModifier;

                if (collision.relativeVelocity.magnitude < m_fallDamageThreshold)
                    fallDamage = 0;

                // only take fallDamage if falldamage is big enough
                // this check prevents small falls from affecting
                if (fallDamage > m_fallDamageThreshold)
                    TakeDamage(Mathf.RoundToInt(fallDamage));
            }
        }
    }

    /// <summary> Increment by x% of the starting health (only if not yet reached max health) </summary>
    public void AddHealth()
    {
        AddHealth(Mathf.RoundToInt(regenPercent / 100 * MaxHealth));
    }

    /// <summary> Add health </summary>
    /// <param name="healthToAdd"></param>
    public void AddHealth(int healthToAdd)
    {
        // add regenPercent but not exceding maxHealth
        CurrentHealth = Mathf.Clamp(CurrentHealth + healthToAdd, 0, MaxHealth);
        CheckHealth();
        SpriteRenderer.color = Color.green;
    }

    private void InitHealthBar()
    {
        if (!healthBar)
            healthBar = Instantiate(healthBar, transform);
        m_healthSlider = healthBar.transform.GetComponentsInChildren<Slider>()[0];
        m_healthSlider.maxValue = MaxHealth;
    }
}