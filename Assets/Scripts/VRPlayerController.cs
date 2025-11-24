using UnityEngine;
using UnityEngine.XR;

public class VRPlayerController : MonoBehaviour
{
    [Header("VR Settings")]
    public float moveSpeed = 3.0f;
    public float verticalSpeed = 0.5f; // Vertical (up/down) movement speed - adjust in Inspector if too fast
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
        bool leftPrimaryButton = false;   // X button
        bool leftSecondaryButton = false; // Y button
        
        // Get left controller input
        InputDevice leftDevice = InputDevices.GetDeviceAtXRNode(leftHandNode);
        if (leftDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 leftInput))
        {
            leftThumbstick = leftInput;
        }
        
        // Get LEFT controller button input for vertical movement (X = up, Y = down)
        if (leftDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool xButton))
        {
            leftPrimaryButton = xButton;
        }
        if (leftDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out bool yButton))
        {
            leftSecondaryButton = yButton;
        }
        
        // Use left thumbstick for horizontal movement
        Vector3 horizontalMove = new Vector3(leftThumbstick.x, 0, leftThumbstick.y);
        horizontalMove = playerCamera.transform.TransformDirection(horizontalMove);
        horizontalMove.y = 0; // Keep horizontal movement flat
        
        // Use LEFT X/Y buttons for vertical movement: X (primary) = up, Y (secondary) = down
        float verticalInput = 0f;
        if (leftPrimaryButton) verticalInput += 1f;
        if (leftSecondaryButton) verticalInput -= 1f;
        
        // Combine horizontal and vertical movement with their respective speeds
        Vector3 finalMovement = horizontalMove * moveSpeed * Time.deltaTime;
        finalMovement.y = verticalInput * verticalSpeed * Time.deltaTime;
        
        // Move the character
        characterController.Move(finalMovement);
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

