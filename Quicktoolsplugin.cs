using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Studio;
using UnityEngine;
using UnityEngine.UI;

namespace StudioCustomButton
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class QuickToolsPlugin : BaseUnityPlugin
    {
        public const string GUID = "emkyfreak.quicktools";
        public const string PluginName = "QuickTools";
        public const string Version = "1.0.0";

        private static new ManualLogSource Logger;

        // UI Elements
        private GameObject _mainButton;
        private GameObject _customPanel;
        private Canvas _studioCanvas;
        private bool _isInitialized = false;

        // Sub-menu containers
        private GameObject _maleToolsMenu;
        private GameObject _femaleToolsMenu;
        private GameObject _itemToolsMenu;
        private GameObject _mainMenu;

        // Male Tools State
        private List<ChaControl> _maleCharacters = new List<ChaControl>();
        private int _currentMaleIndex = -1;
        private ChaControl _selectedMale = null;
        private Text _maleNameDisplay = null; // Display current male name
        private Toggle _lockViewToggle;
        private Toggle _hideHeadToggle;
        private bool _isViewLocked = false;
        private bool _isHeadHidden = true;
        private List<Renderer> _hiddenRenderers = new List<Renderer>();
        private Transform _vrCameraOrigin;
        private float _cameraTilt = 0f;
        private Vector3 _savedCamTargetPos;
        private Vector3 _savedCamAngle;
        private Vector3 _savedCamDir;
        private bool _isCamSaved = false;

        // Female Tools State
        private List<ChaControl> _femaleCharacters = new List<ChaControl>();
        private int _currentFemaleIndex = -1;
        private ChaControl _selectedFemale = null;
        private Text _femaleNameDisplay = null; // Display current female name
        private Text _sightTargetDisplay = null; // Display sight target mode
        private Text _outfitDisplay = null; // Display current outfit
        private Text _clothingDisplay = null; // Display clothing state
        private int _sightTargetMode = 0; // 0=Default, 1=Look at Me, 2=Look Away
        private int _currentClothingState = 0; // 0=Clothed, 1=Partial, 2=Nude
        private int _currentEyebrowPattern = 0;
        private int _currentEyePattern = 0;
        private int _currentMouthPattern = 0;
        private Text _eyebrowDisplay = null; // Display eyebrow pattern index
        private Text _eyeDisplay = null; // Display eye pattern index
        private Text _mouthDisplay = null; // Display mouth pattern index
        private int _animGroup = 0;
        private int _animCategory = 0;
        private int _animID = 0;
        private Text _animGroupDisplay = null;
        private Text _animCategoryDisplay = null;
        private Text _animIDDisplay = null;
        // Coordinate Card selection state
        private List<string> _clothingCardPaths = new List<string>();
        private int _currentCardIndex = -1;
        private Text _clothingCardDisplay = null;
        private bool _lockHairAccOnClothingChange = true;
        private List<string> _coordFolders = new List<string>();
        private int _currentFolderIndex = 0;
        private Text _coordFolderDisplay = null;

        // Config
        private ConfigEntry<bool> _extendedOutfitMode;
        private ConfigEntry<string> _customSpawnGroup;
        private ConfigEntry<int> _customSpawnCategory;
        private ConfigEntry<int> _customSpawnItem;
        private ConfigEntry<string> _customSpawnItems; // Format: name|group|cat|item;name2|group2|cat2|item2

        void Awake()
        {
            Logger = base.Logger;

            // Config for custom spawn
            _customSpawnGroup = Config.Bind("Item Tools", "Custom Spawn Group", "0",
                "Group number for custom spawn button");
            _customSpawnCategory = Config.Bind("Item Tools", "Custom Spawn Category", 0,
                "Category number for custom spawn button");
            _customSpawnItem = Config.Bind("Item Tools", "Custom Spawn Item", 0,
                "Item number for custom spawn button");

            // Config for extended outfit mode
            _extendedOutfitMode = Config.Bind("Female Tools", "Extended Outfit Mode", false,
                "Enable cycling through 0-120 outfits instead of 0-6. Adds +1/-1 and +10/-10 buttons.");

            // Config for custom spawn items (dynamic list)
            _customSpawnItems = Config.Bind("Item Tools", "Custom Spawn Items List", "",
                "Format: name|group|cat|item;name2|group2|cat2|item2 (Example: Sphere|0|0|0;Light|3|100|50)");

            Logger.LogInfo(PluginName + " v" + Version + " loaded");
        }

        void Start()
        {
            StartCoroutine(WaitAndInitialize());
        }

        private IEnumerator WaitAndInitialize()
        {
            Logger.LogInfo("Waiting for Studio to load...");

            while (Studio.Studio.Instance == null)
            {
                yield return new WaitForSeconds(0.5f);
            }

            Logger.LogInfo("Studio detected, waiting for UI initialization...");
            yield return new WaitForSeconds(0.5f); // Reduced from 2f to 0.5f

            _studioCanvas = FindStudioCanvas();

            if (_studioCanvas == null)
            {
                Logger.LogError("Failed to find Studio Canvas!");
                yield break;
            }

            CreateMainButton();
            CreateCustomPanel();

            _isInitialized = true;
            Logger.LogInfo("UI initialization complete!");
        }

        private Canvas FindStudioCanvas()
        {
            string[] canvasNames = new string[]
            {
                "Canvas",
                "Canvas Main Menu",
                "StudioScene/Canvas Main Menu",
                "Canvas Object Hierarchy"
            };

            foreach (string canvasName in canvasNames)
            {
                GameObject canvasObj = GameObject.Find(canvasName);
                if (canvasObj != null)
                {
                    Canvas canvas = canvasObj.GetComponent<Canvas>();
                    if (canvas != null)
                    {
                        Logger.LogInfo("Found Studio Canvas: " + canvasName);
                        return canvas;
                    }
                }
            }

            Canvas[] allCanvases = FindObjectsOfType<Canvas>();
            foreach (Canvas canvas in allCanvases)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay &&
                    canvas.gameObject.activeInHierarchy)
                {
                    Logger.LogInfo("Using Canvas: " + canvas.name);
                    return canvas;
                }
            }

            return null;
        }

        private void CreateMainButton()
        {
            _mainButton = new GameObject("QuickTools_MainButton");
            _mainButton.transform.SetParent(_studioCanvas.transform, false);

            Image bgImage = _mainButton.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.4f, 0.8f, 0.9f);

            Button button = _mainButton.AddComponent<Button>();
            button.onClick.AddListener(OnMainButtonClicked);

            _mainButton.AddComponent<DragPanel>();

            RectTransform rectTransform = _mainButton.GetComponent<RectTransform>();
            // MOVED to avoid transformation window
            rectTransform.anchorMin = new Vector2(1, 0);
            rectTransform.anchorMax = new Vector2(1, 0);
            rectTransform.pivot = new Vector2(1, 0);
            rectTransform.anchoredPosition = new Vector2(-140, 10);  // 140px from right, 10px from bottom
            rectTransform.sizeDelta = new Vector2(120, 40);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(_mainButton.transform, false);

            Text textComponent = textObj.AddComponent<Text>();
            textComponent.text = "QuickTools";
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.fontSize = 16;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            SetupStretchedRectTransform(textRect);

            Logger.LogInfo("Main button created (draggable, top-right)");
        }

        private void CreateCustomPanel()
        {
            _customPanel = new GameObject("QuickTools_Panel");
            _customPanel.transform.SetParent(_studioCanvas.transform, false);
            _customPanel.SetActive(false);

            Image panelBg = _customPanel.AddComponent<Image>();
            panelBg.color = new Color(0.15f, 0.15f, 0.15f, 1f); // Slightly lighter and fully opaque for VR visibility

            RectTransform panelRect = _customPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(420, 600);

            AddDraggableComponent(_customPanel);

            CreateTitleBar();
            CreateCloseButton();
            CreateMainMenuButtons();
            CreateSubMenus();

            Logger.LogInfo("Custom panel created");
        }

        private void AddDraggableComponent(GameObject panel)
        {
            DragPanel dragScript = panel.AddComponent<DragPanel>();
        }

        // Simple drag component for the panel
        private class DragPanel : UnityEngine.EventSystems.UIBehaviour, UnityEngine.EventSystems.IDragHandler
        {
            private RectTransform _rectTransform;
            private Canvas _canvas;

            protected override void Awake()
            {
                base.Awake();
                _rectTransform = GetComponent<RectTransform>();
                _canvas = GetComponentInParent<Canvas>();
            }

            public void OnDrag(UnityEngine.EventSystems.PointerEventData eventData)
            {
                if (_canvas == null) return;
                // Using eventData.delta divided by the canvas scale factor works perfectly 
                // regardless of where the anchor/pivot is, preventing it from jumping off-screen!
                _rectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
            }
        }

        private void CreateTitleBar()
        {
            GameObject titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(_customPanel.transform, false);

            Image titleBg = titleBar.AddComponent<Image>();
            titleBg.color = new Color(0.2f, 0.4f, 0.8f, 1f);

            RectTransform titleRect = titleBar.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 40);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(titleBar.transform, false);

            Text titleText = textObj.AddComponent<Text>();
            titleText.text = "QuickTools";
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = 18;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            SetupStretchedRectTransform(textRect);
        }

        private void CreateCloseButton()
        {
            GameObject closeBtn = new GameObject("CloseButton");
            closeBtn.transform.SetParent(_customPanel.transform, false);

            Image closeBg = closeBtn.AddComponent<Image>();
            closeBg.color = new Color(0.8f, 0.2f, 0.2f, 1f);

            Button closeButton = closeBtn.AddComponent<Button>();
            closeButton.onClick.AddListener(OnClosePanel);

            RectTransform closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.anchoredPosition = new Vector2(-5, -5);
            closeRect.sizeDelta = new Vector2(35, 35);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(closeBtn.transform, false);

            Text xText = textObj.AddComponent<Text>();
            xText.text = "X";
            xText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            xText.fontSize = 20;
            xText.fontStyle = FontStyle.Bold;
            xText.alignment = TextAnchor.MiddleCenter;
            xText.color = Color.white;

            RectTransform xRect = textObj.GetComponent<RectTransform>();
            SetupStretchedRectTransform(xRect);
        }

        private void CreateMainMenuButtons()
        {
            _mainMenu = new GameObject("MainMenu");
            _mainMenu.transform.SetParent(_customPanel.transform, false);

            // Add RectTransform component (UI objects need this)
            RectTransform mainRect = _mainMenu.AddComponent<RectTransform>();
            mainRect.anchorMin = new Vector2(0, 0);
            mainRect.anchorMax = new Vector2(1, 1);
            mainRect.offsetMin = new Vector2(10, 10);
            mainRect.offsetMax = new Vector2(-10, -50);

            CreateMenuButton("Male Tools", 0, () => ShowSubMenu("male"));
            CreateMenuButton("Female Tools", 1, () => ShowSubMenu("female"));
            CreateMenuButton("Item Tools", 2, () => ShowSubMenu("item"));

            Logger.LogInfo("Main menu created with 3 buttons");
        }

        private void CreateMenuButton(string label, int index, UnityEngine.Events.UnityAction action)
        {
            GameObject btnObj = new GameObject("MenuButton_" + label);
            btnObj.transform.SetParent(_mainMenu.transform, false);

            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            Button button = btnObj.AddComponent<Button>();
            button.onClick.AddListener(action);

            float yOffset = -60 - (index * 70);

            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.1f, 1);
            btnRect.anchorMax = new Vector2(0.9f, 1);
            btnRect.pivot = new Vector2(0.5f, 1);
            btnRect.anchoredPosition = new Vector2(0, yOffset);
            btnRect.sizeDelta = new Vector2(0, 55);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            Text btnText = textObj.AddComponent<Text>();
            btnText.text = label;
            btnText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            btnText.fontSize = 16;
            btnText.fontStyle = FontStyle.Bold;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            SetupStretchedRectTransform(textRect);
        }

        private void CreateSubMenus()
        {
            CreateMaleToolsMenu();
            CreateFemaleToolsMenu();
            CreateItemToolsMenu();

            // Hide all sub-menus initially
            _maleToolsMenu.SetActive(false);
            _femaleToolsMenu.SetActive(false);
            _itemToolsMenu.SetActive(false);

            // Show main menu by default
            _mainMenu.SetActive(true);

            Logger.LogInfo("Sub-menus created and initialized");
        }

        #region Male Tools Menu

        private void CreateMaleToolsMenu()
        {
            _maleToolsMenu = new GameObject("MaleToolsMenu");
            _maleToolsMenu.transform.SetParent(_customPanel.transform, false);

            RectTransform menuRect = _maleToolsMenu.AddComponent<RectTransform>();
            menuRect.anchorMin = new Vector2(0, 0);
            menuRect.anchorMax = new Vector2(1, 1);
            menuRect.offsetMin = new Vector2(10, 10);
            menuRect.offsetMax = new Vector2(-10, -50);

            // Add ScrollRect for scrollable content
            GameObject contentObj = new GameObject("ScrollContent");
            contentObj.transform.SetParent(_maleToolsMenu.transform, false);

            float yPos = -20;

            // Back button
            yPos = CreateBackButton(_maleToolsMenu, yPos);
            yPos -= 10;

            // Male Cycler
            yPos = CreateLabel(_maleToolsMenu, "Male Character:", yPos);
            yPos -= 25; // Move down for display
            _maleNameDisplay = CreateDisplayField(_maleToolsMenu, "No male selected", yPos);
            yPos -= 30; // Move down for buttons
            yPos = CreateCyclerButtons(_maleToolsMenu, "Male", yPos, OnMalePrevious, OnMaleNext);
            yPos -= 15;

            // POV Buttons
            yPos = CreateActionButton(_maleToolsMenu, "Set POV", yPos, OnSetPOV);
            yPos -= 5;
            yPos = CreateActionButton(_maleToolsMenu, "Stop POV (Reset Camera)", yPos, OnStopPOV);
            yPos -= 10;

            // Lock View Checkbox
            yPos = CreateToggle(_maleToolsMenu, "Lock View", yPos, false, OnLockViewChanged, out _lockViewToggle);
            yPos -= 10;

            // Hide Head Checkbox
            yPos = CreateToggle(_maleToolsMenu, "Hide Head & Accessories", yPos, true, OnHideHeadChanged, out _hideHeadToggle);
            yPos -= 15;

            // Tilt Buttons
            yPos = CreateLabel(_maleToolsMenu, "Camera Tilt:", yPos);
            yPos = CreateActionButton(_maleToolsMenu, "Tilt Up", yPos, OnTiltUp);
            yPos -= 5;
            yPos = CreateActionButton(_maleToolsMenu, "Reset Tilt", yPos, OnTiltReset);
            yPos -= 5;
            yPos = CreateActionButton(_maleToolsMenu, "Tilt Down", yPos, OnTiltDown);
        }

        private void OnMalePrevious()
        {
            ScanMaleCharacters();
            if (_maleCharacters.Count == 0)
            {
                if (_maleNameDisplay != null)
                    _maleNameDisplay.text = "No males in scene";
                return;
            }

            _currentMaleIndex--;
            if (_currentMaleIndex < 0) _currentMaleIndex = _maleCharacters.Count - 1;

            _selectedMale = _maleCharacters[_currentMaleIndex];
            string displayName = string.Format("{0} ({1}/{2})",
                _selectedMale.fileParam.fullname,
                _currentMaleIndex + 1,
                _maleCharacters.Count);

            if (_maleNameDisplay != null)
                _maleNameDisplay.text = displayName;

            Logger.LogInfo(string.Format("Selected male: {0} ({1}/{2})", _selectedMale.fileParam.fullname, _currentMaleIndex + 1, _maleCharacters.Count));
        }

        private void OnMaleNext()
        {
            ScanMaleCharacters();
            if (_maleCharacters.Count == 0)
            {
                if (_maleNameDisplay != null)
                    _maleNameDisplay.text = "No males in scene";
                return;
            }

            _currentMaleIndex++;
            if (_currentMaleIndex >= _maleCharacters.Count) _currentMaleIndex = 0;

            _selectedMale = _maleCharacters[_currentMaleIndex];
            string displayName = string.Format("{0} ({1}/{2})",
                _selectedMale.fileParam.fullname,
                _currentMaleIndex + 1,
                _maleCharacters.Count);

            if (_maleNameDisplay != null)
                _maleNameDisplay.text = displayName;

            Logger.LogInfo(string.Format("Selected male: {0} ({1}/{2})", _selectedMale.fileParam.fullname, _currentMaleIndex + 1, _maleCharacters.Count));
        }

        private void ScanMaleCharacters()
        {
            _maleCharacters.Clear();

            if (Studio.Studio.Instance == null)
            {
                Logger.LogWarning("Studio.Instance is null, cannot scan characters");
                return;
            }

            OCIChar[] allChars = Studio.Studio.Instance.dicObjectCtrl.Values.OfType<OCIChar>().ToArray();
            foreach (var ociChar in allChars)
            {
                if (ociChar.sex == 0) // Male
                {
                    _maleCharacters.Add(ociChar.charInfo);
                }
            }

            Logger.LogInfo(string.Format("Found {0} male characters", _maleCharacters.Count));
        }

        private void OnSetPOV()
        {
            if (_selectedMale == null) return;

            // Save the studio camera position before snapping into POV
            var camCtrl = Studio.Studio.Instance.cameraCtrl;
            if (camCtrl != null && !_isViewLocked && !_isCamSaved)
            {
                _savedCamTargetPos = camCtrl.targetPos;
                _savedCamAngle = camCtrl.cameraAngle;
                _savedCamDir = GetCameraDistance(camCtrl);
                _isCamSaved = true;
            }

            StartCoroutine(ApplyPOVRoutine());
        }

        private void OnStopPOV()
        {
            // 1. Turn off the locks
            _isViewLocked = false;
            if (_lockViewToggle != null) _lockViewToggle.isOn = false;

            // 2. Restore the head
            if (_selectedMale != null)
            {
                OCIChar targetMale = Studio.Studio.Instance.dicObjectCtrl.Values.OfType<OCIChar>()
                    .FirstOrDefault(x => x.charInfo == _selectedMale);
                if (targetMale != null) ShowHead(targetMale);
            }

            // 3. Reset Tilt
            _cameraTilt = 0f;

            // 4. Restore Studio Camera Position
            var camCtrl = Studio.Studio.Instance.cameraCtrl;
            if (camCtrl != null && _isCamSaved)
            {
                camCtrl.targetPos = _savedCamTargetPos;
                camCtrl.cameraAngle = _savedCamAngle;
                SetCameraDistance(camCtrl, _savedCamDir);
                _isCamSaved = false;
            }
            Logger.LogInfo("POV Stopped and Camera Restored");
        }

        private IEnumerator ApplyPOVRoutine()
        {
            // Find the OCIChar for the selected male once
            OCIChar targetMale = null;
            foreach (var kvp in Studio.Studio.Instance.dicObjectCtrl)
            {
                OCIChar chara = kvp.Value as OCIChar;
                if (chara != null && chara.charInfo == _selectedMale)
                {
                    targetMale = chara;
                    break;
                }
            }

            if (targetMale == null) yield break;

            // Look for VR camera once before tracking
            GameObject vrCamObj = GameObject.Find("VRGIN_Camera (origin)");
            _vrCameraOrigin = vrCamObj != null ? vrCamObj.transform : null;

            // Apply camera math for a few frames to ensure it snaps
            for (int i = 0; i < 5; i++)
            {
                ApplyCameraTracking();
                yield return new WaitForEndOfFrame();
            }

            // OPTIMIZATION: Only run the heavy mesh purge ONCE at the end of setting POV
            if (_hideHeadToggle != null && _hideHeadToggle.isOn)
            {
                HideHead(targetMale);
            }
        }

        private void ApplyCameraTracking()
        {
            OCIChar targetMale = null;
            foreach (var kvp in Studio.Studio.Instance.dicObjectCtrl)
            {
                OCIChar chara = kvp.Value as OCIChar;
                if (chara != null && chara.charInfo == _selectedMale) { targetMale = chara; break; }
            }

            if (targetMale == null) return;

            Transform headBone = targetMale.charInfo.objHeadBone.transform;

            if (_vrCameraOrigin != null)
            {
                // --- VR MODE ---
                Camera vrCam = _vrCameraOrigin.GetComponentInChildren<Camera>();
                Quaternion baseRotation = headBone.rotation * Quaternion.Inverse(vrCam.transform.localRotation);
                Quaternion targetRotation = baseRotation * Quaternion.Euler(_cameraTilt, 0, 0);

                Vector3 eyeOffset = vrCam.transform.position - _vrCameraOrigin.position;
                _vrCameraOrigin.position = headBone.position - eyeOffset;
                _vrCameraOrigin.rotation = targetRotation;
            }
            else
            {
                // --- DESKTOP / STUDIO MODE ---
                var camCtrl = Studio.Studio.Instance.cameraCtrl;
                if (camCtrl != null)
                {
                    // 1. Calculate the true rotation of the head + your custom tilt
                    Quaternion targetRotation = headBone.rotation * Quaternion.Euler(_cameraTilt, 0, 0);

                    // 2. Lock the orbit pivot exactly to the center of the head
                    camCtrl.targetPos = headBone.position;

                    // 3. Apply the rotation
                    camCtrl.cameraAngle = targetRotation.eulerAngles;

                    // 4. Force distance to 0 every single frame!
                    // This forces the camera to stay INSIDE the pivot point (the head) instead of orbiting outside of it.
                    SetCameraDistance(camCtrl, Vector3.zero);
                }
            }
        }

        private void HideHead(OCIChar targetMale)
        {
            if (targetMale == null || targetMale.charInfo == null) return;

            // Clear previous list
            _hiddenRenderers.Clear();

            // 1. Standard Hide (head object)
            GameObject headObj = targetMale.charInfo.objHead;
            if (headObj != null)
            {
                Renderer[] headRenderers = headObj.GetComponentsInChildren<Renderer>();
                foreach (Renderer r in headRenderers)
                {
                    if (r != null && r.enabled)
                    {
                        _hiddenRenderers.Add(r);
                        r.enabled = false;
                    }
                }
            }

            // 2. Universal Spatial Purge
            GameObject bodyObj = targetMale.charInfo.objBodyBone;
            Transform headBone = targetMale.charInfo.objHeadBone.transform;

            if (bodyObj != null && headBone != null)
            {
                Vector3 headCenter = headBone.position;
                Renderer[] allRenderers = bodyObj.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < allRenderers.Length; i++)
                {
                    Renderer r = allRenderers[i];
                    // Only track and hide things that are currently visible
                    if (r == null || !r.enabled) continue;

                    string n = r.name.ToLower();
                    bool isHeadMesh = n.Contains("head") || n.Contains("face") || n.Contains("hair") ||
                                      n.Contains("eye") || n.Contains("mayu") || n.Contains("hitomi") ||
                                      n.Contains("sclera") || n.Contains("tongue") || n.Contains("sita") ||
                                      n.Contains("shita") || n.Contains("tooth") || n.Contains("teeth") ||
                                      n.Contains("cf_o_tooth") || n.Contains("cf_o_tongue") ||
                                      n.Contains("o_tang") || n.Contains("cm_o_tooth") || n.Contains("cm_o_tang");

                    float dist = Vector3.Distance(r.bounds.center, headCenter);
                    bool isTooClose = dist < 0.2f;

                    if (isHeadMesh || isTooClose)
                    {
                        _hiddenRenderers.Add(r);
                        r.enabled = false;
                    }
                }
                Logger.LogInfo("Head and accessories hidden and tracked.");
            }
        }

        private void ShowHead(OCIChar targetMale)
        {
            // Restore ONLY the exact meshes we hid, leaving intentionally hidden meshes alone
            foreach (Renderer r in _hiddenRenderers)
            {
                if (r != null) r.enabled = true;
            }
            _hiddenRenderers.Clear();

            Logger.LogInfo("Head cleanly restored.");
        }

        private void OnLockViewChanged(bool isOn)
        {
            _isViewLocked = isOn;
            Logger.LogInfo("Lock View: " + (isOn ? "ON" : "OFF"));
        }

        private void OnHideHeadChanged(bool isOn)
        {
            _isHeadHidden = isOn;

            if (_selectedMale == null) return;

            // Find the OCIChar for the selected male
            OCIChar targetMale = null;
            foreach (var kvp in Studio.Studio.Instance.dicObjectCtrl)
            {
                OCIChar chara = kvp.Value as OCIChar;
                if (chara != null && chara.charInfo == _selectedMale)
                {
                    targetMale = chara;
                    break;
                }
            }

            if (targetMale == null) return;

            if (isOn)
            {
                HideHead(targetMale);
            }
            else
            {
                ShowHead(targetMale);
            }
        }

        private void UpdateHeadVisibility()
        {
            if (_selectedMale == null) return;

            // Find head object and accessories
            Transform headBone = FindHeadBone(_selectedMale);
            if (headBone == null) return;

            // Hide/show head mesh renderers
            Renderer[] renderers = headBone.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer.name.Contains("head") || renderer.name.Contains("Head") ||
                    renderer.name.Contains("cf_O_head"))
                {
                    renderer.enabled = !_isHeadHidden;
                }
            }

            // Hide/show accessories on head
            foreach (var accessory in _selectedMale.nowCoordinate.accessory.parts)
            {
                if (accessory.type == 0) // Head accessories
                {
                    // Find and toggle accessory GameObjects
                    // Note: This is simplified; actual implementation may need more specific logic
                }
            }

            Logger.LogInfo("Head visibility updated: " + (_isHeadHidden ? "Hidden" : "Visible"));
        }

        private void OnTiltUp()
        {
            _cameraTilt += 10f;
            ApplyCameraTilt();
            Logger.LogInfo(string.Format("Camera tilt: {0}", _cameraTilt));
        }

        private void OnTiltReset()
        {
            _cameraTilt = 0f;
            ApplyCameraTilt();
            Logger.LogInfo("Camera tilt reset");
        }

        private void OnTiltDown()
        {
            _cameraTilt -= 10f;
            ApplyCameraTilt();
            Logger.LogInfo(string.Format("Camera tilt: {0}", _cameraTilt));
        }

        private void ApplyCameraTilt()
        {
            if (_vrCameraOrigin != null)
            {
                // VR mode
                Vector3 euler = _vrCameraOrigin.localEulerAngles;
                euler.x = _cameraTilt;
                _vrCameraOrigin.localEulerAngles = euler;
            }
            else
            {
                // Force an immediate camera update so the button works even when "Lock View" is off
                ApplyCameraTracking();
            }

            Logger.LogInfo(string.Format("Camera tilt updated to: {0}", _cameraTilt));
        }

        private Transform FindHeadBone(ChaControl character)
        {
            // Common head bone names in Koikatsu
            string[] headBoneNames = new string[]
            {
                "cf_J_Head",
                "cf_j_head",
                "Head",
                "head"
            };

            Animator animator = character.animBody;
            if (animator != null)
            {
                foreach (string boneName in headBoneNames)
                {
                    Transform bone = animator.GetBoneTransform(HumanBodyBones.Head);
                    if (bone != null) return bone;
                }
            }

            // Fallback: search in hierarchy
            foreach (string boneName in headBoneNames)
            {
                Transform bone = character.transform.FindLoop(boneName);
                if (bone != null) return bone;
            }

            return null;
        }

        #endregion

        #region Female Tools Menu

        private void CreateFemaleToolsMenu()
        {
            _femaleToolsMenu = new GameObject("FemaleToolsMenu");
            _femaleToolsMenu.transform.SetParent(_customPanel.transform, false);

            RectTransform menuRect = _femaleToolsMenu.AddComponent<RectTransform>();
            menuRect.anchorMin = new Vector2(0, 0);
            menuRect.anchorMax = new Vector2(1, 1);
            menuRect.offsetMin = new Vector2(10, 10);
            menuRect.offsetMax = new Vector2(-10, -50);

            // Add scroll view
            GameObject scrollViewport = new GameObject("Viewport");
            scrollViewport.transform.SetParent(_femaleToolsMenu.transform, false);

            RectTransform viewportRect = scrollViewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.anchoredPosition = Vector2.zero;

            Image viewportMask = scrollViewport.AddComponent<Image>();
            viewportMask.color = new Color(1, 1, 1, 0.01f); // Nearly transparent
            Mask mask = scrollViewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Scrollable content container
            GameObject scrollContent = new GameObject("ScrollContent");
            scrollContent.transform.SetParent(scrollViewport.transform, false);

            RectTransform contentRect = scrollContent.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 1200); // Tall enough for all content

            // Add ScrollRect to menu
            ScrollRect scrollRect = _femaleToolsMenu.AddComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 20f;

            // create all controls as children of scrollContent instead of _femaleToolsMenu
            float yPos = -20;

            // Back button
            yPos = CreateBackButton(scrollContent, yPos);
            yPos -= 10;

            // Female Cycler
            yPos = CreateLabel(scrollContent, "Female Character:", yPos);
            yPos -= 25;
            _femaleNameDisplay = CreateDisplayField(scrollContent, "No female selected", yPos);
            yPos -= 30;
            yPos = CreateCyclerButtons(scrollContent, "Female", yPos, OnFemalePrevious, OnFemaleNext);
            yPos -= 15;

            // Sight Target Cycler
            yPos = CreateLabel(scrollContent, "Sight Target:", yPos);
            yPos -= 25;
            _sightTargetDisplay = CreateDisplayField(scrollContent, "Default", yPos);
            yPos -= 30;
            yPos = CreateCyclerButtons(scrollContent, "Sight", yPos, OnSightPrevious, OnSightNext);
            yPos -= 15;

            // Outfit Cycler
            yPos = CreateLabel(scrollContent, "Outfit:", yPos);
            yPos -= 25;
            _outfitDisplay = CreateDisplayField(scrollContent, "Outfit 0", yPos);
            yPos -= 30;

            if (_extendedOutfitMode.Value)
            {
                // Extended mode: 4 buttons (+10, +1, -1, -10)
                yPos = CreateQuadButtons(scrollContent, "Outfit", yPos,
                    OnOutfitMinus10, OnOutfitPrevious, OnOutfitNext, OnOutfitPlus10,
                    "-10", "-1", "+1", "+10");
            }
            else
            {
                // Normal mode: 2 buttons
                yPos = CreateCyclerButtons(scrollContent, "Outfit", yPos, OnOutfitPrevious, OnOutfitNext);
            }
            yPos -= 10;

            // Clothing State Cycler
            yPos = CreateLabel(scrollContent, "Clothing:", yPos);
            yPos -= 25;
            _clothingDisplay = CreateDisplayField(scrollContent, "Clothed", yPos);
            yPos -= 30;
            yPos = CreateCyclerButtons(scrollContent, "Clothing", yPos, OnClothingPrevious, OnClothingNext);
            yPos -= 15;

            // Coordinate FOLDER Cycler
            yPos = CreateLabel(scrollContent, "Coordinate Folder:", yPos);
            yPos -= 25;
            _coordFolderDisplay = CreateDisplayField(scrollContent, "Loading...", yPos);
            yPos -= 30;
            yPos = CreateCyclerButtons(scrollContent, "CoordFolder", yPos, OnFolderPrevious, OnFolderNext);
            yPos -= 10;

            // Coordinate CARD Cycler
            yPos = CreateLabel(scrollContent, "Coordinate Card:", yPos);
            yPos -= 25;
            _clothingCardDisplay = CreateDisplayField(scrollContent, "No Card Selected", yPos);
            yPos -= 30;
            yPos = CreateCyclerButtons(scrollContent, "CoordinateCard", yPos, OnClothingCardPrevious, OnClothingCardNext);
            yPos -= 10;

            // Lock Hair Accessories checkbox
            Toggle hairLockToggle;
            yPos = CreateToggle(scrollContent, "Lock Hair Accessories", yPos, true, OnLockHairAccChanged, out hairLockToggle);
            yPos -= 15;

            // SEPARATOR
            yPos = CreateSeparator(scrollContent, yPos);

            // Facial Feature Cyclers
            yPos = CreateLabel(scrollContent, "Eyebrows:", yPos);
            yPos -= 25;
            _eyebrowDisplay = CreateDisplayField(scrollContent, "Pattern: 0", yPos);
            yPos -= 30;
            yPos = CreateCyclerButtons(scrollContent, "Eyebrows", yPos, OnEyebrowsPrevious, OnEyebrowsNext);
            yPos -= 10;

            yPos = CreateLabel(scrollContent, "Eyes:", yPos);
            yPos -= 25;
            _eyeDisplay = CreateDisplayField(scrollContent, "Pattern: 0", yPos);
            yPos -= 30;
            yPos = CreateCyclerButtons(scrollContent, "Eyes", yPos, OnEyesPrevious, OnEyesNext);
            yPos -= 10;

            yPos = CreateLabel(scrollContent, "Mouth:", yPos);
            yPos -= 25;
            _mouthDisplay = CreateDisplayField(scrollContent, "Pattern: 0", yPos);
            yPos -= 30;
            yPos = CreateCyclerButtons(scrollContent, "Mouth", yPos, OnMouthPrevious, OnMouthNext);
            yPos -= 15;

            // SEPARATOR 2: Between Facial Features and Animation
            yPos = CreateSeparator(scrollContent, yPos);

            // Animation Controls
            yPos = CreateLabel(scrollContent, "Animation Group (0-20):", yPos);
            yPos -= 25;
            _animGroupDisplay = CreateDisplayField(scrollContent, "Group: 0", yPos);
            yPos -= 30;
            yPos = CreateCyclerButtons(scrollContent, "AnimGroup", yPos, () => CycleAnimValue(ref _animGroup, -1, 0, 20),
                () => CycleAnimValue(ref _animGroup, 1, 0, 20));
            yPos -= 10;

            yPos = CreateLabel(scrollContent, "Category (0-99):", yPos);
            yPos -= 25;
            _animCategoryDisplay = CreateDisplayField(scrollContent, "Category: 0", yPos);
            yPos -= 30;
            yPos = CreateCyclerButtons(scrollContent, "AnimCat", yPos, () => CycleAnimValue(ref _animCategory, -1, 0, 99),
                () => CycleAnimValue(ref _animCategory, 1, 0, 99));
            yPos -= 10;

            yPos = CreateLabel(scrollContent, "Animation ID (0-99):", yPos);
            yPos -= 25;
            _animIDDisplay = CreateDisplayField(scrollContent, "ID: 0", yPos);
            yPos -= 30;
            yPos = CreateCyclerButtons(scrollContent, "AnimID", yPos, () => CycleAnimValue(ref _animID, -1, 0, 99),
                () => CycleAnimValue(ref _animID, 1, 0, 99));
            yPos -= 10;

            yPos = CreateActionButton(scrollContent, "Play Animation", yPos, OnPlayAnimation);

            // Update content height based on actual content
            contentRect.sizeDelta = new Vector2(0, Mathf.Abs(yPos) + 20);
        }

        private void OnFemalePrevious()
        {
            ScanFemaleCharacters();
            if (_femaleCharacters.Count == 0)
            {
                if (_femaleNameDisplay != null)
                    _femaleNameDisplay.text = "No females in scene";
                return;
            }

            _currentFemaleIndex--;
            if (_currentFemaleIndex < 0) _currentFemaleIndex = _femaleCharacters.Count - 1;

            _selectedFemale = _femaleCharacters[_currentFemaleIndex];
            string displayName = string.Format("{0} ({1}/{2})",
                _selectedFemale.fileParam.fullname,
                _currentFemaleIndex + 1,
                _femaleCharacters.Count);

            if (_femaleNameDisplay != null)
                _femaleNameDisplay.text = displayName;

            Logger.LogInfo(string.Format("Selected female: {0} ({1}/{2})", _selectedFemale.fileParam.fullname, _currentFemaleIndex + 1, _femaleCharacters.Count));

            UpdateFemaleDisplays();
        }

        private void OnFemaleNext()
        {
            ScanFemaleCharacters();
            if (_femaleCharacters.Count == 0)
            {
                if (_femaleNameDisplay != null)
                    _femaleNameDisplay.text = "No females in scene";
                return;
            }

            _currentFemaleIndex++;
            if (_currentFemaleIndex >= _femaleCharacters.Count) _currentFemaleIndex = 0;

            _selectedFemale = _femaleCharacters[_currentFemaleIndex];
            string displayName = string.Format("{0} ({1}/{2})",
                _selectedFemale.fileParam.fullname,
                _currentFemaleIndex + 1,
                _femaleCharacters.Count);

            if (_femaleNameDisplay != null)
                _femaleNameDisplay.text = displayName;

            Logger.LogInfo(string.Format("Selected female: {0} ({1}/{2})", _selectedFemale.fileParam.fullname, _currentFemaleIndex + 1, _femaleCharacters.Count));

            UpdateFemaleDisplays();
        }

        private void ScanFemaleCharacters()
        {
            _femaleCharacters.Clear();

            if (Studio.Studio.Instance == null)
            {
                Logger.LogWarning("Studio.Instance is null, cannot scan characters");
                return;
            }

            OCIChar[] allChars = Studio.Studio.Instance.dicObjectCtrl.Values.OfType<OCIChar>().ToArray();
            foreach (var ociChar in allChars)
            {
                if (ociChar.sex == 1) // Female
                {
                    _femaleCharacters.Add(ociChar.charInfo);
                }
            }

            Logger.LogInfo(string.Format("Found {0} female characters", _femaleCharacters.Count));
        }

        private void UpdateFemaleDisplays()
        {
            if (_selectedFemale == null) return;

            _currentEyebrowPattern = _selectedFemale.fileStatus.eyebrowPtn;
            _currentEyePattern = _selectedFemale.fileStatus.eyesPtn;
            _currentMouthPattern = _selectedFemale.fileStatus.mouthPtn;

            if (_outfitDisplay != null)
            {
                int currentOutfit = _selectedFemale.fileStatus.coordinateType;
                _outfitDisplay.text = string.Format("Outfit {0}", currentOutfit);
            }

            byte topState = _selectedFemale.fileStatus.clothesState[0];
            _currentClothingState = topState;
            string[] stateNames = new string[] { "Clothed", "Partial", "Nude" };
            if (_currentClothingState >= 0 && _currentClothingState <= 2)
            {
                if (_clothingDisplay != null)
                    _clothingDisplay.text = stateNames[_currentClothingState];
            }

            if (_eyebrowDisplay != null) _eyebrowDisplay.text = string.Format("Pattern: {0}", _currentEyebrowPattern);
            if (_eyeDisplay != null) _eyeDisplay.text = string.Format("Pattern: {0}", _currentEyePattern);
            if (_mouthDisplay != null) _mouthDisplay.text = string.Format("Pattern: {0}", _currentMouthPattern);
        }

        private void OnSightPrevious()
        {
            _sightTargetMode--;
            if (_sightTargetMode < 0) _sightTargetMode = 1; // Only 0 and 1
            ApplySightTarget();

            // Update display
            string[] modes = new string[] { "Default", "Look at Camera" };
            if (_sightTargetDisplay != null)
                _sightTargetDisplay.text = modes[_sightTargetMode];
        }

        private void OnSightNext()
        {
            _sightTargetMode++;
            if (_sightTargetMode > 1) _sightTargetMode = 0; // Only 0 and 1
            ApplySightTarget();

            // Update display
            string[] modes = new string[] { "Default", "Look at Camera" };
            if (_sightTargetDisplay != null)
                _sightTargetDisplay.text = modes[_sightTargetMode];
        }

        private void ApplySightTarget()
        {
            if (_selectedFemale == null)
            {
                Logger.LogWarning("No female selected for sight target");
                return;
            }

            string[] modes = new string[] { "Default", "Look at Camera" };
            Logger.LogInfo(string.Format("Sight Target: {0}", modes[_sightTargetMode]));

            // Access the eye look controller
            var eyeLookCtrl = _selectedFemale.eyeLookCtrl;
            if (eyeLookCtrl == null)
            {
                Logger.LogWarning("Eye look controller not found");
                return;
            }

            // Use Koikatsu's built-in eye look patterns
            switch (_sightTargetMode)
            {
                case 0: // Default - look forward
                    eyeLookCtrl.ptnNo = 0;
                    eyeLookCtrl.target = null;
                    break;
                case 1: // Look at Camera
                    eyeLookCtrl.ptnNo = 1;
                    Camera cam = Camera.main;
                    if (cam != null)
                        eyeLookCtrl.target = cam.transform;
                    break;
            }
        }

        private void OnOutfitPrevious()
        {
            if (_selectedFemale == null) return;
            int currentOutfit = _selectedFemale.fileStatus.coordinateType;
            currentOutfit--;
            int maxOutfit = _extendedOutfitMode.Value ? 120 : 6;
            if (currentOutfit < 0) currentOutfit = maxOutfit;
            _selectedFemale.ChangeCoordinateType((ChaFileDefine.CoordinateType)currentOutfit, true);
            _selectedFemale.Reload();
            Logger.LogInfo(string.Format("Outfit changed to: {0}", currentOutfit));

            if (_outfitDisplay != null)
                _outfitDisplay.text = string.Format("Outfit {0}", currentOutfit);
        }

        private void OnOutfitNext()
        {
            if (_selectedFemale == null) return;
            int currentOutfit = _selectedFemale.fileStatus.coordinateType;
            currentOutfit++;
            int maxOutfit = _extendedOutfitMode.Value ? 120 : 6;
            if (currentOutfit > maxOutfit) currentOutfit = 0;
            _selectedFemale.ChangeCoordinateType((ChaFileDefine.CoordinateType)currentOutfit, true);
            _selectedFemale.Reload();
            Logger.LogInfo(string.Format("Outfit changed to: {0}", currentOutfit));

            if (_outfitDisplay != null)
                _outfitDisplay.text = string.Format("Outfit {0}", currentOutfit);
        }

        private void OnOutfitMinus10()
        {
            if (_selectedFemale == null) return;
            int currentOutfit = _selectedFemale.fileStatus.coordinateType;
            currentOutfit -= 10;
            int maxOutfit = _extendedOutfitMode.Value ? 120 : 6;
            if (currentOutfit < 0) currentOutfit = maxOutfit;
            _selectedFemale.ChangeCoordinateType((ChaFileDefine.CoordinateType)currentOutfit, true);
            _selectedFemale.Reload();
            Logger.LogInfo(string.Format("Outfit changed to: {0}", currentOutfit));
            if (_outfitDisplay != null)
                _outfitDisplay.text = string.Format("Outfit {0}", currentOutfit);
        }

        private void OnOutfitPlus10()
        {
            if (_selectedFemale == null) return;
            int currentOutfit = _selectedFemale.fileStatus.coordinateType;
            currentOutfit += 10;
            int maxOutfit = _extendedOutfitMode.Value ? 120 : 6;
            if (currentOutfit > maxOutfit) currentOutfit = 0;
            _selectedFemale.ChangeCoordinateType((ChaFileDefine.CoordinateType)currentOutfit, true);
            _selectedFemale.Reload();
            Logger.LogInfo(string.Format("Outfit changed to: {0}", currentOutfit));
            if (_outfitDisplay != null)
                _outfitDisplay.text = string.Format("Outfit {0}", currentOutfit);
        }

        private void OnClothingPrevious()
        {
            if (_selectedFemale == null) return;
            _currentClothingState--;
            if (_currentClothingState < 0) _currentClothingState = 2;
            ApplyClothingState();
        }

        private void OnClothingNext()
        {
            if (_selectedFemale == null) return;
            _currentClothingState++;
            if (_currentClothingState > 2) _currentClothingState = 0;
            ApplyClothingState();
        }

        private void ApplyClothingState()
        {
            if (_selectedFemale == null) return;

            string[] stateNames = new string[] { "Clothed", "Partial", "Nude" };

            // Set all clothing parts based on state
            // 0 = On, 1 = Half, 2 = Off
            byte clothState = 0;
            if (_currentClothingState == 1) clothState = 1; // Partial - some half off
            else if (_currentClothingState == 2) clothState = 2; // Nude - all off

            // Apply to all 8 clothing parts
            for (int i = 0; i < 8; i++)
            {
                _selectedFemale.SetClothesState(i, clothState);
            }

            Logger.LogInfo(string.Format("Clothing state: {0}", stateNames[_currentClothingState]));

            if (_clothingDisplay != null)
                _clothingDisplay.text = stateNames[_currentClothingState];
        }

        private void OnEyebrowsPrevious()
        {
            if (_selectedFemale == null) return;
            _currentEyebrowPattern--;
            if (_currentEyebrowPattern < 0) _currentEyebrowPattern = 7; // Koikatsu has 8 eyebrow patterns (0-7)
            _selectedFemale.ChangeEyebrowPtn(_currentEyebrowPattern);
            Logger.LogInfo(string.Format("Eyebrow pattern: {0}", _currentEyebrowPattern));

            if (_eyebrowDisplay != null)
                _eyebrowDisplay.text = string.Format("Pattern: {0}", _currentEyebrowPattern);
        }

        private void OnEyebrowsNext()
        {
            if (_selectedFemale == null) return;
            _currentEyebrowPattern++;
            if (_currentEyebrowPattern > 7) _currentEyebrowPattern = 0;
            _selectedFemale.ChangeEyebrowPtn(_currentEyebrowPattern);
            Logger.LogInfo(string.Format("Eyebrow pattern: {0}", _currentEyebrowPattern));

            if (_eyebrowDisplay != null)
                _eyebrowDisplay.text = string.Format("Pattern: {0}", _currentEyebrowPattern);
        }

        private void OnEyesPrevious()
        {
            if (_selectedFemale == null) return;
            _currentEyePattern--;
            if (_currentEyePattern < 0) _currentEyePattern = 39; // Koikatsu has 40 eye patterns (0-39)
            _selectedFemale.ChangeEyesPtn(_currentEyePattern);
            Logger.LogInfo(string.Format("Eye pattern: {0}", _currentEyePattern));

            if (_eyeDisplay != null)
                _eyeDisplay.text = string.Format("Pattern: {0}", _currentEyePattern);
        }

        private void OnEyesNext()
        {
            if (_selectedFemale == null) return;
            _currentEyePattern++;
            if (_currentEyePattern > 39) _currentEyePattern = 0;
            _selectedFemale.ChangeEyesPtn(_currentEyePattern);
            Logger.LogInfo(string.Format("Eye pattern: {0}", _currentEyePattern));

            if (_eyeDisplay != null)
                _eyeDisplay.text = string.Format("Pattern: {0}", _currentEyePattern);
        }

        private void OnMouthPrevious()
        {
            if (_selectedFemale == null) return;
            _currentMouthPattern--;
            if (_currentMouthPattern < 0) _currentMouthPattern = 34; // Koikatsu has 35 mouth patterns (0-34)
            _selectedFemale.ChangeMouthPtn(_currentMouthPattern);
            Logger.LogInfo(string.Format("Mouth pattern: {0}", _currentMouthPattern));

            if (_mouthDisplay != null)
                _mouthDisplay.text = string.Format("Pattern: {0}", _currentMouthPattern);
        }

        private void OnMouthNext()
        {
            if (_selectedFemale == null) return;
            _currentMouthPattern++;
            if (_currentMouthPattern > 34) _currentMouthPattern = 0;
            _selectedFemale.ChangeMouthPtn(_currentMouthPattern);
            Logger.LogInfo(string.Format("Mouth pattern: {0}", _currentMouthPattern));

            if (_mouthDisplay != null)
                _mouthDisplay.text = string.Format("Pattern: {0}", _currentMouthPattern);
        }

        private void CycleAnimValue(ref int value, int delta, int min, int max)
        {
            value += delta;
            if (value < min) value = max;
            if (value > max) value = min;

            Logger.LogInfo(string.Format("Animation value: Group={0}, Cat={1}, ID={2}", _animGroup, _animCategory, _animID));

            // Update displays
            if (_animGroupDisplay != null)
                _animGroupDisplay.text = string.Format("Group: {0}", _animGroup);
            if (_animCategoryDisplay != null)
                _animCategoryDisplay.text = string.Format("Category: {0}", _animCategory);
            if (_animIDDisplay != null)
                _animIDDisplay.text = string.Format("ID: {0}", _animID);
        }

        private void OnPlayAnimation()
        {
            if (_selectedFemale == null)
            {
                Logger.LogWarning("No female selected");
                return;
            }

            if (Studio.Studio.Instance == null)
            {
                Logger.LogWarning("Studio instance is null");
                return;
            }

            // Find the OCIChar for this character
            OCIChar ociChar = Studio.Studio.Instance.dicObjectCtrl.Values.OfType<OCIChar>()
                .FirstOrDefault(x => x.charInfo == _selectedFemale);

            if (ociChar != null)
            {
                // Load and play animation
                Logger.LogInfo(string.Format("Playing animation: Group={0}, Cat={1}, ID={2}", _animGroup, _animCategory, _animID));

                // Use Studio's animation loader
                try
                {
                    ociChar.LoadAnime(_animGroup, _animCategory, _animID);
                }
                catch (Exception ex)
                {
                    Logger.LogError(string.Format("Error loading animation: {0}", ex.Message));
                }
            }
            else
            {
                Logger.LogWarning("Could not find OCIChar for selected female");
            }
        }

        private void ScanCoordFolders()
        {
            if (_coordFolders.Count > 0) return;
            try
            {
                // Start from the root /coordinate folder instead of /coordinate/female
                string rootPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../UserData/coordinate"));
                if (System.IO.Directory.Exists(rootPath))
                {
                    // Check if the root folder itself has PNGs
                    if (System.IO.Directory.GetFiles(rootPath, "*.png", System.IO.SearchOption.TopDirectoryOnly).Length > 0)
                    {
                        _coordFolders.Add(rootPath);
                    }

                    // Add all subfolders that ACTUALLY contain PNG files
                    string[] dirs = System.IO.Directory.GetDirectories(rootPath, "*", System.IO.SearchOption.AllDirectories);
                    foreach (string d in dirs)
                    {
                        if (System.IO.Directory.GetFiles(d, "*.png", System.IO.SearchOption.TopDirectoryOnly).Length > 0)
                        {
                            _coordFolders.Add(d);
                        }
                    }

                    _coordFolders.Sort(); // Keep folders alphabetical
                    UpdateFolderDisplay();
                }
            }
            catch (Exception ex) { Logger.LogError("Error scanning folders: " + ex.Message); }
        }

        private void UpdateFolderDisplay()
        {
            if (_coordFolderDisplay == null || _coordFolders.Count == 0) return;

            string folderPath = _coordFolders[_currentFolderIndex];
            string rootPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../UserData/coordinate"));

            string displayName;

            // Format the name so you can see relative paths (e.g. "female/modpack")
            if (folderPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase))
                displayName = "Root (/coordinate)";
            else if (folderPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
                displayName = folderPath.Substring(rootPath.Length).TrimStart('\\', '/').Replace('\\', '/');
            else
                displayName = new System.IO.DirectoryInfo(folderPath).Name;

            // Truncate from the left if it's too long so we see the folder name, not just the prefix
            if (displayName.Length > 25) displayName = "..." + displayName.Substring(displayName.Length - 22);

            _coordFolderDisplay.text = displayName;
        }

        private void OnFolderPrevious()
        {
            ScanCoordFolders();
            if (_coordFolders.Count == 0) return;

            _currentFolderIndex--;
            if (_currentFolderIndex < 0) _currentFolderIndex = _coordFolders.Count - 1;

            _clothingCardPaths.Clear(); // Force rescan of the new folder
            _currentCardIndex = -1;

            UpdateFolderDisplay();
            if (_clothingCardDisplay != null) _clothingCardDisplay.text = "Folder Changed";
        }

        private void OnFolderNext()
        {
            ScanCoordFolders();
            if (_coordFolders.Count == 0) return;

            _currentFolderIndex++;
            if (_currentFolderIndex >= _coordFolders.Count) _currentFolderIndex = 0;

            _clothingCardPaths.Clear(); // Force rescan of the new folder
            _currentCardIndex = -1;

            UpdateFolderDisplay();
            if (_clothingCardDisplay != null) _clothingCardDisplay.text = "Folder Changed";
        }

        private void ScanClothingCards()
        {
            ScanCoordFolders(); // Ensure folders are loaded
            if (_clothingCardPaths.Count > 0 || _coordFolders.Count == 0) return;

            try
            {
                string currentFolder = _coordFolders[_currentFolderIndex];

                // ONLY scan the currently selected folder
                if (System.IO.Directory.Exists(currentFolder))
                {
                    string[] files = System.IO.Directory.GetFiles(currentFolder, "*.png", System.IO.SearchOption.TopDirectoryOnly);
                    _clothingCardPaths = new List<string>(files);
                    _clothingCardPaths.Sort(); // Keep series in alphabetical order

                    Logger.LogInfo(string.Format("Found {0} cards in {1}", _clothingCardPaths.Count, currentFolder));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error scanning clothing cards: " + ex.Message);
            }
        }

        private void OnClothingCardPrevious()
        {
            ScanClothingCards();
            if (_clothingCardPaths.Count == 0)
            {
                if (_clothingCardDisplay != null) _clothingCardDisplay.text = "No cards in folder";
                return;
            }

            _currentCardIndex--;
            if (_currentCardIndex < 0) _currentCardIndex = _clothingCardPaths.Count - 1;

            ApplyClothingCard();
        }

        private void OnClothingCardNext()
        {
            ScanClothingCards();
            if (_clothingCardPaths.Count == 0)
            {
                if (_clothingCardDisplay != null) _clothingCardDisplay.text = "No cards in folder";
                return;
            }

            _currentCardIndex++;
            if (_currentCardIndex >= _clothingCardPaths.Count) _currentCardIndex = 0;

            ApplyClothingCard();
        }

        private void ApplyClothingCard()
        {
            if (_selectedFemale == null || _clothingCardPaths.Count == 0) return;

            // Ensure we have System.IO for this
            string path = _clothingCardPaths[_currentCardIndex];
            string cardName = System.IO.Path.GetFileNameWithoutExtension(path);

            // Get the Studio Object Control for the character
            OCIChar ociChar = Studio.Studio.Instance.dicObjectCtrl.Values.OfType<OCIChar>()
                .FirstOrDefault(x => x.charInfo == _selectedFemale);

            if (ociChar == null) return;

            try
            {
                if (!_lockHairAccOnClothingChange)
                {
                    // ----- UNLOCKED MODE -----
                    // Standard Studio load (this replaces clothes, hair, and accessories 
                    // with whatever is saved on the PNG card)
                    ociChar.LoadClothesFile(path);
                }
                else
                {
                    // ----- LOCKED MODE -----
                    // 1. Create a temporary coordinate object to load the card data into
                    ChaFileCoordinate tempCoord = new ChaFileCoordinate();
                    tempCoord.LoadFile(path);

                    // 2. Access the character's currently worn coordinate
                    ChaFileCoordinate currentCoord = _selectedFemale.nowCoordinate;

                    // 3. Swap the clothes data ONLY
                    // We do NOT touch currentCoord.hair or currentCoord.accessory
                    currentCoord.clothes = tempCoord.clothes;

                    // 4. Force the 3D model to update
                    // Arguments: Reload(bool head, bool hair, bool clothe, bool accessory)
                    // We set head to false because we aren't changing the face shape.
                    _selectedFemale.Reload(false, true, true, true);
                }

                // Update UI Display Text
                if (_clothingCardDisplay != null)
                {
                    string displayCardName = cardName;
                    if (displayCardName.Length > 20) displayCardName = displayCardName.Substring(0, 17) + "...";
                    _clothingCardDisplay.text = displayCardName;
                }

                Logger.LogInfo($"Applied {cardName}. Lock Hair: {_lockHairAccOnClothingChange}");
            }
            catch (Exception ex)
            {
                Logger.LogError("Error applying clothing card: " + ex.Message);
            }
        }

        private void OnLockHairAccChanged(bool isOn)
        {
            _lockHairAccOnClothingChange = isOn;
            Logger.LogInfo("Lock Hair Acc: " + (isOn ? "ON" : "OFF"));
        }

        #endregion

        #region Item Tools Menu

        private void CreateItemToolsMenu()
        {
            _itemToolsMenu = new GameObject("ItemToolsMenu");
            _itemToolsMenu.transform.SetParent(_customPanel.transform, false);

            RectTransform menuRect = _itemToolsMenu.AddComponent<RectTransform>();
            menuRect.anchorMin = new Vector2(0, 0);
            menuRect.anchorMax = new Vector2(1, 1);
            menuRect.offsetMin = new Vector2(10, 10);
            menuRect.offsetMax = new Vector2(-10, -50);

            float yPos = -20;

            // Back button
            yPos = CreateBackButton(_itemToolsMenu, yPos);
            yPos -= 10;

            // Disable Dynamic Bones
            yPos = CreateActionButton(_itemToolsMenu, "Disable New Dynamic Bones", yPos, OnDisableDynamicBones);
            yPos -= 10;

            // Spawn Camera/Monitor
            yPos = CreateActionButton(_itemToolsMenu, "Spawn Camera Kit", yPos, OnSpawnCameraKit);
            yPos -= 10;

            // Custom Spawn #1 (legacy - still use the old config)
            yPos = CreateActionButton(_itemToolsMenu, "Spawn Custom #1", yPos, OnSpawnCustom1);
            yPos -= 10;

            // Dynamic custom spawn items
            List<CustomSpawnItem> customItems = ParseCustomSpawnItems();
            if (customItems.Count > 0)
            {
                yPos = CreateSeparator(_itemToolsMenu, yPos);
                yPos = CreateLabel(_itemToolsMenu, "Custom Items:", yPos);
                yPos -= 5;

                for (int i = 0; i < customItems.Count; i++)
                {
                    CustomSpawnItem item = customItems[i];
                    int group = item.group;
                    int cat = item.category;
                    int itemId = item.item;

                    yPos = CreateActionButton(_itemToolsMenu, "Spawn: " + item.name, yPos,
                        () => SpawnItem(group, cat, itemId));
                    yPos -= 5;
                }
            }

            yPos -= 5;
            yPos = CreateLabel(_itemToolsMenu, "Config in F1 Settings:", yPos);
            yPos -= 15;
            yPos = CreateLabel(_itemToolsMenu, "Custom Spawn Items List", yPos);
            yPos -= 15;
            yPos = CreateLabel(_itemToolsMenu, "Format: name|g|c|i;name2|...", yPos);
        }

        // Helper class for custom spawn items
        [Serializable]
        public class CustomSpawnItem
        {
            public string name;
            public int group;
            public int category;
            public int item;
        }

        private List<CustomSpawnItem> ParseCustomSpawnItems()
        {
            List<CustomSpawnItem> items = new List<CustomSpawnItem>();

            // Simple parser for format: name|group|cat|item;name2|group2|cat2|item2
            // TODO This needs a rework so its simpler to use
            string config = _customSpawnItems.Value;
            if (string.IsNullOrEmpty(config)) return items;

            string[] itemStrings = config.Split(';');
            foreach (string itemString in itemStrings)
            {
                if (string.IsNullOrEmpty(itemString.Trim())) continue;

                string[] parts = itemString.Split('|');
                if (parts.Length == 4)
                {
                    CustomSpawnItem item = new CustomSpawnItem();
                    item.name = parts[0].Trim();
                    if (int.TryParse(parts[1].Trim(), out item.group) &&
                        int.TryParse(parts[2].Trim(), out item.category) &&
                        int.TryParse(parts[3].Trim(), out item.item))
                    {
                        items.Add(item);
                        Logger.LogInfo(string.Format("Parsed custom item: {0} ({1}/{2}/{3})",
                            item.name, item.group, item.category, item.item));
                    }
                }
            }
            return items;
        }

        private void OnDisableDynamicBones()
        {
            if (Studio.Studio.Instance == null)
            {
                Logger.LogWarning("Studio instance is null");
                return;
            }

            // Find all items in the scene
            var allItems = Studio.Studio.Instance.dicObjectCtrl.Values.OfType<OCIItem>();
            int checkedCount = 0;
            int disabledCount = 0;

            foreach (var item in allItems)
            {
                if (item == null || item.treeNodeObject == null) continue;

                checkedCount++;

                // Check if this item has the dynamic bone collider name
                if (item.treeNodeObject.textName.Contains("Dynamic Bone") ||
                    item.treeNodeObject.textName.Contains("J694"))
                {
                    Logger.LogInfo(string.Format("Found dynamic bone item: {0}", item.treeNodeObject.textName));

                    if (item.objectItem == null)
                    {
                        Logger.LogWarning("objectItem is null");
                        continue;
                    }

                    // Try to find PoseController component
                    Component poseController = item.objectItem.GetComponent("PoseController");
                    if (poseController != null)
                    {
                        Logger.LogInfo("Found PoseController component");
                        bool disabled = DisableDynamicBoneField(poseController);
                        if (disabled)
                        {
                            disabledCount++;
                            Logger.LogInfo("Successfully disabled dynamic bone");
                        }
                        else
                        {
                            Logger.LogWarning("Failed to disable dynamic bone field");
                        }
                    }
                    else
                    {
                        Logger.LogWarning("PoseController component not found on this item");
                    }
                }
            }

            Logger.LogInfo(string.Format("Checked {0} items total, disabled dynamic bones on {1} items", checkedCount, disabledCount));
        }

        private bool DisableDynamicBoneField(Component poseController)
        {
            try
            {
                Type poseControllerType = poseController.GetType();
                Logger.LogInfo(string.Format("PoseController type: {0}", poseControllerType.Name));

                // Get ALL fields to see what's available
                FieldInfo[] allFields = poseControllerType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                Logger.LogInfo(string.Format("Found {0} fields in PoseController", allFields.Length));

                // Log all field names for debugging
                foreach (var field in allFields)
                {
                    Logger.LogInfo(string.Format("  Field: {0} (Type: {1})", field.Name, field.FieldType.Name));
                }

                // Get the private editor fields
                FieldInfo collidersEditorField = poseControllerType.GetField("_collidersEditor",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfo dynamicBonesEditorField = poseControllerType.GetField("_dynamicBonesEditor",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                object collidersEditor = collidersEditorField != null ? collidersEditorField.GetValue(poseController) : null;
                object dynamicBonesEditor = dynamicBonesEditorField != null ? dynamicBonesEditorField.GetValue(poseController) : null;

                Logger.LogInfo(string.Format("collidersEditor: {0}, dynamicBonesEditor: {1}",
                    collidersEditor != null ? "Found" : "NULL",
                    dynamicBonesEditor != null ? "Found" : "NULL"));

                // Try to find and list all fields in the editors
                if (collidersEditor != null)
                {
                    Type editorType = collidersEditor.GetType();
                    Logger.LogInfo(string.Format("Colliders editor type: {0}", editorType.Name));

                    FieldInfo[] editorFields = editorType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    Logger.LogInfo(string.Format("Colliders editor has {0} fields:", editorFields.Length));
                    foreach (var field in editorFields)
                    {
                        Logger.LogInfo(string.Format("  Editor field: {0} (Type: {1})", field.Name, field.FieldType.Name));

                        // Try to disable any boolean field that seems related
                        if (field.FieldType == typeof(bool) && (field.Name.Contains("new") || field.Name.Contains("New") || field.Name.Contains("check")))
                        {
                            Logger.LogInfo(string.Format("  Attempting to disable boolean field: {0}", field.Name));
                            field.SetValue(collidersEditor, false); // Set to false to disable
                            Logger.LogInfo(string.Format("  Successfully set {0} to false", field.Name));
                            return true;
                        }
                    }
                }

                if (dynamicBonesEditor != null)
                {
                    Type editorType = dynamicBonesEditor.GetType();
                    Logger.LogInfo(string.Format("DynamicBones editor type: {0}", editorType.Name));

                    FieldInfo[] editorFields = editorType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    Logger.LogInfo(string.Format("DynamicBones editor has {0} fields:", editorFields.Length));
                    foreach (var field in editorFields)
                    {
                        Logger.LogInfo(string.Format("  Editor field: {0} (Type: {1})", field.Name, field.FieldType.Name));

                        // Try to disable any boolean field that seems related
                        if (field.FieldType == typeof(bool) && (field.Name.Contains("new") || field.Name.Contains("New") || field.Name.Contains("check")))
                        {
                            Logger.LogInfo(string.Format("  Attempting to disable boolean field: {0}", field.Name));
                            field.SetValue(dynamicBonesEditor, false); // Set to false to disable
                            Logger.LogInfo(string.Format("  Successfully set {0} to false", field.Name));
                            return true;
                        }
                    }
                }

                Logger.LogWarning("No suitable boolean fields found to disable");
            }
            catch (Exception ex)
            {
                Logger.LogError(string.Format("Error disabling dynamic bone field: {0}", ex.Message));
                Logger.LogError(string.Format("Stack trace: {0}", ex.StackTrace));
            }

            return false;
        }

        private void OnSpawnCameraKit()
        {
            // Spawn camera
            SpawnItemByName("16:9 FOV120 Camera  CH01");

            // Spawn monitor
            SpawnItemByName("16:9 FOV120 Monitor CH01");

            Logger.LogInfo("Camera kit spawned");
        }

        private void OnSpawnCustom1()
        {
            int group = int.Parse(_customSpawnGroup.Value);
            int category = _customSpawnCategory.Value;
            int item = _customSpawnItem.Value;

            SpawnItem(group, category, item);
            Logger.LogInfo(string.Format("Custom item spawned: {0}/{1}/{2}", group, category, item));
        }

        private void SpawnItemByName(string itemName)
        {
            if (Studio.Studio.Instance == null)
            {
                Logger.LogError("Studio instance is null, cannot spawn item");
                return;
            }
            Logger.LogInfo(string.Format("Searching for item: {0}", itemName));
            // Search Studio's item database
            var itemInfo = Studio.Info.Instance.dicItemLoadInfo;
            bool found = false;
            int foundGroup = -1;
            int foundCategory = -1;
            int foundItem = -1;
            foreach (var groupEntry in itemInfo)
            {
                int group = groupEntry.Key;
                foreach (var categoryEntry in groupEntry.Value)
                {
                    int category = categoryEntry.Key;
                    foreach (var itemEntry in categoryEntry.Value)
                    {
                        int itemId = itemEntry.Key;
                        Info.ItemLoadInfo item = itemEntry.Value;
                        if (!string.IsNullOrEmpty(item.name) &&
                            item.name.IndexOf(itemName, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            foundGroup = group;
                            foundCategory = category;
                            foundItem = itemId;
                            Logger.LogInfo(string.Format(
                                "Found item: {0} at Group={1}, Cat={2}, ID={3}",
                                item.name, group, category, itemId));
                            found = true;
                            break;
                        }
                    }
                    if (found) break;
                }
                if (found) break;
            }
            if (found)
            {
                SpawnItem(foundGroup, foundCategory, foundItem);
            }
            else
            {
                Logger.LogWarning(string.Format("Item not found: {0}", itemName));
            }
        }

        private void SpawnItem(int group, int category, int item)
        {
            if (Studio.Studio.Instance == null)
            {
                Logger.LogError("Studio instance is null, cannot spawn item");
                return;
            }

            try
            {
                // Use InvokeMember to call AddItem method
                Type studioType = typeof(Studio.Studio);

                object[] args = new object[] { group, category, item };

                studioType.InvokeMember("AddItem",
                    BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    Studio.Studio.Instance,
                    args);

                Logger.LogInfo(string.Format("Item spawned successfully: {0}/{1}/{2}", group, category, item));
            }
            catch (Exception ex)
            {
                Logger.LogError(string.Format("Error spawning item: {0}", ex.Message));
            }
        }

        #endregion

        #region UI Helper Methods

        private Text CreateDisplayField(GameObject parent, string initialText, float yPos)
        {
            GameObject displayObj = new GameObject("Display_" + initialText);
            displayObj.transform.SetParent(parent.transform, false);

            Text displayText = displayObj.AddComponent<Text>();
            displayText.text = initialText;
            displayText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            displayText.fontSize = 14;
            displayText.fontStyle = FontStyle.Bold;
            displayText.alignment = TextAnchor.MiddleCenter;
            displayText.color = Color.white;

            RectTransform displayRect = displayObj.GetComponent<RectTransform>();
            displayRect.anchorMin = new Vector2(0, 1);
            displayRect.anchorMax = new Vector2(1, 1);
            displayRect.pivot = new Vector2(0.5f, 1);
            displayRect.anchoredPosition = new Vector2(0, yPos); // Position directly at yPos
            displayRect.sizeDelta = new Vector2(-20, 25);

            return displayText;
        }

        private float CreateSeparator(GameObject parent, float yPos)
        {
            GameObject separatorObj = new GameObject("Separator");
            separatorObj.transform.SetParent(parent.transform, false);

            Image separatorImage = separatorObj.AddComponent<Image>();
            separatorImage.color = new Color(0.5f, 0.5f, 0.5f, 1f); // Fully opaque for VR visibility

            RectTransform sepRect = separatorObj.GetComponent<RectTransform>();
            sepRect.anchorMin = new Vector2(0, 1);
            sepRect.anchorMax = new Vector2(1, 1);
            sepRect.pivot = new Vector2(0.5f, 1);
            sepRect.anchoredPosition = new Vector2(0, yPos);
            sepRect.sizeDelta = new Vector2(-40, 2); // Thin line with margins

            return yPos - 10; // Return position after separator + small gap
        }

        private float CreateBackButton(GameObject parent, float yPos)
        {
            GameObject btnObj = new GameObject("BackButton");
            btnObj.transform.SetParent(parent.transform, false);

            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = new Color(0.5f, 0.2f, 0.2f, 1f);

            Button button = btnObj.AddComponent<Button>();
            button.onClick.AddListener(() => ShowSubMenu("main"));

            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0, 1);
            btnRect.anchorMax = new Vector2(1, 1);
            btnRect.pivot = new Vector2(0.5f, 1);
            btnRect.anchoredPosition = new Vector2(0, yPos);
            btnRect.sizeDelta = new Vector2(0, 40);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            Text btnText = textObj.AddComponent<Text>();
            btnText.text = "← Back to Main Menu";
            btnText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            btnText.fontSize = 14;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            SetupStretchedRectTransform(textRect);

            return yPos - 40;
        }

        private float CreateLabel(GameObject parent, string text, float yPos)
        {
            GameObject labelObj = new GameObject("Label_" + text);
            labelObj.transform.SetParent(parent.transform, false);

            Text labelText = labelObj.AddComponent<Text>();
            labelText.text = text;
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 12;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;

            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 1);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.pivot = new Vector2(0, 1);
            labelRect.anchoredPosition = new Vector2(10, yPos);
            labelRect.sizeDelta = new Vector2(-20, 25);

            return yPos - 25;
        }

        private float CreateCyclerButtons(GameObject parent, string id, float yPos,
            UnityEngine.Events.UnityAction prevAction, UnityEngine.Events.UnityAction nextAction)
        {
            // Previous button
            GameObject prevBtn = new GameObject("Prev_" + id);
            prevBtn.transform.SetParent(parent.transform, false);

            Image prevBg = prevBtn.AddComponent<Image>();
            prevBg.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            Button prevButton = prevBtn.AddComponent<Button>();
            prevButton.onClick.AddListener(prevAction);

            RectTransform prevRect = prevBtn.GetComponent<RectTransform>();
            prevRect.anchorMin = new Vector2(0, 1);
            prevRect.anchorMax = new Vector2(0.48f, 1);
            prevRect.pivot = new Vector2(0, 1);
            prevRect.anchoredPosition = new Vector2(10, yPos);
            prevRect.sizeDelta = new Vector2(0, 35);

            GameObject prevText = new GameObject("Text");
            prevText.transform.SetParent(prevBtn.transform, false);
            Text prevTextComp = prevText.AddComponent<Text>();
            prevTextComp.text = "◄ Previous";
            prevTextComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            prevTextComp.fontSize = 12;
            prevTextComp.alignment = TextAnchor.MiddleCenter;
            prevTextComp.color = Color.white;
            SetupStretchedRectTransform(prevText.GetComponent<RectTransform>());

            // Next button
            GameObject nextBtn = new GameObject("Next_" + id);
            nextBtn.transform.SetParent(parent.transform, false);

            Image nextBg = nextBtn.AddComponent<Image>();
            nextBg.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            Button nextButton = nextBtn.AddComponent<Button>();
            nextButton.onClick.AddListener(nextAction);

            RectTransform nextRect = nextBtn.GetComponent<RectTransform>();
            nextRect.anchorMin = new Vector2(0.52f, 1);
            nextRect.anchorMax = new Vector2(1, 1);
            nextRect.pivot = new Vector2(1, 1);
            nextRect.anchoredPosition = new Vector2(-10, yPos);
            nextRect.sizeDelta = new Vector2(0, 35);

            GameObject nextText = new GameObject("Text");
            nextText.transform.SetParent(nextBtn.transform, false);
            Text nextTextComp = nextText.AddComponent<Text>();
            nextTextComp.text = "Next ►";
            nextTextComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            nextTextComp.fontSize = 12;
            nextTextComp.alignment = TextAnchor.MiddleCenter;
            nextTextComp.color = Color.white;
            SetupStretchedRectTransform(nextText.GetComponent<RectTransform>());

            return yPos - 35;
        }

        private float CreateQuadButtons(GameObject parent, string id, float yPos,
    UnityEngine.Events.UnityAction action1, UnityEngine.Events.UnityAction action2,
    UnityEngine.Events.UnityAction action3, UnityEngine.Events.UnityAction action4,
    string label1, string label2, string label3, string label4)
        {
            // Button 1 (-10)
            GameObject btn1 = new GameObject("Btn1_" + id);
            btn1.transform.SetParent(parent.transform, false);
            Image bg1 = btn1.AddComponent<Image>();
            bg1.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            Button button1 = btn1.AddComponent<Button>();
            button1.onClick.AddListener(action1);
            RectTransform rect1 = btn1.GetComponent<RectTransform>();
            rect1.anchorMin = new Vector2(0, 1);
            rect1.anchorMax = new Vector2(0.23f, 1);
            rect1.pivot = new Vector2(0, 1);
            rect1.anchoredPosition = new Vector2(10, yPos);
            rect1.sizeDelta = new Vector2(0, 35);
            CreateButtonText(btn1, label1);

            // Button 2 (-1)
            GameObject btn2 = new GameObject("Btn2_" + id);
            btn2.transform.SetParent(parent.transform, false);
            Image bg2 = btn2.AddComponent<Image>();
            bg2.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            Button button2 = btn2.AddComponent<Button>();
            button2.onClick.AddListener(action2);
            RectTransform rect2 = btn2.GetComponent<RectTransform>();
            rect2.anchorMin = new Vector2(0.26f, 1);
            rect2.anchorMax = new Vector2(0.48f, 1);
            rect2.pivot = new Vector2(0, 1);
            rect2.anchoredPosition = new Vector2(0, yPos);
            rect2.sizeDelta = new Vector2(0, 35);
            CreateButtonText(btn2, label2);

            // Button 3 (+1)
            GameObject btn3 = new GameObject("Btn3_" + id);
            btn3.transform.SetParent(parent.transform, false);
            Image bg3 = btn3.AddComponent<Image>();
            bg3.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            Button button3 = btn3.AddComponent<Button>();
            button3.onClick.AddListener(action3);
            RectTransform rect3 = btn3.GetComponent<RectTransform>();
            rect3.anchorMin = new Vector2(0.52f, 1);
            rect3.anchorMax = new Vector2(0.74f, 1);
            rect3.pivot = new Vector2(0, 1);
            rect3.anchoredPosition = new Vector2(0, yPos);
            rect3.sizeDelta = new Vector2(0, 35);
            CreateButtonText(btn3, label3);

            // Button 4 (+10)
            GameObject btn4 = new GameObject("Btn4_" + id);
            btn4.transform.SetParent(parent.transform, false);
            Image bg4 = btn4.AddComponent<Image>();
            bg4.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            Button button4 = btn4.AddComponent<Button>();
            button4.onClick.AddListener(action4);
            RectTransform rect4 = btn4.GetComponent<RectTransform>();
            rect4.anchorMin = new Vector2(0.77f, 1);
            rect4.anchorMax = new Vector2(1, 1);
            rect4.pivot = new Vector2(1, 1);
            rect4.anchoredPosition = new Vector2(-10, yPos);
            rect4.sizeDelta = new Vector2(0, 35);
            CreateButtonText(btn4, label4);

            return yPos - 35;
        }

        private void CreateButtonText(GameObject button, string text)
        {
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(button.transform, false);
            Text textComp = textObj.AddComponent<Text>();
            textComp.text = text;
            textComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComp.fontSize = 12;
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.color = Color.white;
            SetupStretchedRectTransform(textObj.GetComponent<RectTransform>());
        }

        private float CreateActionButton(GameObject parent, string label, float yPos,
            UnityEngine.Events.UnityAction action)
        {
            GameObject btnObj = new GameObject("ActionBtn_" + label);
            btnObj.transform.SetParent(parent.transform, false);

            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = new Color(0.2f, 0.5f, 0.3f, 1f);

            Button button = btnObj.AddComponent<Button>();
            button.onClick.AddListener(action);

            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0, 1);
            btnRect.anchorMax = new Vector2(1, 1);
            btnRect.pivot = new Vector2(0.5f, 1);
            btnRect.anchoredPosition = new Vector2(0, yPos);
            btnRect.sizeDelta = new Vector2(-20, 40);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            Text btnText = textObj.AddComponent<Text>();
            btnText.text = label;
            btnText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            btnText.fontSize = 13;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            SetupStretchedRectTransform(textRect);

            return yPos - 40;
        }

        private float CreateToggle(GameObject parent, string label, float yPos, bool defaultValue,
            UnityEngine.Events.UnityAction<bool> onValueChanged, out Toggle toggleComponent)
        {
            GameObject toggleObj = new GameObject("Toggle_" + label);
            toggleObj.transform.SetParent(parent.transform, false);

            // Add Toggle component (this automatically adds RectTransform)
            Toggle toggle = toggleObj.AddComponent<Toggle>();
            toggle.isOn = defaultValue;
            toggle.onValueChanged.AddListener(onValueChanged);

            // Get the RectTransform that was auto-added by Toggle
            RectTransform toggleRect = toggleObj.GetComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0, 1);
            toggleRect.anchorMax = new Vector2(1, 1);
            toggleRect.pivot = new Vector2(0, 1);
            toggleRect.anchoredPosition = new Vector2(10, yPos);
            toggleRect.sizeDelta = new Vector2(-20, 30);

            // Background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(toggleObj.transform, false);
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            // Image component auto-adds RectTransform
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.5f);
            bgRect.anchorMax = new Vector2(0, 0.5f);
            bgRect.pivot = new Vector2(0, 0.5f);
            bgRect.anchoredPosition = new Vector2(5, 0);
            bgRect.sizeDelta = new Vector2(20, 20);

            // Checkmark
            GameObject checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(background.transform, false);
            Image checkImage = checkmark.AddComponent<Image>();
            checkImage.color = Color.green;
            // Image component auto-adds RectTransform
            SetupStretchedRectTransform(checkmark.GetComponent<RectTransform>());

            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(toggleObj.transform, false);
            Text labelText = labelObj.AddComponent<Text>();
            labelText.text = label;
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 12;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;

            // Text component auto-adds RectTransform
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.pivot = new Vector2(0, 0.5f);
            labelRect.anchoredPosition = new Vector2(30, 0);
            labelRect.sizeDelta = new Vector2(-30, 0);

            toggleComponent = toggle;
            return yPos - 30;
        }

        private void SetupStretchedRectTransform(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        #endregion

        #region Menu Navigation

        private void ShowSubMenu(string menuName)
        {
            // Defensive null checks - prevent crashes if menus aren't initialized
            if (_mainMenu == null || _maleToolsMenu == null || _femaleToolsMenu == null || _itemToolsMenu == null)
            {
                Logger.LogError("One or more sub-menus are null! UI not properly initialized.");
                Logger.LogError(string.Format("MainMenu: {0}, MaleTools: {1}, FemaleTools: {2}, ItemTools: {3}",
                    _mainMenu != null ? "OK" : "NULL",
                    _maleToolsMenu != null ? "OK" : "NULL",
                    _femaleToolsMenu != null ? "OK" : "NULL",
                    _itemToolsMenu != null ? "OK" : "NULL"));
                return;
            }

            Logger.LogInfo("Switching to menu: " + menuName);

            // Hide all menus first
            _mainMenu.SetActive(false);
            _maleToolsMenu.SetActive(false);
            _femaleToolsMenu.SetActive(false);
            _itemToolsMenu.SetActive(false);

            // Show the requested menu
            switch (menuName)
            {
                case "main":
                    _mainMenu.SetActive(true);
                    Logger.LogInfo("Main menu activated");
                    break;
                case "male":
                    _maleToolsMenu.SetActive(true);
                    Logger.LogInfo("Male tools menu activated");
                    break;
                case "female":
                    _femaleToolsMenu.SetActive(true);
                    Logger.LogInfo("Female tools menu activated");
                    break;
                case "item":
                    _itemToolsMenu.SetActive(true);
                    Logger.LogInfo("Item tools menu activated");
                    break;
                default:
                    Logger.LogWarning("Unknown menu name: " + menuName);
                    _mainMenu.SetActive(true); // Fallback to main menu
                    break;
            }
        }

        #endregion

        #region Event Handlers

        private void OnMainButtonClicked()
        {
            if (_customPanel == null)
            {
                Logger.LogError("Custom panel is null!");
                return;
            }

            bool isActive = _customPanel.activeSelf;
            _customPanel.SetActive(!isActive);

            if (!isActive)
            {
                // Only try to show main menu if panel is being opened
                ShowSubMenu("main");
            }
        }

        private void OnClosePanel()
        {
            if (_customPanel == null)
            {
                Logger.LogError("Custom panel is null!");
                return;
            }

            _customPanel.SetActive(false);
        }

        #endregion

        void LateUpdate()
        {
            // Lock view update - only run if all necessary objects exist
            if (!_isViewLocked || !_isInitialized || _selectedMale == null) return;

            ApplyCameraTracking();
        }

        void OnDestroy()
        {
            if (_mainButton != null) Destroy(_mainButton);
            if (_customPanel != null) Destroy(_customPanel);
        }

        private Vector3 GetCameraDistance(Studio.CameraControl camCtrl)
        {
            try
            {
                Type type = typeof(Studio.CameraControl);
                // Look for the hidden cameraData object
                FieldInfo dataField = type.GetField("cameraData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                PropertyInfo dataProp = type.GetProperty("cameraData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                object cameraData = null;
                if (dataField != null) cameraData = dataField.GetValue(camCtrl);
                else if (dataProp != null) cameraData = dataProp.GetValue(camCtrl, null);

                if (cameraData != null)
                {
                    // Extract the distance Vector3 from it
                    FieldInfo distField = cameraData.GetType().GetField("distance", BindingFlags.Public | BindingFlags.Instance);
                    PropertyInfo distProp = cameraData.GetType().GetProperty("distance", BindingFlags.Public | BindingFlags.Instance);

                    if (distField != null) return (Vector3)distField.GetValue(cameraData);
                    if (distProp != null) return (Vector3)distProp.GetValue(cameraData, null);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Could not get camera distance: " + ex.Message);
            }
            return Vector3.zero;
        }

        private void SetCameraDistance(Studio.CameraControl camCtrl, Vector3 distance)
        {
            try
            {
                Type type = typeof(Studio.CameraControl);
                FieldInfo dataField = type.GetField("cameraData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                PropertyInfo dataProp = type.GetProperty("cameraData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                object cameraData = null;
                if (dataField != null) cameraData = dataField.GetValue(camCtrl);
                else if (dataProp != null) cameraData = dataProp.GetValue(camCtrl, null);

                if (cameraData != null)
                {
                    FieldInfo distField = cameraData.GetType().GetField("distance", BindingFlags.Public | BindingFlags.Instance);
                    PropertyInfo distProp = cameraData.GetType().GetProperty("distance", BindingFlags.Public | BindingFlags.Instance);

                    if (distField != null) distField.SetValue(cameraData, distance);
                    else if (distProp != null) distProp.SetValue(cameraData, distance, null);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Could not set camera distance: " + ex.Message);
            }
        }

    }
}
