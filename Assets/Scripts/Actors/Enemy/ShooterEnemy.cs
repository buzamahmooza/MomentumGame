using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Shooter))]
public class ShooterEnemy : Enemy
{
    [SerializeField] protected Transform shootTransform;
    [SerializeField] [Range(1, 50)] private int burstSize = 7;
    protected Shooter shooter;


    protected override void Awake()
    {
        base.Awake();
        if (!shooter) shooter = GetComponent<Shooter>();
        if (!shootTransform) shootTransform = transform.Find("shootPosition");
    }

    public override void Attack()
    {
        base.Attack();
        if (!CanAttack)
            return;
        StartCoroutine(FireBurst());
    }

    private IEnumerator FireBurst()
    {
        Vector3 shootDirection = GameManager.Player.transform.position - this.transform.position;

        int i = 0;
        while (i++ < burstSize)
        {
            // set x velocity to 0
            Rb.velocity = new Vector2(0, Rb.velocity.y);

            Anim.SetTrigger("Attack");

            shooter.Shoot(shootDirection);
            if (!Health.IsDead)
                yield return new WaitForSeconds(60.0f / shooter.CurrentWeaponStats.rpm);
        }

        IsAttacking = false;
    }
}