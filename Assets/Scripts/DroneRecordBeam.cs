using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DroneRecordBeam : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform beamOrigin;    // tip of drone nose
    [SerializeField] private Transform droneForward;  // usually drone body

    [Header("Beam Settings")]
    [SerializeField] private bool beamEnabledByDefault = false;
    [SerializeField] private float maxBeamLength = 5f;
    [SerializeField] private float beamWidth = 0.04f;
    [SerializeField] private float fadeOutFraction = 0.2f;
    [SerializeField] private Gradient beamColors;
    [SerializeField] private float spotlightIntensity = 4000f;
    [SerializeField] private float spotAngle = 8f;

    [Header("Collision (optional)")]
    [SerializeField] private bool useCollision = true;
    [SerializeField] private LayerMask collisionLayers = ~0;

    private LineRenderer lineRenderer;
    private Light spotLight;
    private bool isRecording;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;
        lineRenderer.widthMultiplier = beamWidth;
        lineRenderer.colorGradient = beamColors;

        spotLight = beamOrigin.gameObject.GetComponent<Light>();
        if (spotLight == null)
        {
            spotLight = beamOrigin.gameObject.AddComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.renderMode = LightRenderMode.ForcePixel;
        }

        spotLight.intensity = spotlightIntensity;
        spotLight.spotAngle = spotAngle;
        spotLight.range = maxBeamLength;

        SetBeamActive(beamEnabledByDefault);
    }

    private void LateUpdate()
    {
        if (!isRecording || beamOrigin == null || droneForward == null)
        {
            return;
        }

        var direction = droneForward.forward;
        Vector3 startPos = beamOrigin.position;
        float length = maxBeamLength;

        if (useCollision && Physics.Raycast(startPos, direction, out var hit, maxBeamLength, collisionLayers))
        {
            length = hit.distance;
        }

        Vector3 endPos = startPos + direction * length;

        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);

        // optional fade
        if (fadeOutFraction > 0f)
        {
            float head = Mathf.Clamp01(1f - fadeOutFraction);
            var curve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(head, 1f),
                new Keyframe(1f, 0f));
            lineRenderer.widthCurve = curve;
        }

        spotLight.transform.rotation = Quaternion.LookRotation(direction);
        spotLight.range = length;
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
        lineRenderer.enabled = active;
        if (spotLight != null)
        {
            spotLight.enabled = active;
        }
    }
}
