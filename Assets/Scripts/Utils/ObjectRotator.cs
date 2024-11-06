using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectRotator : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float inertiaStrength = 0.9f;
    [SerializeField] private float springStrength = 10f;
    [SerializeField] private float dampingStrength = 2f;
    [SerializeField] private float minRotationSpeed = 0.01f;

    private Vector3 targetRotationVelocity;
    private Vector3 currentRotationVelocity;
    private Vector2 lastMousePosition;
    private bool isDragging;

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            isDragging = true;
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(1))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            Vector2 mouseDelta = (Vector2)Input.mousePosition - lastMousePosition;
            lastMousePosition = Input.mousePosition;

            targetRotationVelocity = new Vector3(
                -mouseDelta.y * rotationSpeed,
                mouseDelta.x * rotationSpeed,
                0f
            );
        }
        else
        {
            targetRotationVelocity = Vector3.zero;
        }

        // Spring-damper physics system
        Vector3 springForce = (targetRotationVelocity - currentRotationVelocity) * springStrength;
        Vector3 dampingForce = -currentRotationVelocity * dampingStrength;

        // Update current velocity
        currentRotationVelocity += (springForce + dampingForce) * Time.deltaTime;

        // Apply inertia
        if (!isDragging)
        {
            currentRotationVelocity *= inertiaStrength;
        }

        // Stop if very slow
        if (currentRotationVelocity.magnitude < minRotationSpeed)
        {
            currentRotationVelocity = Vector3.zero;
        }

        // Apply rotation
        if (currentRotationVelocity.magnitude > 0)
        {
            transform.Rotate(
                currentRotationVelocity.x * Time.deltaTime,
                currentRotationVelocity.y * Time.deltaTime,
                currentRotationVelocity.z * Time.deltaTime,
                Space.World
            );
        }
    }
}
