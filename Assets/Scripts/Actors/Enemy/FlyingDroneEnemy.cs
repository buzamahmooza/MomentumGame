using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Security.AccessControl;

public class FlyingDroneEnemy : ShooterEnemy
{
    [SerializeField] [Range(0, 20)] private float m_timeBetweenMissiles = 7;
    [SerializeField] [Range(0, 20)] private int m_maxActiveMissiles = 1;
    [SerializeField] private GameObject m_missilePrefab;

    private readonly int m_timeBetweenHealing = 5;
    private bool m_isHealing;
    private readonly HashSet<GameObject> m_missilesFired = new HashSet<GameObject>();
    private float m_lastMissileTime;

    protected override void Start()
    {
        base.Start();
        InvokeRepeating("TryToHeal", 0, m_timeBetweenHealing);
        Health.OnTakeDamage += TryToHeal;
        Health.OnTakeDamage += StopHealing;
    }

    public override void Attack()
    {
        if (m_isHealing) return;

        base.Attack();
        Invoke("FireMissile", m_timeBetweenMissiles);
        if (!CanAttack)
            return;
    }

    private void FireMissile()
    {
        if ((Time.time - m_lastMissileTime) < m_timeBetweenMissiles || !CanAttack)
        {
            print("Gonna try FireMissile later, reason: " + (!CanAttack ? "!CanAttack" : "m_timeBetweenMissiles"));
            Invoke("FireMissile", 1); // try again after 1s 
            return;
        }

        GameObject missile = Instantiate(m_missilePrefab, shootTransform.position, Quaternion.identity);
        Missile missileScript = missile.GetComponent<Missile>();
        missileScript.Shooter = this.gameObject;
        missileScript.Target = Targeting.Target;
        missileScript.IsArmed = false;
        m_missilesFired.Add(missile);
        m_lastMissileTime = Time.time;
    }

    private GameObject[] ActiveMissiles
    {
        get { return m_missilesFired.ToArray().Where(m => m != null).ToArray(); }
    }

    /// starts healing IF health is low enough and not already healing
    void TryToHeal()
    {
        if (Health.CurrentHealth < 0.7f * Health.MaxHealth && !m_isHealing)
        {
            // move away from player
            Rb.velocity = (transform.position - GameComponents.Player.transform.position).normalized * 5;
            Invoke("StopMoving", 2);

            m_isHealing = true;
            Heal();
        }
    }

    /// <summary>
    /// used to stop moving when healing
    /// </summary>
    void StopMoving()
    {
        Rb.velocity = Vector2.zero;
        //disable AI to stop moving
        Ai.enabled = false;
    }

    void Heal()
    {
        CanAttack = false;

        if (Health.CurrentHealth > Health.MaxHealth * 0.99f)
            StopHealing();

        Anim.SetBool("Healing", m_isHealing);
        if (m_isHealing)
        {
            Health.AddHealth(Mathf.RoundToInt(Health.MaxHealth * 0.05f));
            Invoke("Heal", 0.2f);
        }
    }

    void StopHealing()
    {
        CancelInvoke("Heal");
        if (m_isHealing)
        {
            m_isHealing = false;
            Anim.SetBool("Healing", m_isHealing);
            Ai.enabled = true;
            CanAttack = true;
        }
    }
}