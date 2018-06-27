using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]

public class HealthScript : MonoBehaviour
{
    public const float HEALTH_REGENERATION_PERIOD = 1.0f;


    public GameObject healthBar, deathEffects;
    [HideInInspector] public bool isDead = false;
    [SerializeField] public int currentHealth;

    // To be set in inspector

    [SerializeField] protected bool regenerateHealth = false;
    [SerializeField] protected bool flashColorWhenDamage = true;
    [SerializeField] protected bool useHealthbar = true;
    [SerializeField] protected bool destroyOnDeath = false;
    [SerializeField] protected int startHealth = 100;
    [SerializeField] protected float fallDamageModifier = 0.5f;
    [SerializeField]
    protected AudioClip
        hurtAudioClip,
        deathAudioClip;

    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private float hurtDamageThreshold = 10;
    [SerializeField] private float regenAmount = 0.01f;

    protected SpriteRenderer rend;
    protected Slider healthSlider;
    protected AudioSource audioSource;
    protected Color originalColor;

    private Animator _anim;

    public void Awake() {
        //gameObject.AddComponent<ScriptedHealthBar>();
        if (GetComponent<Animator>() != null) _anim = GetComponent<Animator>();
        rend = GetComponent<SpriteRenderer>();
        // If GetSource() is null, then add a source
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        if (useHealthbar && healthBar != null)
            InitializeHealthBar();

        originalColor = rend.color;
        if (gameObject.layer == LayerMask.NameToLayer("Object"))
            fallDamageModifier = 0;
    }

    private void Start() {
        currentHealth = startHealth;
        // Initializing the healthbar settings
        if (healthSlider != null && useHealthbar) {
            healthSlider.maxValue = startHealth;
            healthSlider.value = currentHealth;
        }
        InvokeRepeating("RegenerateHealth", 0, HEALTH_REGENERATION_PERIOD); //Periodically regenerate health
    }

    private void Update() {
        LerpColor(); //In case the color flashes red, it has to go back to normal
    }

    public virtual void TakeDamage(int damageAmount) {
        if (!isDead) {
            Hurt();
            //Debug.Log("Take damage " + gameObject.name);
            currentHealth -= damageAmount;
            UpdateHealthBar();
            CheckHealth();
        }
    }

    /// <summary> Plays the hurt animation (if any), color flashes red, plays hurt sound. </summary>
    private void Hurt() {
        audioSource.PlayOneShot(hurtAudioClip);
        if (_anim != null && !isDead)
            _anim.SetTrigger("Hurt");
        if (flashColorWhenDamage)
            rend.color = damageColor;
    }
    public float CheckHealth() {
        if (currentHealth <= 0 || isDead)
            Die();
        currentHealth = Mathf.Clamp(currentHealth, 0, startHealth);
        UpdateHealthBar();
        return currentHealth;
    }
    private void UpdateHealthBar() {
        if (healthSlider != null)
            healthSlider.value = currentHealth;
        else if (useHealthbar)
            Debug.LogWarning("Healthslider not found for " + gameObject.name);
    }

    public virtual void Die() {
        //Debug.Log(gameObject.name + " died");
        if (deathAudioClip) audioSource.PlayOneShot(deathAudioClip);
        if (!isDead) { // If this is the first time this method is called
                       // Make death effects, sounds, and animation
            if (deathEffects != null) Instantiate(deathEffects, transform.position, Quaternion.identity);
            if (_anim != null) _anim.SetTrigger("Die");
            if (audioSource != null) audioSource.Play();
        }
        currentHealth = 0;
        isDead = true;

        if (destroyOnDeath)
            Destroy(gameObject);
        //else
        //    gameObject.SetActive(false);
    }

    private void LerpColor() {
        if (!rend.color.Equals(originalColor) && flashColorWhenDamage)
            rend.color = Color.Lerp(rend.color, originalColor, Time.deltaTime * 10);
    }

    private void OnCollisionEnter2D(Collision2D other) {
        if (fallDamageModifier > 0) { // Skips taking fall damage to be more efficient.
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
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
    /// <summary> If not yet reached max health, increment by 1% of the starting health </summary>
    private void RegenerateHealth() {
        currentHealth += (currentHealth < startHealth) ? (int)(regenAmount * startHealth) : 0;
        CheckHealth();
    }

    private void InitializeHealthBar() {
        if (healthBar == null)
            healthBar = Instantiate(healthBar, transform) as GameObject;
        healthSlider = healthBar.transform.GetComponentsInChildren<Slider>()[0];
    }
}
