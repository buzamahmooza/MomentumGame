using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyHealth : HealthScript
{
    [SerializeField] public int scoreValue = 0;  // assigned in inspector
    [SerializeField] private GameObject floatingText;  // assigned in inspector
    private GameObject player;
    private EnemyAI enemyAI;

    private new void Awake() {
        base.Awake();
        player = GameObject.FindGameObjectWithTag("Player");
        enemyAI = GetComponent<EnemyAI>();
    }

    public override void TakeDamage(int damageAmount) {
        base.TakeDamage(damageAmount);
        CreateFloatingDamage(damageAmount);
    }

    public IEnumerator EnumStun(float seconds) {
        if (enemyAI == null) {
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
        Debug.Assert(this.floatingText != null);
        GameObject floatingDamageInstance = (Instantiate(this.floatingText, transform.position, Quaternion.identity) as GameObject);
        FloatingText floatingText = floatingDamageInstance.GetComponent<FloatingText>();
        floatingText.InitBounceDmg(damageValue);
        floatingText.text.color = Color.Lerp(Color.yellow, Color.red, (float)damageValue / startHealth);
    }

    private void CreateFloatingScore(int scoreVal) {
        Debug.Assert(floatingText != null);
        (Instantiate(floatingText, transform.position, Quaternion.identity) as GameObject).GetComponent<FloatingText>().InitFloatingScore(scoreVal);
    }

    public override void Die() {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        if (enemyAI) enemyAI.enabled = false;

        if (!isDead) {
            if (scoreValue > 0)
                CreateFloatingScore(scoreValue);
            if (rb) {
                // Add force up to give a nice effect
                //rb.velocity = Vector2.zero; // override physics
                rb.AddForce(Vector2.up * 6 * rb.mass, ForceMode2D.Impulse);
            }
        }

        Destroy(GetComponent<Collider2D>()); //Destroy colliders (so it would go through the floor and fall out of the map)
        base.Die();

        GrappleHookDJ grappleScript = player.GetComponent<GrappleHookDJ>();
        if (grappleScript && grappleScript.grabbedObj == this.gameObject) {
            grappleScript.EndGrapple();
        }
    }
}
