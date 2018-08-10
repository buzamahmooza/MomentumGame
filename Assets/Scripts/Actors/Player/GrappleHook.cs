using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

[RequireComponent(typeof(Targeting))]
[RequireComponent(typeof(Joint2D))]
public abstract class GrappleHook : MonoBehaviour
{
    // Terms:   When I say "grapple", "flying" I mean that the player should grapple toward the object.
    //          And when I say "pull", I mean that the player should by pulling the grappled object.

    [HideInInspector] public bool m_Flying = false, m_Pulling = false;
    [HideInInspector] public GameObject GrabbedObj;
    [HideInInspector] public Vector3 Target;

    // assigned in the Inspector
    [SerializeField] protected Transform gun;
    [SerializeField] protected LayerMask mask;
    [SerializeField] protected float speed = 5;
    [SerializeField] protected float maxGrappleRange = 300;
    [SerializeField] protected bool releaseGrappleOnInputRelease = true;

    protected Vector3 _targetPointOffset;
    protected AimInput _aimInput;
    protected Animator m_Anim;
    protected PlayerMove playerMove;
    protected LineRenderer lr;
    [NonSerialized] public Joint2D Joint;
    protected float maxDistance = 10.0f;

    private void Awake()
    {
        if (mask == 0) mask = LayerMask.GetMask("Default", "Floor", "Enemy", "Object");
        lr = GetComponent<LineRenderer>();
        _aimInput = GetComponent<AimInput>();
        playerMove = GetComponent<PlayerMove>();
        m_Anim = GetComponent<Animator>();

        Joint = GetComponent<Joint2D>();
        Joint.enableCollision = true;
        Joint.enabled = false;
    }

    public virtual void EndGrapple()
    {
        lr.enabled = false;
        m_Flying = false;
        m_Pulling = false;
        playerMove.BlockMoveInput = false;
        Target = transform.position;
        GrabbedObj = null;
        Joint.connectedBody = null;
        Joint.enabled = false;
    }
}