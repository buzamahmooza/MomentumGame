﻿using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace Actors.Player
{
    [RequireComponent(typeof(DistanceJoint2D))]
    public class GrappleHookDj : GrappleHook
    {
        // Terms:   When I say "grapple", "flying" I mean that the player should grapple toward the object.
        //          And when I say "pull", I mean that the player should by pulling the grappled object.

        // Terms:   When I say "grapple", "flying" I mean that the player should grapple toward the object.
        //          And when I say "pull", I mean that the player should by pulling the grappled object.
        [NonSerialized] public new DistanceJoint2D Joint;

        private Vector3 AnchorVec3 => transform.position + (Vector3) Joint.anchor;

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

        private bool InputReleased => Input.GetMouseButtonUp(1) || Input.GetKeyUp(KeyCode.LeftShift) ||
                                      AimInput.UsingJoystick && Input.GetAxisRaw("LeftTrigger") < 0.3f;

        private void Awake()
        {
            if (Mask == 0) Mask = LayerMask.GetMask("Default", "Floor", "Enemy", "Object");
            LineRenderer = GetComponent<LineRenderer>();
            AimInput = GetComponent<AimInput>();
            PlayerMove = GetComponent<PlayerMove>();
            Anim = GetComponent<Animator>();

            Joint = GetComponent<DistanceJoint2D>() ?? gameObject.AddComponent<DistanceJoint2D>();
            Joint.maxDistanceOnly = true;
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
            if (InputReleased && ReleaseGrappleOnInputRelease)
                EndGrapple();
        }

        private void FindTarget()
        {
            // use mouse direction if using mouse
            var direction = AimInput.UsingMouse
                ? AimInput.AimDirection
                : PlayerMove.MovementInput.magnitude > 0.1f // if no movement input
                    ? PlayerMove.MovementInput // use movement input 
                    : Vector2.right * PlayerMove.FacingSign; // otherwise just use player facingSign

            foreach (var hit in Physics2D.RaycastAll(Gun.position, direction, MaxGrappleRange, Mask))
            {
                var grappleConditions = hit && hit.collider != null &&
                                        hit.collider.gameObject != gameObject;
                var pullConditions = hit.collider.gameObject.GetComponent<Health>() != null &&
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

                if (grappleConditions)
                {
                    Joint.connectedAnchor = hit.point /*+ (Vector2)targetPointOffset*/;
                    Flying = true;
                    return;
                }
            }
        }

        private void RenderLine()
        {
            LineRenderer.SetPosition(0, Gun.position);
            var endVec =
                Pulling ? (Vector2) (GrabbedObj.transform.position + TargetPointOffset) : Joint.connectedAnchor;
            LineRenderer.SetPosition(1, endVec);
        }

        private void CloseDistance()
        {
            var inputVec = new Vector2(CrossPlatformInputManager.GetAxis("Horizontal"),
                CrossPlatformInputManager.GetAxis("Vertical"));

            var grappleDir = (Flying ? Joint.connectedAnchor :
                                 Pulling ? (Vector2) Joint.connectedBody.gameObject.transform.position :
                                 Vector2.zero) - (Vector2) AnchorVec3;

            var dot = Vector2.Dot(grappleDir.normalized, inputVec.normalized);

            var smoother = Speed * Time.deltaTime / Vector2.Distance(AnchorVec3, Target);

            var newDistance = Flying
                    ? // if flying, make the newDistance depend on the input
                    Mathf.Lerp(Joint.distance - dot * Speed * Time.deltaTime * (Pulling ? -1 : 1), 0, smoother)
                    : /*invert controls if pulling objects*/
                    Mathf.Lerp(Joint.distance, 0,
                        smoother) // else if pulling, always pull (keep decreasing the distance)
                ;

            Joint.distance = Mathf.Clamp(newDistance, 0, MaxDistance);
        }

        private void Fly()
        {
            Joint.enabled = true;
            if (GrabbedObj == null) Joint.connectedBody = null;

            CloseDistance();
            RenderLine();
        }

        private void Pull()
        {
            Joint.enabled = true;

            var otherRigidbody = GrabbedObj ? GrabbedObj.GetComponent<Rigidbody2D>() : null;
            Joint.connectedBody = otherRigidbody;

            if (otherRigidbody && !otherRigidbody.bodyType.Equals(RigidbodyType2D.Static))
            {
                // if player is facing a direction other than that of the otherRigidbody, Flip()
                var otherIsToTheRight = otherRigidbody.gameObject.transform.position.x > transform.position.x;
                if (otherIsToTheRight ^ PlayerMove.FacingRight) PlayerMove.Flip();

                CloseDistance();
                RenderLine();
            }
            else if (!otherRigidbody)
            {
                Debug.LogError("Connected body is null!");
                EndGrapple();
            }
        }

        private void ConfigureDistance()
        {
            Joint.distance = Vector3.Distance(AnchorVec3, Target);
            MaxDistance = Joint.distance;
        }

        public override void EndGrapple()
        {
            if (Joint)
            {
                Joint.connectedBody = null;
                Joint.enabled = false;
            }

            LineRenderer.enabled = false;
            Flying = false;
            Pulling = false;
            PlayerMove.IsMoveInputBlocked = false;
            Target = transform.position;
            GrabbedObj = null;
        }
    }
}