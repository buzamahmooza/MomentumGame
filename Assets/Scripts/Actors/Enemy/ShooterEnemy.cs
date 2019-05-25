using System.Collections;
using UnityEngine;

namespace Actors.Enemy
{
    [RequireComponent(typeof(Shooter))]
    public class ShooterEnemy : Enemy
    {
        [SerializeField] protected Transform ShootTransform;
        [SerializeField] [Range(1, 50)] private int m_burstSize = 7;
        protected Shooter Shooter;


        protected override void Awake()
        {
            base.Awake();
            if (!Shooter) Shooter = GetComponent<Shooter>();
            if (!ShootTransform)
            {
                Debug.LogWarning(this.name + ": ShootTransform not assigned");
                ShootTransform = transform.Find("shootPosition");
                if(!ShootTransform) 
                    ShootTransform = transform;
            }
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
            Vector3 shootDirection = GameComponents.Player.transform.position - this.transform.position;

            int i = 0;
            while (i++ < m_burstSize)
            {
                // set x velocity to 0
                Rb.velocity = new Vector2(0, Rb.velocity.y);

                Anim.SetTrigger("Attack");

                Shooter.Shoot(shootDirection);
                if (!Health.IsDead)
                    yield return new WaitForSeconds(60.0f / Shooter.CurrentWeaponStats.rpm);
            }

            IsAttacking = false;
        }
    }
}