using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class Health : MonoBehaviour
{
    public const float HEALTH_REGENERATION_PERIOD = 1.0f;

    public GameObject healthBar, deathEffects;
    [HideInInspector] public bool IsDead = false;
    [SerializeField] public int CurrentHealth;

    // To be set in inspector

    [SerializeField] protected bool useHealthbar = true;
    [SerializeField] protected bool destroyOnDeath = false;
    [SerializeField] protected int maxHealth = 100;
    [SerializeField] protected float fallDamageModifier = 0.5f;
    [SerializeField]
    protected AudioClip
        hurtAudioClip,
        deathAudioClip;

    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private float hurtDamageThreshold = 10;
    [SerializeField] [Range(0, 100)] private float regenPercent = 1f;

    private Rigidbody2D rb;
    protected SpriteRenderer rend;
    protected Slider healthSlider;
    protected AudioSource audioSource;
    protected Color originalColor;
    protected Walker walker;

    private Animator _anim;

    protected virtual void Awake() {
        //gameObject.AddComponent<ScriptedHealthBar>();
        _anim = GetComponent<Animator>();
        walker = GetComponent<Walker>();
        rend = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        // If GetSource() is null, then add a source
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
    }

    private void Start() {
        CurrentHealth = maxHealth;
        // Initializing the healthbar settings
        if (healthSlider && useHealthbar) {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = CurrentHealth;
        }

        if (useHealthbar && healthBar != null)
            InitHealthBar();

        originalColor = rend.color;
        if (gameObject.layer == LayerMask.NameToLayer("Object"))
            fallDamageModifier = 0;

        InvokeRepeating("RegenerateHealth", 0, HEALTH_REGENERATION_PERIOD); //Periodically regenerate health
    }

    private void Update() {
        LerpColor(); //In case the color flashes red, it has to go back to normal
    }

    public virtual void TakeDamage(int damageAmount) {
        if (IsDead)
            return;

        Hurt();
        //Debug.Log("Take damage " + gameObject.name);
        CurrentHealth -= damageAmount;
        UpdateHealthBar();
        CheckHealth();
    }

    /// <summary> Plays the hurt animation (if any), color flashes red, plays hurt sound. </summary>
    private void Hurt() {
        audioSource.PlayOneShot(hurtAudioClip);
        if (_anim != null && !IsDead)
            _anim.SetTrigger("Hurt");
        rend.color = damageColor;
    }

    public void Stun(float seconds) {
        StartCoroutine(EnumStun(seconds));
    }
    public IEnumerator EnumStun(float seconds) {
        if (!walker || !walker.BlockMoveInput) {
            yield return null;
        } else {
            Debug.Log(gameObject.name + ":   Oh no! what's going on? I can't see!");
            walker.BlockMoveInput = false;

            yield return new WaitForSeconds(seconds);
            Debug.Log(gameObject.name + ": Mwahahaha I can see again! Time to die robot!!");
            walker.BlockMoveInput = true;
        }
    }
    public float CheckHealth() {
        if (CurrentHealth <= 0 || IsDead)
            Die();
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);
        UpdateHealthBar();
        return CurrentHealth;
    }
    private void UpdateHealthBar() {
        if (healthSlider != null)
            healthSlider.value = CurrentHealth;
        else
            Debug.LogWarning("Healthslider not found on " + gameObject.name);
    }

    public virtual void Die() {
        if (deathAudioClip) audioSource.PlayOneShot(deathAudioClip);
        // If this is the first time this method is called
        if (!IsDead) {
            // Make death effects, sounds, and animation
            if (deathEffects) Instantiate(deathEffects, transform.position, Quaternion.identity);
            if (_anim) _anim.SetTrigger("Die");
            if (audioSource) audioSource.Play();
        }
        IsDead = true;
        CurrentHealth = 0;

        if (destroyOnDeath)
            Destroy(gameObject);
    }

    private void LerpColor() {
        if (!rend.color.Equals(originalColor))
            rend.color = Color.Lerp(rend.color, originalColor, Time.deltaTime * 10);
    }

    private void OnCollisionEnter2D(Collision2D other) {
        // Skips taking fall damage to be more efficient.
        if (fallDamageModifier > 0) {
            Rigidbody2D otherRb = other.gameObject.GetComponent<Rigidbody2D>();

            if (rb != null && otherRb != null) {
                float fallDamage = Vector3.Distance(otherRb.velocity * otherRb.mass, rb.velocity * rb.mass) * fallDamageModifier;

                if (fallDamage > hurtDamageThreshold)   // Was the fall hard enough 
                                                        // this check prevents small falls from affecting
                                                        // also prevents having many small damages accumulate
                    TakeDamage((int)fallDamage);

                //print(this.gameObject.name + " took fall damage from " + other.gameObject.name + "\t" + fallDamage);
            }
        }
    }
    /// <summary> If not yet reached max health, increment by x% of the starting health </summary>
    public void RegenerateHealth() {
        RegenerateHealth((int)(regenPercent / 100) * maxHealth);
    }
    public void RegenerateHealth(int addedHealth) {
        // add regenPercent but not exceding maxHealth
        CurrentHealth = Mathf.Clamp(CurrentHealth + addedHealth, 0, maxHealth);
        CheckHealth();

    }

    private void InitHealthBar() {
        if (!healthBar)
            healthBar = Instantiate(healthBar, transform);
        healthSlider = healthBar.transform.GetComponentsInChildren<Slider>()[0];
        healthSlider.maxValue = maxHealth;
    }
}
