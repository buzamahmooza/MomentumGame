using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class FloatingText : MonoBehaviour
{
    [HideInInspector] public Text text;
    [HideInInspector] public Rigidbody2D rb;

    [SerializeField] private float duration = 2;
    [SerializeField] float defaultBounceSpeed = 3;
    [SerializeField] [Range(0, 100)] private float smooth = 0.5f;
    [SerializeField] [Range(0, 2)] private float _travelSmoother = 0.2f;

    /// <summary>
    /// Set the destination if you want the text to go somewhere
    /// (just like in ClashOfClans, the gold goes to the gold GUI counter at the top)
    /// </summary>
    [NonSerialized] public Vector3 destination;

    private void Awake()
    {
        text = GetComponentInChildren<Text>();
        rb = GetComponent<Rigidbody2D>() ?? gameObject.AddComponent<Rigidbody2D>();
    }

    private void Start()
    {
        Destroy(gameObject, 10);
    }

    private void Update()
    {
        text.color = Color.Lerp(text.color, Color.clear, Mathf.Exp(1 - duration * smooth));
        duration -= Time.fixedDeltaTime;
        transform.LookAt(Camera.main.transform.position);

        if (destination.magnitude > 0)
        {
            var destinationWorldPoint = Camera.main.ScreenToWorldPoint(destination);
            if(Vector2.Distance(destinationWorldPoint, transform.position) < 0.1f) 
                OnReachDestination();
            
            transform.position = Vector2.MoveTowards(
                transform.position,
                destinationWorldPoint,
                Mathf.Exp(1 / Vector2.Distance(transform.position, destinationWorldPoint) * _travelSmoother)
            );
        }
    }

    private void OnReachDestination()
    {
        Destroy(gameObject);
    }


    public void InitBounceDmg(int damageValue)
    {
        Init(damageValue + "", defaultBounceSpeed * damageValue / 50 * (Random.insideUnitCircle + Vector2.up), true);
        gameObject.transform.localScale *= 0.6f;
    }

    public void InitFloatingScore(int scoreValue)
    {
        Init(scoreValue + "pts");
    }

    /// <summary>
    /// Short for <code>Init(newText, Vector3.up * 10, false);</code>
    /// </summary>
    /// <param name="newText"></param>
    public void Init(string newText)
    {
        Init(newText, Vector3.up * 10, false);
    }

    public void Init(string newText, Vector3 movement, bool gravity)
    {
        text.text = newText;

        rb.AddForce(movement, ForceMode2D.Impulse);
        if (!gravity) rb.gravityScale = 0;
    }

    public void Init(string newText, Vector3 target)
    {
        text.text = newText;
        StartCoroutine(SetDestinationDelayed(target));
    }

    private IEnumerator SetDestinationDelayed(Vector3 target)
    {
        yield return new WaitForSecondsRealtime(0.5f);
        destination = target;
    }
}