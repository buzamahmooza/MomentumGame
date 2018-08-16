using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

[RequireComponent(typeof(Enemy))]
public class EnemyHealth : Health
{
    [SerializeField] [Range(0, 500)] public int ScoreValue = 0; // assigned in inspector
    [SerializeField] private GameObject healthPickup;
    [SerializeField] private bool jumpAndPhaseThroughWhenDead = true;
    
    private GameObject m_player;
    private EnemyAI m_enemyAi;

    protected override void Awake()
    {
        base.Awake();
        m_player = GameManager.Player;
        m_enemyAi = GetComponent<EnemyAI>();
    }

    private void FixedUpdate()
    {
        // just destroy the enemy if he's way too far from the player
        if (Vector3.Distance(transform.position, m_player.transform.position) > 300)
        {
            Destroy(this.gameObject);
        }
    }

    private void CreateFloatingScore(int scoreVal)
    {
        Debug.Assert(floatingTextPrefab != null);
        Instantiate(floatingTextPrefab, transform.position, Quaternion.identity)
            .GetComponent<FloatingText>()
            .InitFloatingScore(scoreVal);
    }

    public override void Die()
    {
        if (m_enemyAi) m_enemyAi.enabled = false;

        // if this is the first time the enemy dies:
        if (!IsDead)
        {
            if (ScoreValue > 0)
            {
                CreateFloatingScore(ScoreValue);
                GameManager.ScoreManager.AddScore(ScoreValue, true);
                GameManager.PlayerHealth.AddHealth();
                
                // TODO: BAD design, subscribe to the OnDeath Action from the RoomBuilder instead, this is temperary. Remove later!
                KillObjective killObjective = GameManager.RoomBuilder.CurrentObjective as KillObjective;
                if (killObjective != null)
                    killObjective.OnEnemyKill();
            }

            // Add force up to give a nice effect
            if (Walker.Rb && jumpAndPhaseThroughWhenDead)
            {
                gameObject.layer = LayerMask.NameToLayer("EnemyIgnore");
                Walker.Rb.AddForce(Vector2.up * 6 * Walker.Rb.mass, ForceMode2D.Impulse);
            }
            else
            {
                gameObject.layer = LayerMask.NameToLayer("EnemyIgnoreButNotFloor");
            }
        }

        // spawn pickups (explode a random amount)
        if (healthPickup != null)
        {
            int numberOfPickupsToSpawn = UnityEngine.Random.Range(1, 5);
            while (numberOfPickupsToSpawn-- > 0)
            {
                GameObject pickupInstance = Instantiate(healthPickup, transform.position, Quaternion.identity);
                Rigidbody2D pickupRb = pickupInstance.GetComponent<Rigidbody2D>();

                // set trajectory
                Vector2 randomVector2 = UnityEngine.Random.insideUnitCircle;
                randomVector2.y = Mathf.Abs(randomVector2.y);

                // add force and set gravity
                pickupRb.AddForce(randomVector2, ForceMode2D.Impulse);
                pickupRb.gravityScale = 0.5f;
            }
        }

        // disable colliders (so it would go through the floor and fall out of the map)
        base.Die();
    }
}