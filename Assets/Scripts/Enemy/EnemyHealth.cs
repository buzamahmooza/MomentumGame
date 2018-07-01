using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyHealth : Health
{
    [SerializeField] private int scoreValue = 0;  // assigned in inspector
    [SerializeField] private GameObject floatingTextPrefab;  // assigned in inspector
    private GameObject player;
    private EnemyAI enemyAI;

    protected override void Awake() {
        base.Awake();
        player = GameManager.Player;
        enemyAI = GetComponent<EnemyAI>();
    }

    public override void TakeDamage(int damageAmount) {
        base.TakeDamage(damageAmount);
        CreateFloatingDamage(damageAmount);
    }

    public IEnumerator EnumStun(float seconds) {
        if (enemyAI == null) {
            print("EnumStun():  enemyAI is null");
            yield return null;
        } else {
            print("Enemy:   Oh no! what's going on? I can't see!");
            enemyAI.enabled = false;

            yield return new WaitForSeconds(seconds);
            print("Mwahahaha I can see again! Time to die robot!!");
            enemyAI.enabled = true;
        }
    }

    private void FixedUpdate() {
        if (Vector3.Distance(transform.position, player.transform.position) > 300) {
            Destroy(this.gameObject);
        }
    }

    private void AddScore(float scoreAmount) {
        throw new NotImplementedException();
    }

    private void CreateFloatingDamage(int damageValue) {
        Debug.Assert(this.floatingTextPrefab != null);
        GameObject floatingDamageInstance = (Instantiate(this.floatingTextPrefab, transform.position, Quaternion.identity) as GameObject);
        FloatingText theFloatingText = floatingDamageInstance.GetComponent<FloatingText>();
        theFloatingText.InitBounceDmg(damageValue);
        theFloatingText.text.color = Color.Lerp(Color.yellow, Color.red, (float)damageValue / maxHealth);
    }

    private void CreateFloatingScore(int scoreVal) {
        Debug.Assert(floatingTextPrefab != null);
        Instantiate(floatingTextPrefab, transform.position, Quaternion.identity)
            .GetComponent<FloatingText>()
            .InitFloatingScore(scoreVal);
        GameManager.ScoreManager.AddScore(scoreVal);
    }

    public override void Die() {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        if (enemyAI) enemyAI.enabled = false;

        if (!IsDead) {
            if (scoreValue > 0) {
                CreateFloatingScore(scoreValue);
                GameManager.Player.GetComponent<Health>().RegenerateHealth();
            }

            // Add force up to give a nice effect
            if (rb) {
                //rb.velocity = Vector2.zero; // override physics
                rb.AddForce(Vector2.up * 6 * rb.mass, ForceMode2D.Impulse);
            }
        }

        gameObject.layer = LayerMask.NameToLayer("EnemyIgnore");

        // disable colliders (so it would go through the floor and fall out of the map)
        //GetComponent<Collider2D>().enabled = false;
        base.Die();

        //GrappleHookDJ grappleScript = player.GetComponent<GrappleHookDJ>();
        //if (grappleScript && grappleScript.grabbedObj == this.gameObject) {
        //    grappleScript.EndGrapple();
        //}
    }
}
