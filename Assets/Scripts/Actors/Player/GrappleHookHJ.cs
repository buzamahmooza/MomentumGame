using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
// ReSharper disable UnusedVariable

[RequireComponent(typeof(HingeJoint2D))]
public class GrappleHookHJ : GrappleHook
{
    // Terms:   When I say "grapple", "flying" I mean that the player should grapple toward the object.
    //          And when I say "pull", I mean that the player should by pulling the grappled object.
    [NonSerialized] public new HingeJoint2D Joint;

    /// <summary> the world position of where the join anchor is located (used only for drawing debug lines) </summary>
    private Vector3 AnchorVec3
    {
        get { return transform.position + (Vector3) Joint.anchor; }
    }

    private void Awake()
    {
        if (mask == 0) mask = LayerMask.GetMask("Default", "Floor", "Enemy", "Object");
        LineRenderer = GetComponent<LineRenderer>();
        AimInput = GetComponent<AimInput>();
        PlayerMove = GetComponent<PlayerMove>();
        Anim = GetComponent<Animator>();

        Joint = GetComponent<HingeJoint2D>();
        Joint.enableCollision = true;
        Joint.enabled = false;
    }

    private void Update()
    {
        if (InputPressed && !Flying)
            FindTarget();

        if (Flying)
        {
            Fly();
            Debug.DrawLine(AnchorVec3, Joint.connectedAnchor, Color.red);
        }
        else if (Pulling)
        {
            Pull();
            Debug.DrawLine(AnchorVec3, Joint.connectedBody.gameObject.transform.position, Color.blue);
        }
        else
        {
            EndGrapple();
        }

        //If reached Target
        if (!Pulling && Vector2.Distance(transform.position, Target) < 0.5f)
            EndGrapple();
        // if released input
        if (InputReleased && releaseGrappleOnInputRelease)
            EndGrapple();
    }

    private bool InputPressed
    {
        get
        {
            return
#if !(UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_IPHONE)
                Input.GetKeyDown(KeyCode.LeftShift) ||
                Input.GetMouseButtonDown(1) ||
#endif
                Input.GetAxisRaw("LeftTrigger") > 0.5f;
        }
    }

    private bool InputReleased
    {
        get
        {
            return Input.GetMouseButtonUp(1) || Input.GetKeyUp(KeyCode.LeftShift) ||
                   AimInput.UsingJoystick && Input.GetAxisRaw("LeftTrigger") < 0.3f;
        }
    }

    private void FindTarget()
    {
        // use mouse direction if using mouse
        Vector2 direction = AimInput.UsingMouse
            ? AimInput.AimDirection
            : PlayerMove.MovementInput.magnitude > 0.1f // if no movement input
                ? PlayerMove.MovementInput // use movement input 
                : Vector2.right * PlayerMove.FacingSign; // otherwise just use player facingSign

        foreach (RaycastHit2D hit in Physics2D.RaycastAll(gun.position, direction, maxGrappleRange, mask))
        {
            bool grappleConditions = hit && hit.collider != null &&
                                     hit.collider.gameObject != gameObject;
            bool pullConditions = hit.collider.gameObject.GetComponent<Health>() != null &&
                                  hit.collider.attachedRigidbody != null;

            if (Anim.GetBool("Slamming")) continue; //  not allowed to grapple while slamming

            LineRenderer.enabled = true;
            Target = hit.point;
            RenderLine();
            ConfigureDistance();
            GrabbedObj = hit.collider.gameObject;
            /* 
             * targetPointOffset is the offset vector between the grabbedObj.position and the hit.point, 
             * without this, grabbing will always grab the origin position of the other object,  
             * but we want the grabbed position, grabbed position = other_position + targetPointOffset
             */
            TargetPointOffset = hit.point - (Vector2) GrabbedObj.transform.position;

            if (pullConditions)
            {
                Pulling = true;
                Joint.connectedAnchor = TargetPointOffset;
                return;
            }
            else if (grappleConditions)
            {
                Joint.connectedAnchor = hit.point /*+ (Vector2)targetPointOffset*/;
                Flying = true;
                return;
            }
        }
    }

    private void RenderLine()
    {
        LineRenderer.SetPosition(0, gun.position);
        Vector2 endVec =
            Pulling ? (Vector2) (GrabbedObj.transform.position + TargetPointOffset) : Joint.connectedAnchor;
        LineRenderer.SetPosition(1, endVec);
    }

    private void CloseDistance()
    {
        Vector2 inputVec = new Vector2(CrossPlatformInputManager.GetAxis("Horizontal"),
            CrossPlatformInputManager.GetAxis("Vertical"));

        Vector2 grappleDir = (Flying ? Joint.connectedAnchor :
                                 Pulling ? (Vector2) (Joint.connectedBody.gameObject.transform.position) :
                                 Vector2.zero) - (Vector2) AnchorVec3;

//        float dot = Vector2.Dot(grappleDir.normalized, inputVec.normalized);
//        float smoother = speed * Time.deltaTime / Vector2.Distance(AnchorVec3, Target);

//        float newDistance = m_Flying
//                ? // if flying, make the newDistance depend on the input
//                Mathf.Lerp(Joint.distance - dot * speed * Time.deltaTime * (m_Pulling ? -1 : 1), 0, smoother)
//                : /*invert controls if pulling objects*/
//                Mathf.Lerp(Joint.distance, 0, smoother) // else if pulling, always pull (keep decreasing the distance)
//            ;
//
//        Joint.distance = Mathf.Clamp(newDistance, 0, maxDistance);
    }

    private void Fly()
    {
        Joint.enabled = true;
        if (GrabbedObj == null)
        {
            Joint.connectedBody = null;
        }

        CloseDistance();
        RenderLine();
    }

    private void Pull()
    {
        Joint.enabled = true;

        Rigidbody2D otherRigidbody = GrabbedObj ? GrabbedObj.GetComponent<Rigidbody2D>() : null;
        Joint.connectedBody = otherRigidbody;

        if (otherRigidbody && !otherRigidbody.bodyType.Equals(RigidbodyType2D.Static))
        {
            // if player is facing a direction other than that of the otherRigidbody, Flip()
            bool otherIsToTheRight = otherRigidbody.gameObject.transform.position.x > transform.position.x;
            if (otherIsToTheRight ^ PlayerMove.FacingRight)
            {
                PlayerMove.Flip();
            }

            CloseDistance();
            RenderLine();
        }
        else if (!otherRigidbody)
        {
            Debug.LogError("Connected body is null!");
            EndGrapple();
            return;
        }
    }

    private void ConfigureDistance()
    {
//        Joint.distance = Vector3.Distance(AnchorVec3, Target);
//        maxDistance = Joint.distance;
    }
    
    public new void EndGrapple()
    {
        Joint.connectedBody = null;
        Joint.enabled = false;
        LineRenderer.enabled = false;
        Flying = false;
        Pulling = false;
        PlayerMove.BlockMoveInput = false;
        Target = transform.position;
        GrabbedObj = null;
    }
}