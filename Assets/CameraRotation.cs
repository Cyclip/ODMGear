using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

public class CameraRotation : MonoBehaviour
{
    public Transform playerBody;
    public Transform glasses;
    public float mouseSensitivity = 10f;
    public float sensitivityMultiplier = 12f;

    private float xRotation;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Camera camera = GetComponent<Camera>();
        float[] distances = new float[32];

        // distance[layer #] = distance
        distances[11] = 15; // HouseObjects
        distances[12] = 10; // HouseDetails
        distances[13] = 10; // Environment

        camera.layerCullDistances = distances;
    }

    // Update is called once per frame
    void Update()
    {
        float x = Input.GetAxis("Mouse X") * mouseSensitivity * sensitivityMultiplier * Time.deltaTime;
        float y = Input.GetAxis("Mouse Y") * mouseSensitivity * sensitivityMultiplier * Time.deltaTime;

        xRotation -= y;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Rotate
        playerBody.Rotate(Vector3.up * x);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        glasses.localRotation = Quaternion.Euler(Mathf.Clamp(xRotation, -90f, 12f), 0f, 0f);
    }
}
