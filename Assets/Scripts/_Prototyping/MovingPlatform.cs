using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour {


    public Transform platform, startTransform, endTransform;

    private Vector3 direction, target, startPos, endPos;
    private Transform destination;
    public float speed = 5;
    private Rigidbody2D rb;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(startTransform.position, platform.localScale);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(endTransform.position, platform.localScale);
    }

    private void Awake()
    {
        if (platform == null) platform = transform;
        if (startTransform== null) startPos = transform.position;
        if (endTransform== null) endPos= transform.position + Vector3.right*5;
        rb = platform.gameObject.GetComponent<Rigidbody2D>();
        if (rb == null) rb = platform.gameObject.AddComponent<Rigidbody2D>();
        rb.isKinematic = true;
        rb.gravityScale = 0;
    }

    // Use this for initialization
    private void Start () {
        startPos = startTransform.position;
        endPos = endTransform.position;
        target = startTransform.position;
	}
	
	// Update is called once per frame
    private void FixedUpdate () {
        //platform.position = Vector2.Lerp(platform.position, target, Time.fixedDeltaTime*speed);
        rb.MovePosition(speed * Vector2.right * Time.fixedDeltaTime * (target.x-platform.position.x));
        // Changes direction
        if (Vector2.Distance(platform.position, target) < speed * Time.fixedDeltaTime)
        {
            target = target == startPos ? endPos : startPos;
        }
    }
}
