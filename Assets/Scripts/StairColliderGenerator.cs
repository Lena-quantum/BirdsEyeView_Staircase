using UnityEngine;

/// <summary>
/// Automatically generates colliders below stairs that match the stair geometry.
/// Works with photogrammetric models where stairs are inclined at ~45 degrees.
/// </summary>
public class StairColliderGenerator : MonoBehaviour
{
    [Header("Stair Detection")]
    [Tooltip("The GameObject containing the stair mesh (photogrammetric model)")]
    [SerializeField] private GameObject stairMeshObject;
    
    [Tooltip("Layer mask for detecting stair surfaces")]
    [SerializeField] private LayerMask stairLayerMask = -1;
    
    [Header("Collider Settings")]
    [Tooltip("Distance below stairs to place colliders (in meters)")]
    [SerializeField] private float colliderOffset = 0.1f;
    
    [Tooltip("Thickness of the collider (in meters)")]
    [SerializeField] private float colliderThickness = 0.2f;
    
    [Tooltip("Use MeshCollider (more accurate) or BoxCollider (better performance)")]
    [SerializeField] private bool useMeshCollider = false;
    
    [Header("Auto-Detection")]
    [Tooltip("Automatically find stair mesh on this GameObject if not assigned")]
    [SerializeField] private bool autoFindStairMesh = true;
    
    [Tooltip("Parent GameObject for generated colliders")]
    [SerializeField] private Transform colliderParent;
    
    private void Start()
    {
        if (autoFindStairMesh && stairMeshObject == null)
        {
            stairMeshObject = gameObject;
        }
        
        if (stairMeshObject == null)
        {
            Debug.LogError("StairColliderGenerator: No stair mesh object assigned!");
            return;
        }
    }
    
    [ContextMenu("Generate Stair Colliders")]
    public void GenerateStairColliders()
    {
        if (stairMeshObject == null)
        {
            Debug.LogError("StairColliderGenerator: No stair mesh object assigned!");
            return;
        }
        
        // Get the mesh from the stair object
        MeshFilter meshFilter = stairMeshObject.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("StairColliderGenerator: No mesh found on stair object!");
            return;
        }
        
        Mesh stairMesh = meshFilter.sharedMesh;
        Vector3[] vertices = stairMesh.vertices;
        int[] triangles = stairMesh.triangles;
        
        // Create parent for colliders if not assigned
        if (colliderParent == null)
        {
            GameObject parentObj = new GameObject("StairColliders");
            parentObj.transform.SetParent(stairMeshObject.transform);
            parentObj.transform.localPosition = Vector3.zero;
            parentObj.transform.localRotation = Quaternion.identity;
            parentObj.transform.localScale = Vector3.one;
            colliderParent = parentObj.transform;
        }
        
        // Clear existing colliders
        ClearExistingColliders();
        
        if (useMeshCollider)
        {
            GenerateMeshCollider(stairMesh, meshFilter.transform);
        }
        else
        {
            GenerateBoxColliders(stairMesh, meshFilter.transform);
        }
        
