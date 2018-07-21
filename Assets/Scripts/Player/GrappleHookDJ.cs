using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

[RequireComponent(typeof(Shooter))]
public class GrappleHookDJ : MonoBehaviour
{
    // Terms:   When I say "grapple", "flying" I mean that the player should grapple toward the object.
    //          And when I say "pull", I mean that the player should by pulling the grappled object.

    [HideInInspector] public bool m_Flying = false, m_Pulling = false;
    [HideInInspector] public GameObject grabbedObj;
    [HideInInspector] public Vector3 target;

    // assigned in the Inspector
    [SerializeField] Transform gun;
    [SerializeField] LayerMask mask;
    [SerializeField] float speed = 5;
    [SerializeField] float maxGrappleRange = 300;
    [SerializeField] bool releaseGrappleOnInputRelease = true;

    Vector3 targetPointOffset;
    AimInput aimInput;
    Animator m_Anim;
    PlayerMove playerMove;
    LineRenderer lr;
    DistanceJoint2D joint;
    float maxDistance = 10.0f;

    public Vector3 AnchorVec3 {
        get {
            return transform.position + new Vector3(joint.anchor.x, joint.anchor.y, 0);
        }
    }

    private void Awake() {
        if (mask == 0) mask = LayerMask.NameToLayer("Floor");
        lr = GetComponent<LineRenderer>();
        aimInput = GetComponent<AimInput>();
        playerMove = GetComponent<PlayerMove>();
        m_Anim = GetComponent<Animator>();

        joint = GetComponent<DistanceJoint2D>() ?? gameObject.AddComponent<DistanceJoint2D>();
        joint.maxDistanceOnly = true;
        joint.enableCollision = true;
        joint.enabled = false;
    }

    private void Update() {
        if (InputPressed && !m_Flying)
            FindTarget();

        if (m_Flying) {
            Fly();
            Debug.DrawLine(AnchorVec3, joint.connectedAnchor, Color.red);
        } else if (m_Pulling) {
            Pull(grabbedObj);
            Debug.DrawLine(AnchorVec3, joint.connectedBody.gameObject.transform.position, Color.blue);
        } else {
            EndGrapple();
        }

        //If reached Target
        if (!m_Pulling && Vector2.Distance(transform.position, target) < 0.5f)
            EndGrapple();
        // if released input
        if (InputReleased && releaseGrappleOnInputRelease)
            EndGrapple();
    }

    private bool InputPressed {
        get {
            return Input.GetKeyDown(KeyCode.LeftShift) || Input.GetMouseButtonDown(1) || Input.GetAxisRaw("LeftTrigger") > 0.5f;
        }
    }
    private bool InputReleased {
        get {
            return Input.GetMouseButtonUp(1) || Input.GetKeyUp(KeyCode.LeftShift) || aimInput.usingJoystick && Input.GetAxisRaw("LeftTrigger") < 0.3f;
        }
    }

    private void FindTarget() {
        foreach (RaycastHit2D hit in Physics2D.RaycastAll(gun.position, playerMove.CrossPlatformInput, maxGrappleRange, mask)) {
            bool grappleConditions = hit && hit.collider != null &&
                                     hit.collider.gameObject != gameObject;
            bool pullConditions = hit.collider.gameObject.GetComponent<Health>() != null &&
                                  hit.collider.attachedRigidbody != null;

            if (m_Anim.GetBool("Slamming")) continue; //  not allowed to grapple while slamming

            lr.enabled = true;
            target = hit.point;
            RenderLine();
            ConfigureDistance();
            grabbedObj = hit.collider.gameObject;
            /* 
             * targetPointOffset is the offset vector between the grabbedObj.position and the hit.point, 
             * without this, grabbing will always grab the origin position of the other object,  
             * but we want the grabbed position, grabbed position = other_position + targetPointOffset
             */
            targetPointOffset = hit.point - (Vector2)grabbedObj.transform.position;

            if (pullConditions) {
                m_Pulling = true;
                joint.connectedAnchor = (Vector2)targetPointOffset;
                return;
            } else if (grappleConditions) {
                joint.connectedAnchor = hit.point /*+ (Vector2)targetPointOffset*/;
                m_Flying = true;
                return;
            }
        }
    }

    private void RenderLine() {
        lr.SetPosition(0, gun.position);
        Vector2 endVec = m_Pulling ? (Vector2)(grabbedObj.transform.position + targetPointOffset) : joint.connectedAnchor;
        lr.SetPosition(1, endVec);
    }

    private void CloseDistance() {
        Vector2 inputVec = new Vector2(CrossPlatformInputManager.GetAxis("Horizontal"), CrossPlatformInputManager.GetAxis("Vertical"));

        Vector2 grappleDir = (m_Flying ? (joint.connectedAnchor) :
                                 m_Pulling ? (Vector2)(joint.connectedBody.gameObject.transform.position) :
                                 Vector2.zero
                             ) - (Vector2)AnchorVec3;

        float dot = Vector2.Dot(grappleDir.normalized, inputVec.normalized);

        float smoother = speed * Time.deltaTime / Vector2.Distance(AnchorVec3, target);

        float newDistance = m_Flying ?  // if flying, make the newDistance depend on the input
                Mathf.Lerp(joint.distance - dot * speed * Time.deltaTime * (m_Pulling ? -1 : 1), 0, smoother) :/*invert controls if pulling objects*/
                Mathf.Lerp(joint.distance, 0, smoother) // else if pulling, always pull (keep decreasing the distance)
        ;

        joint.distance = Mathf.Clamp(newDistance, 0, maxDistance);
    }

    private void Fly() {
        joint.enabled = true;
        if (grabbedObj == null) {
            joint.connectedBody = null;
        }
        CloseDistance();
        RenderLine();
    }

    private void Pull(GameObject obj) {
        joint.enabled = true;

        var otherRigidbody = grabbedObj.GetComponent<Rigidbody2D>();
        joint.connectedBody = otherRigidbody;

        if (otherRigidbody && !otherRigidbody.bodyType.Equals(RigidbodyType2D.Static)) {
            // if player is facing a direction other than that of the otherRigidbody, Flip()
            bool otherIsToTheRight = otherRigidbody.gameObject.transform.position.x > transform.position.x;
            if (otherIsToTheRight ^ playerMove.FacingRight) {
                playerMove.Flip();
            }
            CloseDistance();
            RenderLine();
        } else if (!otherRigidbody) {
            Debug.LogError("Connected body is null!");
            return;
        }
    }

    private void ConfigureDistance() {
        joint.distance = Vector3.Distance(AnchorVec3, target);
        maxDistance = joint.distance;
    }

    public void EndGrapple() {
        joint.connectedBody = null;
        joint.enabled = false;
        lr.enabled = false;
        m_Flying = false;
        m_Pulling = false;
        playerMove.BlockMoveInput = false;
        target = transform.position;
        grabbedObj = null;
    }
}
