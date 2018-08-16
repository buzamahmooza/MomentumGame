using UnityEngine;
using System.Security.AccessControl;

public class FlyingDroneEnemy : ShooterEnemy
{
    [SerializeField] private float _timeBetweenMissiles = 7;
    [SerializeField] private GameObject _missilePrefab;
    private int _timeBetweenHealing = 5;
    private bool _isHealing;

    protected override void Start()
    {
        base.Start();
        InvokeRepeating("TryToHeal", 0, _timeBetweenHealing);
        health.OnTakeDamage += TryToHeal;
        health.OnTakeDamage += StopHealing;
    }

    public override void Attack()
    {
        if (_isHealing) return;

        base.Attack();
        Invoke("FireMissile", _timeBetweenMissiles);
        if (!m_CanAttack)
            return;
    }

    private void FireMissile()
    {
        if (!m_CanAttack)
        {
            Invoke("FireMissile", 1); // try again after 1s 
            return;
        }
        
        GameObject missile = Instantiate(_missilePrefab, shootTransform.position, Quaternion.identity);
        Missile missileScript = missile.GetComponent<Missile>();
        missileScript.Shooter = this.gameObject;
        missileScript.Target = targeting.Target;
        missileScript.IsArmed = false;
    }

    /// <summary>
    /// starts healing IF health is low enough and not already healing
    /// </summary>
    void TryToHeal()
    {
        if (health.CurrentHealth < 0.7f * health.MaxHealth && !_isHealing)
        {
            // move away from player
            Rb.velocity = (transform.position - GameManager.Player.transform.position).normalized * 5;
            Invoke("StopMoving", 2);
            
            _isHealing = true;
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
        ai.enabled = false;
    }

    void Heal()
    {
        m_CanAttack = false;

        if (health.CurrentHealth > health.MaxHealth * 0.99f)
            StopHealing();

        _anim.SetBool("Healing", _isHealing);
        if (_isHealing)
        {
            health.AddHealth(Mathf.RoundToInt(health.MaxHealth * 0.05f));
            Invoke("Heal", 0.2f);
        }
    }

    void StopHealing()
    {
        CancelInvoke("Heal");
        if (_isHealing)
        {
            _isHealing = false;
            _anim.SetBool("Healing", _isHealing);
            ai.enabled = true;
            m_CanAttack = true;
        }
    }
}