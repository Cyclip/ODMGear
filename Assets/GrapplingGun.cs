using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrapplingGun : MonoBehaviour
{
    private LineRenderer lr;
    private float nextTime; // Cooldown for grappling
    private float lastDistance;   // For swinging
    private Rigidbody rb;

    // Variables to be accessed
    public bool isGrappling = false;
    public Vector3 grapplePoint;
    public SpringJoint joint;

    public LayerMask grappleMask;
    public Transform gunTip, camera, player;
    public float maxDistance = 100f;
    public float springMax = 0.8f;
    public float springMin = 0.1f;
    public float springSpring = 8f;
    public float springDamper = 7f;
    public float springMassScale = 4.5f;
    public float coolDown = 0.3f;
    public float swingSpeed = 2f;

    // Sounds
    public AudioSource shootCoilSound;
    public AudioSource retractCoilSound;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
    }

    void Start()
    {
        rb = player.GetComponent<Rigidbody>();
    }

    void Update()
    {

        if (Input.GetAxisRaw("Shoot/Withdraw Coil") != 0 && Time.time > nextTime)
        {
            nextTime = Time.time + coolDown;
            if (!isGrappling)
            {
                StartGrapple();
            }
            else
            {
                StopGrapple();
            }

            if (isGrappling)
            {
                float currentDistance = Vector3.Distance(player.position, grapplePoint);
                if (lastDistance < currentDistance)
                {
                    // They moved towards the grapple, so swing
                    Vector3 velocity = rb.velocity * swingSpeed;
                    rb.AddForce(velocity);
                }
            }
        }

    }

    void LateUpdate()
    {
        DrawRope();
    }

    void StartGrapple()
    {

        // Raycast to see if they hit anything
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, maxDistance, grappleMask))
        {
            grapplePoint = hit.point; // Set the point to grapple onto the hitpoint
            joint = player.gameObject.AddComponent<SpringJoint>(); // Add a spring joint
            joint.autoConfigureConnectedAnchor = false; // Don't configure position
            joint.connectedAnchor = grapplePoint; // Specify the position

            // Try to keep this amount of distance from grapple point
            float distanceFromPoint = Vector3.Distance(player.position, grapplePoint);
            joint.maxDistance = distanceFromPoint * springMax;
            joint.minDistance = distanceFromPoint * springMin;

            joint.spring = springSpring;
            joint.damper = springDamper;
            joint.massScale = springMassScale;

            lr.positionCount = 2;
            isGrappling = true;
            shootCoilSound.Play();
        } else
        {
            isGrappling = false;
        }
    }

    void DrawRope()
    {
        if (!joint) return;

        lr.SetPosition(0, gunTip.position); // Point 0: Guntip
        lr.SetPosition(1, grapplePoint);    // Point 1: Grapple point
    }

    void StopGrapple ()
    {
        if (isGrappling)
        {
            retractCoilSound.Play();

            lr.positionCount = 0;
            isGrappling = false;
            Destroy(joint);
        }
    }
}
