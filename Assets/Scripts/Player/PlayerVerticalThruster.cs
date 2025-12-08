using UnityEngine;
using UnityEngine.InputSystem;
using Points;

public class PlayerVerticalThruster : MonoBehaviour
{
    [Header("Rig root to move (XR Origin / XR Rig)")]
    public Transform rigRoot;   // drag "XR Origin (XR Rig)" here

    [Header("Vertical Flight")]
    public float verticalSpeed = 2f;

    [Header("Input Actions")]
    public InputActionProperty flyUpAction;    // Y button (left controller)
    public InputActionProperty flyDownAction;  // X button (left controller)

    [Header("Viewpoint Detection")]
    [Tooltip("Optional: Detector for inside/outside corridor. If null, will auto-find.")]
    [SerializeField] private CorridorViewpointDetector _viewpointDetector;

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

    private void Start()
    {
        // Auto-find viewpoint detector if not assigned
        if (_viewpointDetector == null)
        {
            _viewpointDetector = FindFirstObjectByType<CorridorViewpointDetector>();
            if (_viewpointDetector != null)
            {
                Debug.Log("PlayerVerticalThruster: Auto-found CorridorViewpointDetector");
            }
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
            
            // Check if we're outside the corridor
            // When outside, bypass CharacterController to avoid CorridorShell collisions
            bool isOutside = _viewpointDetector != null && !_viewpointDetector.IsInsideCorridor;
            
            if (isOutside)
            {
                // Outside corridor: Use direct transform movement to bypass CorridorShell collisions
                // This allows free vertical movement when viewing from outside
                rigRoot.position += move;
            }
            else if (_characterController != null)
            {
                // Inside corridor: Use CharacterController for collision-aware movement
                // This respects Environment layer collisions (floors, walls, etc.)
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