        Debug.Log($"StairColliderGenerator: Generated colliders below stairs on {stairMeshObject.name}");
    }
    
    private void GenerateMeshCollider(Mesh stairMesh, Transform stairTransform)
    {
        // Create a duplicate mesh for the collider, offset downward
        Mesh colliderMesh = new Mesh();
        colliderMesh.name = "StairColliderMesh";
        
        Vector3[] originalVertices = stairMesh.vertices;
        Vector3[] offsetVertices = new Vector3[originalVertices.Length];
        
        // Calculate average normal to determine downward direction
        Vector3 averageNormal = CalculateAverageNormal(stairMesh);
        Vector3 offsetDirection = -averageNormal.normalized; // Downward from stair surface
        
        // Offset vertices downward
        for (int i = 0; i < originalVertices.Length; i++)
        {
            offsetVertices[i] = originalVertices[i] + offsetDirection * colliderOffset;
        }
        
        colliderMesh.vertices = offsetVertices;
        colliderMesh.triangles = stairMesh.triangles;
        colliderMesh.normals = stairMesh.normals;
        colliderMesh.uv = stairMesh.uv;
        colliderMesh.RecalculateBounds();
        
        // Create GameObject with MeshCollider
        GameObject colliderObj = new GameObject("StairCollider_Mesh");
        colliderObj.transform.SetParent(colliderParent);
        colliderObj.transform.localPosition = Vector3.zero;
        colliderObj.transform.localRotation = Quaternion.identity;
        colliderObj.transform.localScale = Vector3.one;
        
        MeshCollider meshCollider = colliderObj.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = colliderMesh;
        meshCollider.convex = false; // Stairs are typically concave
        
        // Copy layer from stair object
        colliderObj.layer = stairMeshObject.layer;
    }
    
    private void GenerateBoxColliders(Mesh stairMesh, Transform stairTransform)
    {
        // Get bounds of the stair mesh
        Bounds bounds = stairMesh.bounds;
        
        // Calculate the angle of the stairs (assuming ~45 degrees)
        Vector3 averageNormal = CalculateAverageNormal(stairMesh);
        float angle = Vector3.Angle(Vector3.up, averageNormal);
        
        // Create a box collider that matches the stair dimensions
        GameObject colliderObj = new GameObject("StairCollider_Box");
        colliderObj.transform.SetParent(colliderParent);
        
        // Position at the center of the stairs, offset downward
        Vector3 center = bounds.center;
        Vector3 offsetDirection = -averageNormal.normalized;
        colliderObj.transform.localPosition = center + offsetDirection * (colliderOffset + colliderThickness * 0.5f);
        
        // Rotate to match stair angle
        // Calculate rotation to align with stair surface
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, averageNormal);
        colliderObj.transform.localRotation = rotation;
        
        // Scale to match stair dimensions
        Vector3 size = bounds.size;
        size.y = colliderThickness; // Thin collider
        colliderObj.transform.localScale = Vector3.one;
        
        BoxCollider boxCollider = colliderObj.AddComponent<BoxCollider>();
        boxCollider.size = size;
        boxCollider.center = Vector3.zero;
        
        // Copy layer from stair object
        colliderObj.layer = stairMeshObject.layer;
    }
    
    private Vector3 CalculateAverageNormal(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector3 averageNormal = Vector3.zero;
        int normalCount = 0;
        
        // Calculate average normal from triangles
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = vertices[triangles[i]];
            Vector3 v1 = vertices[triangles[i + 1]];
            Vector3 v2 = vertices[triangles[i + 2]];
            
            Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
            averageNormal += normal;
            normalCount++;
        }
        
        if (normalCount > 0)
        {
            averageNormal /= normalCount;
        }
        else
        {
            averageNormal = Vector3.up; // Fallback
        }
        
        return averageNormal.normalized;
    }
    
    private void ClearExistingColliders()
    {
        if (colliderParent == null) return;
        
        // Destroy all existing collider children
        for (int i = colliderParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(colliderParent.GetChild(i).gameObject);
        }
    }
    
    /// <summary>
    /// Alternative: Generate colliders by raycasting downward from stair surface
    /// </summary>
    [ContextMenu("Generate Colliders by Raycast")]
    public void GenerateCollidersByRaycast()
    {
        if (stairMeshObject == null)
        {
            Debug.LogError("StairColliderGenerator: No stair mesh object assigned!");
            return;
        }
        
        MeshFilter meshFilter = stairMeshObject.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("StairColliderGenerator: No mesh found!");
            return;
        }
        
        // Create parent if needed
        if (colliderParent == null)
        {
            GameObject parentObj = new GameObject("StairColliders");
            parentObj.transform.SetParent(stairMeshObject.transform);
            parentObj.transform.localPosition = Vector3.zero;
            parentObj.transform.localRotation = Quaternion.identity;
            colliderParent = parentObj.transform;
        }
        
        ClearExistingColliders();
        
        // Sample points on the stair surface and create colliders below them
        Mesh mesh = meshFilter.sharedMesh;
        Bounds bounds = mesh.bounds;
        
        // Create a grid of sample points
        int samplesX = 10;
        int samplesZ = 10;
        
        for (int x = 0; x < samplesX; x++)
        {
            for (int z = 0; z < samplesZ; z++)
            {
                float u = (float)x / (samplesX - 1);
                float v = (float)z / (samplesZ - 1);
                
                Vector3 localPos = new Vector3(
                    Mathf.Lerp(bounds.min.x, bounds.max.x, u),
                    bounds.max.y,
                    Mathf.Lerp(bounds.min.z, bounds.max.z, v)
                );
                
                Vector3 worldPos = meshFilter.transform.TransformPoint(localPos);
                
                // Raycast downward to find stair surface
                RaycastHit hit;
                if (Physics.Raycast(worldPos + Vector3.up * 0.5f, Vector3.down, out hit, 2f, stairLayerMask))
                {
                    CreateColliderAtPoint(hit.point, hit.normal);
                }
            }
        }
        
        Debug.Log("StairColliderGenerator: Generated colliders using raycast method");
    }
    
    private void CreateColliderAtPoint(Vector3 position, Vector3 normal)
    {
        GameObject colliderObj = new GameObject("StairCollider_Point");
        colliderObj.transform.SetParent(colliderParent);
        colliderObj.transform.position = position - normal * (colliderOffset + colliderThickness * 0.5f);
        colliderObj.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
        
        BoxCollider boxCollider = colliderObj.AddComponent<BoxCollider>();
        boxCollider.size = new Vector3(0.5f, colliderThickness, 0.5f);
        boxCollider.center = Vector3.zero;
        
        colliderObj.layer = stairMeshObject.layer;
    }
}

