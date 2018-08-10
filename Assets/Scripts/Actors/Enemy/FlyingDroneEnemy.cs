using UnityEngine;
using System.Collections;
using System.Security.AccessControl;

public class FlyingDroneEnemy : ShooterEnemy
{
    [SerializeField] private float _timeBetweenMissiles = 7;
    [SerializeField] private GameObject _missilePrefab;
    private int _timeBetweenHealing = 10;
    private bool _isHealing;

    protected override void Start()
    {
        base.Start();
        InvokeRepeating("TryToHeal", 0, _timeBetweenHealing);
        health.OnTakeDamage += TryToHeal;
        health.OnTakeDamage += StopHealing;
    }

    private void StopHealing()
    {
        if (_isHealing)
        {
            _isHealing = false;
            _anim.SetBool("Healing", _isHealing);
            enemyAI.enabled = true;
        }
    }

    public override void Attack()
    {
        base.Attack();
        m_Attacking = true;
        if (!m_CanAttack) return;
        FireMissile();
        StartCoroutine(WaitThenFireMissile());
    }

    private IEnumerator WaitThenFireMissile()
    {
        yield return new WaitForSeconds(_timeBetweenMissiles);
        FireMissile();
    }

    private void FireMissile()
    {
        GameObject missile = Instantiate(_missilePrefab, shootTransform.position, Quaternion.identity);
        Missile missileScript = missile.GetComponent<Missile>();
        missileScript.Shooter = this.gameObject;
        missileScript.Target = targeting.Target;
    }

    /// <summary>
    /// starts healing IF health is low enough and not already healing
    /// </summary>
    void TryToHeal()
    {
        if (health.CurrentHealth < 0.7f * health.MaxHealth && !_isHealing)
        {
            print("Healing");
            // move away from player
            rb.AddForce((transform.position - GameManager.Player.transform.position).normalized * 10,
                ForceMode2D.Impulse);

            //disable AI to stop moving
            enemyAI.enabled = false;
            _isHealing = true;
        }
    }

    void Heal()
    {
        _anim.SetBool("Healing", _isHealing);
        if (_isHealing)
        {
            health.RegenerateHealth(50);
            Invoke("Heal", 1);
        }
    }
}