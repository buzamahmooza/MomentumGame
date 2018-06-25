using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player_Health : MonoBehaviour {

    [SerializeField]
    private float startHealth = 100;
    private float currentHealth;
    [SerializeField] private GameObject healthBar;
    Slider healthSlider;

    void Awake () {
        healthBar = Instantiate(healthBar, transform) as GameObject;
        healthBar.transform.SetParent(gameObject.transform.Find("Canvas").gameObject.transform);
        healthSlider = healthBar.GetComponent<Slider>();
    }

    private void Start()
    {
        currentHealth = startHealth;
        healthSlider.maxValue = startHealth;
        healthSlider.value = currentHealth;

    }

    void Update () {
		
	}

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        checkHealth();
    }

    void checkHealth()
    {
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    void Die()
    {
        print(gameObject.name + " died");
        Destroy(gameObject);
    }
}
