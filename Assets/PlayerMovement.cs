using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using System.Linq;

public class PlayerMovement : MonoBehaviour
{
    // Public variables
    public float moveSpeed = 15f;
    public float maxSpeed = 60f;
    public float maxRunSpeed = 75f;
    public float counterSpeedAir = 0.4f;
    public float counterSpeedGround = 0.125f;
    public float jumpHeight = 2f;
    public float groundDistance = 0.4f;
    public float slideThreshold = 5f;
    public float maxAirSpeed = 60f;
    public float airSlowdown = 0.8f;
    public float gasSpeed = 100f;
    public float airMovementSlowdown = 0.2f;
    public float fallingGravity = 50f;

    public int footstepMagnitudeThreshold = 10;
    public float stepCooldown = 0.2f;

    public Transform wallCollisionCheck;
    public Transform mainBody;
    public LayerMask groundLayerMask;
    public LayerMask moveableGroundLayerMask;
    public Transform groundCheck;
    public Rigidbody rb; // Apply forces
    public Transform camera;
    public Transform cameraOrientation;
    public ParticleSystem gasParticles;

    public AudioSource gasEmitSound;
    public AudioSource gasEndSound;
    public AudioSource retractCoilLoop;
    public AudioSource retractCoilLoopEnd;
    public AudioSource windLoop;

    public AudioSource stepAudioSource;
    public AudioClip StepDirt_1;
    public AudioClip StepStone_1;
    public AudioClip StepWater_1;
    public AudioClip StepWood_1;

    // Grappling
    public Transform gun1, gun2;
    public float retractionSpeed = 2f;
    public float retractForceStrength = 4f;

    // Private variables
    private bool onGround = true;
    private bool isRunning = false;
    private bool emittingGas = false;    // Grapple (gas to propel)
    private bool retractingCoil = false; // Grapple (retract coil to move towards)
    private bool jumping = false;
    private float lastGrapple;
    private GrapplingGun gun1Access;
    private GrapplingGun gun2Access;
    private string[] materialTypes = new string[4] { "dirt", "water", "stone", "wood" }; // Different types of childtags
    private float nextStep;

    public float windDistanceStart = 250f;

