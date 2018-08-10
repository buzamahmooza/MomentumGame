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

    [SerializeField] protected AudioClip hurtAudioClip, deathAudioClip;
    [SerializeField] protected bool destroyOnDeath = false;
    [SerializeField] [Range(0, 1000f)] public int MaxHealth = 100;
    [SerializeField] [Range(0, 1000f)] public int CurrentHealth;
    [SerializeField] public GameObject floatingTextPrefab; // assigned in inspector

    [SerializeField] [Range(0, 100)] protected float fallDamageModifier = 0.5f;
    [SerializeField] [Range(0, 100)] float fallDamageThreshold = 10;

    [SerializeField] protected bool autoRegenHealth = false;
    [SerializeField] [Range(0, 100)] float regenPercent = 1f;

    /// <summary>
    /// the minimum damage needed to create hurtEffects, any smaller damage value will not cause hurtEffects
    /// </summary>
    [SerializeField] [Range(0, 100)] int hurtEffectsDamageValueThreshold = 10;

    [HideInInspector] public bool IsDead = false;

    protected Rigidbody2D rb;
    protected SpriteRenderer SpriteRenderer;
    protected Slider healthSlider;
    protected AudioSource audioSource;
    protected Color originalColor;
    protected Walker walker;

    protected Animator _anim;

    public Action OnTakeDamage;
    public Action OnDeath;


    protected virtual void Awake()
    {
        _anim = GetComponent<Animator>();
        walker = GetComponent<Walker>();
        SpriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        CurrentHealth = MaxHealth;

        if (useHealthbar && healthBar != null)
            InitHealthBar();
        // Initializing the healthbar 
        if (healthSlider && useHealthbar)
        {
            healthSlider.maxValue = MaxHealth;
            healthSlider.value = CurrentHealth;
        }

        originalColor = SpriteRenderer.color;
        if (gameObject.layer == LayerMask.NameToLayer("Object"))
            fallDamageModifier = 0;
        if (autoRegenHealth)
            InvokeRepeating("RegenerateHealth", 0, HealthRegenerationInterval); //Periodically regenerate health
    }

    protected virtual void LateUpdate()
    {
        LerpColor(); //In case the color flashes red, it has to go back to normal
    }

    protected void CreateFloatingDamage(int damageValue)
    {
        Debug.Assert(this.floatingTextPrefab != null);
        GameObject floatingDamageInstance =
            Instantiate(this.floatingTextPrefab, transform.position, Quaternion.identity);
        FloatingText theFloatingText = floatingDamageInstance.GetComponent<FloatingText>();
        theFloatingText.InitBounceDmg(damageValue);
        theFloatingText.text.color = Color.Lerp(Color.yellow, Color.red, (float) damageValue / MaxHealth);
    }


    public virtual void TakeDamage(int damageAmount)
    {
        TakeDamage(damageAmount, Vector3.back + UnityEngine.Random.insideUnitSphere);
    }

    public virtual void TakeDamage(int damageAmount, Vector3 direction)
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

        if (hurtEffects && damageAmount > hurtEffectsDamageValueThreshold)
        {
            GameObject effects;
            if (_stickyHurtEffects)
                effects = Instantiate(hurtEffects, transform.position, Quaternion.identity, transform);
            else
                effects = Instantiate(hurtEffects, transform.position, Quaternion.identity);

            effects.transform.LookAt(direction);
        }

        if (floatingTextPrefab)
            CreateFloatingDamage(damageAmount);
    }

    /// <summary> Plays the hurt animation (if any), color flashes red, plays hurt sound. </summary>
    private void Hurt()
    {
        audioSource.PlayOneShot(hurtAudioClip);
        if (_anim != null && !IsDead)
            _anim.SetTrigger("Hurt");
        SpriteRenderer.color = _damageColor;
    }

    public void Stun(float seconds)
    {
        StartCoroutine(EnumStun(seconds));
    }

    protected IEnumerator EnumStun(float seconds)
    {
        if (!walker || !walker.BlockMoveInput)
        {
            yield return null;
        }
        else
        {
            Debug.Log(gameObject.name + ":   Oh no! what's going on? I can't see!");
            walker.BlockMoveInput = false;

            yield return new WaitForSeconds(seconds);
            Debug.Log(gameObject.name + ": Mwahahaha I can see again! Time to die robot!!");
            walker.BlockMoveInput = true;
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
        if (healthSlider != null)
            healthSlider.value = CurrentHealth;
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
                OnDeath();
            // Make death effects, sounds, and animation
            if (deathEffects)
            {
                if (_stickyDeathEffects)
                    Instantiate(deathEffects, transform.position, Quaternion.identity, transform);
                else
                    Instantiate(deathEffects, transform.position, Quaternion.identity);
            }

            if (_anim)
            {
                _anim.speed = 1f;
                _anim.SetTrigger("Die");
            }

            if (deathAudioClip) audioSource.PlayOneShot(deathAudioClip);
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
        if (!SpriteRenderer.color.Equals(originalColor))
            SpriteRenderer.color = Color.Lerp(SpriteRenderer.color, originalColor, Time.deltaTime * 10);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (IsDead && rb.velocity.y <= 0.1f && gameObject != GameManager.Player)
        {
            Destroy(gameObject, 5f);
        }

        // Skips taking fall damage to be more efficient.
        if (fallDamageModifier > 0)
        {
            Rigidbody2D otherRb = other.gameObject.GetComponent<Rigidbody2D>();

            if (rb != null && otherRb != null)
            {
                float fallDamage = Vector3.Distance(otherRb.velocity * otherRb.mass, rb.velocity * rb.mass) *
                                   fallDamageModifier;

                // only take fallDamage if falldamage is big enough
                // this check prevents small falls from affecting
                if (fallDamage > fallDamageThreshold)
                    TakeDamage(Mathf.RoundToInt(fallDamage));
            }
        }
    }

    /// <summary> Increment by x% of the starting health (only if not yet reached max health) </summary>
    public void RegenerateHealth()
    {
        RegenerateHealth(Mathf.RoundToInt(regenPercent / 100 * MaxHealth));
    }
    /// <summary> Add health </summary>
    /// <param name="addedHealth"></param>
    public void RegenerateHealth(int addedHealth)
    {
        // add regenPercent but not exceding maxHealth
        CurrentHealth = Mathf.Clamp(CurrentHealth + addedHealth, 0, MaxHealth);
        CheckHealth();
        SpriteRenderer.color = Color.green;
    }

    private void InitHealthBar()
    {
        if (!healthBar)
            healthBar = Instantiate(healthBar, transform);
        healthSlider = healthBar.transform.GetComponentsInChildren<Slider>()[0];
        healthSlider.maxValue = MaxHealth;
    }
}