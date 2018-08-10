using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Shooter))]
public class ShooterEnemy : Enemy
{
    [SerializeField] protected Transform shootTransform;
    [SerializeField] [Range(1, 50)] private int burstSize = 7;
    protected Shooter shooter;
    private float timeBetweenShotsInBurst;


    protected override void Awake()
    {
        base.Awake();
        if (!shooter) shooter = GetComponent<Shooter>();
        if (!shootTransform) shootTransform = transform.Find("shootPosition");
    }

    public override void Attack()
    {
        base.Attack();
        m_Attacking = true;
        if (!m_CanAttack) return;
        StartCoroutine(FireBurst());
    }

    private IEnumerator FireBurst()
    {
        timeBetweenShotsInBurst = 60.0f / shooter.CurrentWeaponStats.rpm;
        var i = 0;
        while (i++ < burstSize)
        {
            // set x velocity to 0
            Rb.velocity = new Vector2(0, Rb.velocity.y);

            _anim.SetTrigger("Attack");

            shooter.Shoot(GameManager.Player.transform);
            if (!health.IsDead)
                yield return new WaitForSeconds(timeBetweenShotsInBurst);
        }
        m_Attacking = false;
    }
}