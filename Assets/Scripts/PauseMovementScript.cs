using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMovementScript : MonoBehaviour {

    private Animator animator;
    private Rigidbody2D rb2d;

    private bool _isKinematic = false;
    private float _animatorSpeed;

    private void Awake() {
        rb2d = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (rb2d) {
            _isKinematic = rb2d.isKinematic;
        }
        if (animator) {
            _animatorSpeed = animator.speed;
        }
    }

    public void PauseMovement() {
        if (rb2d) {
            rb2d.isKinematic = true;
        }
        if (animator) {
            animator.speed = 0.0f;
        }
    }
    public void UnpauseMovement() {
        if (rb2d) {
            rb2d.isKinematic = _isKinematic;
        }
        if (animator) {
            animator.speed = _animatorSpeed;
        }
    }
}