    float x, y;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        gasParticles.Stop();

    }

    void LateStart()
    {
        gun1Access = GunComponent(gun1);
        gun2Access = GunComponent(gun2);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateVariables();
    }

    void FixedUpdate()
    {
        Movement();
    }

    void HandleGrapplingMovements()
    {
        lastGrapple = Time.time;
        // If they are retracting coil, change the coil max distance
        if (retractingCoil)
        {

            if (IsGrappling(0))
            {
                gun1.GetComponent<GrapplingGun>().joint.maxDistance -= retractionSpeed;
                Vector3 retractForceVector = (gun1.GetComponent<GrapplingGun>().grapplePoint - transform.position).normalized * retractForceStrength * Time.smoothDeltaTime * gun1.GetComponent<GrapplingGun>().joint.maxDistance;
                rb.AddForce(retractForceVector);
            }
            if (IsGrappling(1))
            {
                gun2.GetComponent<GrapplingGun>().joint.maxDistance -= retractionSpeed;
                Vector3 retractForceVector = (gun2.GetComponent<GrapplingGun>().grapplePoint - transform.position).normalized * retractForceStrength * Time.smoothDeltaTime * gun2.GetComponent<GrapplingGun>().joint.maxDistance;
                rb.AddForce(retractForceVector);
            }
            if (!retractCoilLoop.isPlaying)
            {
                retractCoilLoop.Play();
            }
        }
        if (emittingGas)
        {
            // Increase the gas speed but clamp it
            Vector3 gasVector = camera.transform.forward * gasSpeed;
            rb.AddForce(gasVector);
            if (!gasParticles.isEmitting)
            {
                // If it's not emitting..
                gasParticles.Play();
                gasEmitSound.Play();
            }
        }
        else if (gasParticles.isEmitting)
        {
            // Call only if its emitting
            gasParticles.Stop();
            gasEmitSound.Stop();
            gasEndSound.Play();
        }
    }

    void Movement()
    {
        /*
         * X = Horizontal (left/right)
         * Y = Is not actually Y it's Z but it's vertical (forward/back)
         */

        // If they are on the ground and they want to jump, you must let it be
        if (Input.GetButton("Jump") && onGround)
        {
            jumping = true;
            rb.AddForce(Vector3.up * 100 * jumpHeight);
        }
        else if (!onGround)
        {
            if (!retractingCoil && !emittingGas)
            {
                rb.AddForce(Vector3.down * fallingGravity);
            }
        }

        // Stop from sliding and stuff
        CounterMovement();

        // After counter movement bc s p e e d
        if (IsGrappling(2))
        {
            HandleGrapplingMovements();
        }
        else if (gasParticles.isEmitting)
        {
            emittingGas = false;
            gasParticles.Stop();
            gasEmitSound.Stop();
            gasEndSound.Play();
        }

        if (!retractingCoil && retractCoilLoop.isPlaying)
        {
            retractCoilLoop.Stop();
            retractCoilLoopEnd.Play();
        }

        Vector3 finalForceVector = transform.forward * y + transform.right * x;

        finalForceVector = CheckWallCollisions(finalForceVector);

        // Apply forces
        rb.AddForce(finalForceVector);

        // Wind speed sounds
        float movementSpeed = rb.velocity.magnitude;
        if (movementSpeed > windDistanceStart && !windLoop.isPlaying)
        {
            windLoop.Play();
        }
        else if (movementSpeed < windDistanceStart && windLoop.isPlaying)
        {
            windLoop.Stop();
        }

        // Footstep sounds
        Footsteps();
    }

    Vector3 CheckWallCollisions(Vector3 finalForceVector)
    {
        RaycastHit hit;
        Debug.DrawRay(wallCollisionCheck.transform.position, wallCollisionCheck.transform.forward * 1);
        if (Physics.Raycast(wallCollisionCheck.transform.position, wallCollisionCheck.transform.forward, out hit, 1f))
        {
            return new Vector3(0, 0, 0);
        }
        return finalForceVector;
    }

    void Footsteps()
    {
        if (rb.velocity.magnitude > footstepMagnitudeThreshold && onGround && Time.time > nextStep)
        {
            nextStep = Time.time + stepCooldown;
            string tag = GetStandingTag();
            if (tag == null) return;

            if (tag == "dirt")
            {
                stepAudioSource.PlayOneShot(StepDirt_1, 0.2f);
            }
            else if (tag == "Water")
            {
                stepAudioSource.PlayOneShot(StepWater_1, 0.2f);
            }
            else if (tag == "stone")
            {
                stepAudioSource.PlayOneShot(StepStone_1, 0.2f);
            }
            else if (tag == "wood")
            {
                stepAudioSource.PlayOneShot(StepWood_1, 0.2f);
            }
        }
    }

    string GetStandingTag()
    {
        // Create raycast downwards
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit))
        {
            Transform hitGameObject = hit.collider.gameObject.transform;
            foreach (Transform child in hitGameObject)
            {
                if (materialTypes.Contains(child.tag))
                {
                    return child.tag;
                }
            }
        }
        return null;
    }

    bool IsGrappling(int index)
    {
        if (index == 0)
        {
            return GunComponent(gun1).joint != null;
        }
        else if (index == 1)
        {
            return GunComponent(gun2).joint != null;
        }
        else if (index == 2)
        {
            return GunComponent(gun1).joint != null || GunComponent(gun2).joint != null;
        }
        return false;
    }

    GrapplingGun GunComponent(Transform gun)
    {
        return gun.GetComponent<GrapplingGun>();
    }

    void CounterMovement()
    {
        // Air movements are vital
        if (!onGround)
        {
            return;
            // If grappling or emitting gas
            if (IsGrappling(2) || emittingGas)
            {
                x = Mathf.Clamp(x, -maxAirSpeed * 2.5f, maxAirSpeed * 2.5f);
                y = Mathf.Clamp(y, -maxAirSpeed * 2.5f, maxAirSpeed * 2.5f);
            }
            else
            {
                x *= airMovementSlowdown;
                y *= airMovementSlowdown;

                x = Mathf.Clamp(x, -maxAirSpeed, maxAirSpeed);
                y = Mathf.Clamp(y, -maxAirSpeed, maxAirSpeed);
            }

            return;

        }

        // Clamp so they aren't too fast
        if (isRunning)
        {
            x = Mathf.Clamp(x, -maxRunSpeed, maxRunSpeed);
            y = Mathf.Clamp(y, -maxRunSpeed, maxRunSpeed);
        }
        else
        {
            x = Mathf.Clamp(x, -maxSpeed, maxSpeed);
            y = Mathf.Clamp(y, -maxSpeed, maxSpeed);
        }

        /*/ Threshold to stop sliding
        if (Mathf.Abs(x) < slideThreshold)
        {
            rb.AddForce(transform.forward * -(x / 2));
            x = 0;
        }
        if (Mathf.Abs(y) < slideThreshold)
        {
            rb.AddForce(transform.forward * -(y / 2));
            y = 0;
        }*/
    }

    void UpdateVariables()
    {
        x = Input.GetAxisRaw("Horizontal") * moveSpeed * Time.deltaTime * 150;
        y = Input.GetAxisRaw("Vertical") * moveSpeed * Time.deltaTime * 150;

        onGround = isOnGround();
        isRunning = Input.GetAxisRaw("Run") != 0 && !IsGrappling(2);               // LShift and not grappling
        emittingGas = Input.GetAxisRaw("Emit Gas") != 0 && IsGrappling(2);         // LShift and grappling (use gas instead of running)
        retractingCoil = Input.GetAxisRaw("Retract Coil") != 0 && IsGrappling(2);  // LCtrl  and grappling
    }

    bool isOnGround()
    {
        return Physics.CheckSphere(groundCheck.position, groundDistance, groundLayerMask) || Physics.CheckSphere(groundCheck.position, groundDistance, moveableGroundLayerMask);
    }

}


