﻿using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

[RequireComponent(typeof(Targeting))]
[RequireComponent(typeof(Joint2D))]
public abstract class GrappleHook : MonoBehaviour
{
    // Terms:   When I say "grapple", "flying" I mean that the player should grapple toward the object.
    //          And when I say "pull", I mean that the player should by pulling the grappled object.

    [HideInInspector] public bool Flying = false, Pulling = false;
    [HideInInspector] public GameObject GrabbedObj;
    [HideInInspector] public Vector3 Target;

    // assigned in the Inspector
    [SerializeField] protected Transform gun;
    [SerializeField] protected LayerMask mask;
    [SerializeField] protected float speed = 5;
    [SerializeField] protected float maxGrappleRange = 300;
    [SerializeField] protected bool releaseGrappleOnInputRelease = true;

    protected Vector3 TargetPointOffset;
    protected AimInput AimInput;
    protected Animator Anim;
    protected PlayerMove PlayerMove;
    protected LineRenderer LineRenderer;
    [NonSerialized] public Joint2D Joint;
    protected float MaxDistance = 10.0f;

    private void Awake()
    {
        if (mask.value == 0) mask = LayerMask.GetMask("Default", "Floor", "Enemy", "Object");
        LineRenderer = GetComponent<LineRenderer>();
        AimInput = GetComponent<AimInput>();
        PlayerMove = GetComponent<PlayerMove>();
        Anim = GetComponent<Animator>();

        Joint = GetComponent<Joint2D>();
        Joint.enableCollision = true;
        Joint.enabled = false;
    }

    public virtual void EndGrapple()
    {
        LineRenderer.enabled = false;
        Flying = false;
        Pulling = false;
        PlayerMove.BlockMoveInput = false;
        Target = transform.position;
        GrabbedObj = null;
        Joint.connectedBody = null;
        Joint.enabled = false;
    }
}