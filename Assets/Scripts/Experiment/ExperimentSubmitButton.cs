using UnityEngine;
using UnityEngine.UI;

namespace Experiment
{
    /// <summary>
    /// Component for the Submit button that triggers trial data saving.
    /// Attach this to your wrist menu's Submit button GameObject.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ExperimentSubmitButton : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _confirmationPopup;
        [SerializeField] private float _popupDuration = 3f;

        private Button _button;
        private Canvas _popupCanvas;
        private float _popupTimer = 0f;
        private bool _popupActive = false;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null)
            {
                _button.onClick.AddListener(OnSubmitClicked);
            }

            // Create confirmation popup if not assigned
            if (_confirmationPopup == null)
            {
                CreateConfirmationPopup();
            }
            else
            {
                _popupCanvas = _confirmationPopup.GetComponentInParent<Canvas>();
            }
        }

        private void Update()
        {
            // Handle popup fade-out
            if (_popupActive)
            {
                _popupTimer -= Time.deltaTime;
                if (_popupTimer <= 0f)
                {
                    _popupActive = false;
                    if (_confirmationPopup != null)
                    {
                        _confirmationPopup.SetActive(false);
                    }
                }
            }
        }

        private void OnSubmitClicked()
        {
            var experimentManager = ExperimentDataManager.Instance;
            if (experimentManager == null)
            {
                Debug.LogError("[ExperimentSubmitButton] ExperimentDataManager not found in scene!");
                return;
            }

            // Submit trial data
            experimentManager.SubmitTrial();

            // Show confirmation popup
            ShowConfirmation();

            Debug.Log("[ExperimentSubmitButton] Trial submitted successfully!");
        }

        private void ShowConfirmation()
        {
            if (_confirmationPopup != null)
            {
                _confirmationPopup.SetActive(true);
                _popupActive = true;
                _popupTimer = _popupDuration;
            }
        }

        /// <summary>
        /// Creates a simple confirmation popup if none is assigned.
        /// </summary>
        private void CreateConfirmationPopup()
        {
            // Find or create a world-space canvas for the popup
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("[ExperimentSubmitButton] No main camera found. Popup will not be created.");
                return;
            }

            // Create canvas
            GameObject canvasObj = new GameObject("TrialCompletePopup_Canvas");
            _popupCanvas = canvasObj.AddComponent<Canvas>();
            _popupCanvas.renderMode = RenderMode.WorldSpace;
            _popupCanvas.worldCamera = mainCamera;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10;

            // Position canvas in front of camera
            canvasObj.transform.position = mainCamera.transform.position + mainCamera.transform.forward * 2f;
            canvasObj.transform.rotation = Quaternion.LookRotation(canvasObj.transform.position - mainCamera.transform.position);
            canvasObj.transform.localScale = Vector3.one * 0.001f; // Scale down for world space

            // Create background panel
            GameObject panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(600, 200);

            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.8f);

            // Create text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(panelObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(560, 160);

            Text text = textObj.AddComponent<Text>();
            text.text = "Trial Complete!\nData Saved Successfully";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 36;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;

            _confirmationPopup = canvasObj;
            _confirmationPopup.SetActive(false);

            Debug.Log("[ExperimentSubmitButton] Created confirmation popup.");
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnSubmitClicked);
            }
        }
    }
}

