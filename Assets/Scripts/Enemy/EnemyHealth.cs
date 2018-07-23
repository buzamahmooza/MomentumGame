using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

[RequireComponent(typeof(Enemy))]
public class EnemyHealth : Health
{
    [SerializeField] [Range(0, 100)] private int scoreValue = 0;  // assigned in inspector
    private GameObject player;
    private EnemyAI enemyAI;
    [SerializeField] private GameObject healthPickup;
    [SerializeField] private bool jumpAndPhaseThroughWhenDead = true;

    protected override void Awake() {
        base.Awake();
        player = GameManager.Player;
        enemyAI = GetComponent<EnemyAI>();
    }

    private void FixedUpdate() {
        // just destroy the enemy if he's way too far from the player
        if (Vector3.Distance(transform.position, player.transform.position) > 300) {
            Destroy(this.gameObject);
        }
    }

    private void CreateFloatingScore(int scoreVal) {
        Debug.Assert(floatingTextPrefab != null);
        Instantiate(floatingTextPrefab, transform.position, Quaternion.identity)
            .GetComponent<FloatingText>()
            .InitFloatingScore(scoreVal);
    }

    public override void Die() {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        if (enemyAI) enemyAI.enabled = false;

        // if this is the first time the enemy dies:
        if (!IsDead) {
            if (scoreValue > 0) {
                CreateFloatingScore(scoreValue);
                GameManager.ScoreManager.AddScore(scoreValue);
                GameManager.PlayerHealth.RegenerateHealth();
            }

            // Add force up to give a nice effect
            if (rb && jumpAndPhaseThroughWhenDead) {
                // override physics
                //rb.velocity = Vector2.zero;
                gameObject.layer = LayerMask.NameToLayer("EnemyIgnore");
                rb.AddForce(Vector2.up * 6 * rb.mass, ForceMode2D.Impulse);
            } else {
                gameObject.layer = LayerMask.NameToLayer("EnemyIgnoreButNotFloor");
            }
        }


        // explode a random amount of pickups
        if (healthPickup) {
            int numberOfPickupsToSpawn = UnityEngine.Random.Range(1, 5);
            while (numberOfPickupsToSpawn-- > 0) {
                var pickupInstance = Instantiate(healthPickup, transform.position, Quaternion.identity);
                var pickupRb = pickupInstance.GetComponent<Rigidbody2D>();
                var randomVector2 = UnityEngine.Random.insideUnitCircle; randomVector2.y = Mathf.Abs(randomVector2.y);
                pickupRb.AddForce(randomVector2, ForceMode2D.Impulse);
                pickupRb.gravityScale = 0.5f;
            }

            print("numberOfPickupsToSpawn = " + numberOfPickupsToSpawn);
        }

        // disable colliders (so it would go through the floor and fall out of the map)
        base.Die();

        //GrappleHookDJ grappleScript = player.GetComponent<GrappleHookDJ>();
        //if (grappleScript && grappleScript.grabbedObj == this.gameObject) {
        //    grappleScript.EndGrapple();
        //}
    }
}
