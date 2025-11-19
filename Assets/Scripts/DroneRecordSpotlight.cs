using UnityEngine;

[ExecuteAlways]
public class DroneRecordSpotlight : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light recordLight;      // Spot Light component
    [SerializeField] private Renderer coneRenderer;  // Visual cone mesh renderer
    [SerializeField] private Transform coneRoot;     // Optional separate cone transform for scaling

    [Header("Beam Settings")]
    [ColorUsage(true, true)]
    [SerializeField] private Color lightColor = new Color(1f, 0.847f, 0.239f); // #FFD83D
    [SerializeField] private float intensity = 4000f;
    [SerializeField] private float range = 6f;
    [SerializeField] private float spotAngle = 25f;
    [SerializeField] private float beamThickness = 1.2f; // scales cone x/y

    private bool isRecording;
    private Vector3 coneBaseScale = Vector3.one;
    private float coneBaseRange = 1f;
    private bool hasConeBaseScale;

    private void Awake()
    {
        EnsureReferences();
        ApplySettings();
        SetBeamActive(false); // off by default
    }

    private void OnValidate()
    {
        EnsureReferences();
        ApplySettings();
        if (!Application.isPlaying)
        {
            SetBeamActive(isRecording);
        }
    }

    private void ApplySettings()
    {
        if (recordLight != null)
        {
            recordLight.type = LightType.Spot;
            recordLight.color = lightColor;
            recordLight.intensity = intensity;
            recordLight.range = range;
            recordLight.spotAngle = spotAngle;
            recordLight.shadows = LightShadows.None; // good for Quest performance
        }

        UpdateConeScale();
    }

    public void BeginRecording()
    {
        isRecording = true;
        SetBeamActive(true);
    }

    public void EndRecording()
    {
        isRecording = false;
        SetBeamActive(false);
    }

    private void SetBeamActive(bool active)
    {
        if (recordLight != null)
            recordLight.enabled = active;

        if (coneRenderer != null)
        {
            coneRenderer.enabled = active;
        }
        else if (coneRoot != null)
        {
            coneRoot.gameObject.SetActive(active);
        }
    }

    private void EnsureReferences()
    {
        if (recordLight == null)
        {
            recordLight = GetComponentInChildren<Light>();
        }

        if (coneRoot == null && coneRenderer != null)
        {
            coneRoot = coneRenderer.transform;
        }
    }

    private void CaptureConeScale()
    {
        if (hasConeBaseScale || coneRoot == null)
        {
            return;
        }

        if (coneRoot == transform)
        {
            Debug.LogWarning("DroneRecordSpotlight: Cone root references the drone root. Assign a dedicated child transform for the volumetric cone to avoid stretching the entire drone.");
            coneRoot = null;
            return;
        }

        coneBaseScale = coneRoot.localScale;
        coneBaseRange = Mathf.Max(0.0001f, range);
        hasConeBaseScale = true;
    }

    private void UpdateConeScale()
    {
        if (coneRoot == null)
        {
            return;
        }

        CaptureConeScale();
        if (!hasConeBaseScale)
        {
            return;
        }

        float rangeFactor = Mathf.Max(0.0001f, range) / coneBaseRange;
        coneRoot.localScale = new Vector3(
            coneBaseScale.x * beamThickness,
            coneBaseScale.y * beamThickness,
            coneBaseScale.z * rangeFactor);
    }
}
