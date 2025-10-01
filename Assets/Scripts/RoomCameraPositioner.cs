using UnityEngine;

public class RoomCameraPositioner : MonoBehaviour
{
    [Header("Room Model")]
    public GameObject roomModel;
    
    [Header("Camera Settings")]
    public Transform vrCamera;
    public float eyeHeight = 1.6f;
    
    [Header("Auto Position")]
    public bool autoCenterOnStart = true;
    
    void Start()
    {
        if (autoCenterOnStart && roomModel != null)
        {
            CenterCameraInRoom();
        }
    }
    
    [ContextMenu("Center Camera in Room")]
    public void CenterCameraInRoom()
    {
        if (roomModel == null)
        {
            Debug.LogError("Room model is not assigned!");
            return;
        }
        
        // Get the room's bounds
        Renderer roomRenderer = roomModel.GetComponent<Renderer>();
        if (roomRenderer != null)
        {
            Bounds roomBounds = roomRenderer.bounds;
            
            // Calculate center position
            Vector3 roomCenter = roomBounds.center;
            roomCenter.y = eyeHeight; // Set to eye height
            
            // Position the VR camera
            if (vrCamera != null)
            {
                vrCamera.position = roomCenter;
                Debug.Log($"Camera positioned at room center: {roomCenter}");
            }
            else
            {
                // If no specific camera assigned, try to find the main camera
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    mainCamera.transform.position = roomCenter;
                    Debug.Log($"Main camera positioned at room center: {roomCenter}");
                }
            }
        }
        else
        {
            Debug.LogWarning("Room model doesn't have a Renderer component. Positioning at origin.");
            if (vrCamera != null)
            {
                vrCamera.position = new Vector3(0, eyeHeight, 0);
            }
        }
    }
    
    // Method to manually set camera position
    public void SetCameraPosition(Vector3 position)
    {
        if (vrCamera != null)
        {
            vrCamera.position = position;
            Debug.Log($"Camera positioned at: {position}");
        }
    }
    
    // Method to get room bounds for reference
    public Bounds GetRoomBounds()
    {
        if (roomModel != null)
        {
            Renderer roomRenderer = roomModel.GetComponent<Renderer>();
            if (roomRenderer != null)
            {
                return roomRenderer.bounds;
            }
        }
        return new Bounds();
    }
}

