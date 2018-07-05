using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FloatingText : MonoBehaviour
{

    [HideInInspector] public Text text;
    [HideInInspector] public Rigidbody2D rb;

    [SerializeField] private float duration = 2;
    [SerializeField] [Range(0, 1)] private float smooth = 0.5f;
    [SerializeField] float defaultBounceSpeed = 3;

    private void Awake() {
        text = GetComponentInChildren<Text>();
        rb = GetComponent<Rigidbody2D>() ?? gameObject.AddComponent<Rigidbody2D>();
    }

    private void Start() {
        Destroy(gameObject, 10);
    }
    public void InitBounceDmg(int damageValue) {
        Init(damageValue + "", defaultBounceSpeed * damageValue / 50 * (UnityEngine.Random.insideUnitCircle + Vector2.up), true);
        gameObject.transform.localScale *= 0.6f;
    }
    public void InitFloatingScore(int scoreValue) {
        Init(scoreValue + "pts");
    }
    public void Init(string newText) {
        Init(newText, Vector3.up * 10, false);
    }
    public void Init(string newText, Vector3 movement, bool gravity) {
        text.text = newText;
        
        rb.AddForce(movement, ForceMode2D.Impulse);
        if (!gravity) rb.gravityScale = 0;
        //duration = 5 / Mathf.Sqrt(movement.magnitude);
    }

    private void FixedUpdate() {
        text.color = Color.Lerp(text.color, Color.clear, Mathf.Exp(1 - duration * smooth));
        duration -= Time.fixedDeltaTime;
        transform.LookAt(Camera.main.transform.position);
    }
}
