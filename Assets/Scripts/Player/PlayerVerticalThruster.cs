using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerVerticalThruster : MonoBehaviour
{
    [Header("Rig root to move (XR Origin / XR Rig)")]
    public Transform rigRoot;   // drag "XR Origin (XR Rig)" here

    [Header("Vertical Flight")]
    public float verticalSpeed = 2f;

    [Header("Input Actions")]
    public InputActionProperty flyUpAction;    // Y button (left controller)
    public InputActionProperty flyDownAction;  // X button (left controller)

    private CharacterController _characterController;

    private void Awake()
    {
        if (rigRoot == null)
            rigRoot = transform;

        // Try to find CharacterController on the rig
        _characterController = rigRoot.GetComponent<CharacterController>();
        
        if (_characterController == null)
        {
            Debug.LogWarning("PlayerVerticalThruster: No CharacterController found. Vertical movement will ignore collisions.");
        }
    }

    private void OnEnable()
    {
        flyUpAction.action?.Enable();
        flyDownAction.action?.Enable();
    }

    private void OnDisable()
    {
        flyUpAction.action?.Disable();
        flyDownAction.action?.Disable();
    }

    private void Update()
    {
        if (rigRoot == null)
            return;

        // Vertical movement from Y and X buttons
        float vertical = 0f;
        if (flyUpAction.action != null && flyUpAction.action.IsPressed()) 
            vertical += 1f;
        if (flyDownAction.action != null && flyDownAction.action.IsPressed()) 
            vertical -= 1f;

        if (vertical != 0f)
        {
            Vector3 move = Vector3.up * vertical * verticalSpeed * Time.deltaTime;
            
            // Use CharacterController for collision-aware movement
            if (_characterController != null)
            {
                _characterController.Move(move);
            }
            else
            {
                // Fallback: direct transform movement (no collisions)
                rigRoot.position += move;
            }
        }
    }
}
