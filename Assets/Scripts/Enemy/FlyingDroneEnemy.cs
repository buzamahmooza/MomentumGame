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
        InvokeRepeating("StartHealing", 0, _timeBetweenHealing);
        health.OnTakeDamage += delegate
        {
            _isHealing = false;
            _anim.SetBool("Heal", _isHealing);
        };
    }

    public override void Attack()
    {
        base.Attack();
        m_Attacking = true;
        if (!m_CanAttack) return;
        StartCoroutine(FireMissile());
    }

    private IEnumerator FireMissile()
    {
        yield return new WaitForSeconds(_timeBetweenMissiles);
        GameObject missile = Instantiate(_missilePrefab, shootTransform.position, Quaternion.identity);
        Missile missileScript = missile.GetComponent<Missile>();
        missileScript.Target = targeting.Target;
    }

    void StartHealing()
    {
        if (health.CurrentHealth < 50)
        {
            _isHealing = true;
        }
    }

    void Heal()
    {
        _anim.SetBool("Heal", _isHealing);
        if(_isHealing)
        {
            health.RegenerateHealth(50);
            Invoke("Heal", 1);
        }
    }
    
}