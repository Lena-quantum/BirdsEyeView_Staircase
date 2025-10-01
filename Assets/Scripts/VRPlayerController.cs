using UnityEngine;
using UnityEngine.XR;

public class VRPlayerController : MonoBehaviour
{
    [Header("VR Settings")]
    public float moveSpeed = 3.0f;
    public float rotationSpeed = 90.0f;
    
    [Header("Input References")]
    public XRNode leftHandNode = XRNode.LeftHand;
    public XRNode rightHandNode = XRNode.RightHand;
    
    private CharacterController characterController;
    private Camera playerCamera;
    
    void Start()
    {
        // Get components
        characterController = GetComponent<CharacterController>();
        playerCamera = Camera.main;
        
        // Ensure we have a character controller
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.3f;
        }
        
        // Set up camera for VR
        if (playerCamera != null)
        {
            playerCamera.transform.localPosition = new Vector3(0, 1.6f, 0);
        }
    }
    
    void Update()
    {
        HandleMovement();
        HandleRotation();
    }
    
    void HandleMovement()
    {
        // Get input from VR controllers
        Vector2 leftThumbstick = Vector2.zero;
        Vector2 rightThumbstick = Vector2.zero;
        
        // Try to get thumbstick input from left controller
        if (InputDevices.GetDeviceAtXRNode(leftHandNode).TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 leftInput))
        {
            leftThumbstick = leftInput;
        }
        
        // Try to get thumbstick input from right controller
        if (InputDevices.GetDeviceAtXRNode(rightHandNode).TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 rightInput))
        {
            rightThumbstick = rightInput;
        }
        
        // Use left thumbstick for movement
        Vector3 moveDirection = new Vector3(leftThumbstick.x, 0, leftThumbstick.y);
        moveDirection = playerCamera.transform.TransformDirection(moveDirection);
        moveDirection.y = 0; // Keep movement horizontal
        
        // Apply gravity
        moveDirection.y = Physics.gravity.y;
        
        // Move the character
        characterController.Move(moveDirection * moveSpeed * Time.deltaTime);
    }
    
    void HandleRotation()
    {
        // Get input from VR controllers for rotation
        Vector2 rightThumbstick = Vector2.zero;
        
        if (InputDevices.GetDeviceAtXRNode(rightHandNode).TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 rightInput))
        {
            rightThumbstick = rightInput;
        }
        
        // Use right thumbstick X axis for rotation
        float rotationInput = rightThumbstick.x;
        
        if (Mathf.Abs(rotationInput) > 0.1f)
        {
            transform.Rotate(0, rotationInput * rotationSpeed * Time.deltaTime, 0);
        }
    }
}

