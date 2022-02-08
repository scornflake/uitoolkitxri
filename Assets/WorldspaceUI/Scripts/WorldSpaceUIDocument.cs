using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

/*
 * This thing relies on a PhysicsRaycaster to generate a raycast hit against the panel, so we can fake up a 'screen' equivalent hit to feed to the rest of UIToolkit.
 * If we split the raycasting out, then we'd be able to drive this using a current XR raycast hit I think.
 *
 * Basically you want something that'll provide you a raycasthit.
 * Then; once you have world coords, you can proceed as normal.
 */

namespace WorldspaceUI.Scripts
{
    public class WorldSpaceUIDocument : MonoBehaviour, IPointerMoveHandler, IPointerUpHandler, IPointerDownHandler,
        ISubmitHandler, ICancelHandler, IMoveHandler, IScrollHandler, ISelectHandler, IDeselectHandler, IDragHandler
    {
        [Tooltip("Width of the panel in pixels. The RenderTexture used to render the panel will have this width.")] [SerializeField]
        protected int _panelWidth = 1280;

        [Tooltip("Height of the panel in pixels. The RenderTexture used to render the panel will have this height.")] [SerializeField]
        protected int _panelHeight = 720;

        [Tooltip("Scale of the panel. It is like the zoom in a browser.")] [SerializeField]
        protected float _panelScale = 1.0f;

        [Tooltip("Pixels per world units, it will the termine the real panel size in the world based on panel pixel width and height.")] [SerializeField]
        protected float _pixelsPerUnit = 1280.0f;

        [Tooltip("Visual tree element object of this panel.")] [SerializeField]
        protected VisualTreeAsset _visualTreeAsset;

        [Tooltip("PanelSettings that will be used to create a new instance for this panel.")] [SerializeField]
        protected PanelSettings _panelSettingsPrefab;

        [Tooltip("RenderTexture that will be used to create a new instance for this panel.")] [SerializeField]
        protected RenderTexture _renderTexturePrefab;

        [Tooltip(
            "Some input modules (like the XRUIInputModule from the XR Interaction toolkit package) doesn't send PointerMove events. If you are using such an input module, just set this to true so at least you can properly drag things around.")]
        public bool UseDragEventFix = false;

        public GameObject RaycastSource;

        public Vector2 PanelSize
        {
            get => new Vector2(_panelWidth, _panelHeight);
            set
            {
                _panelWidth = Mathf.RoundToInt(value.x);
                _panelHeight = Mathf.RoundToInt(value.y);
                RefreshPanelSize();
            }
        }

        public float PanelScale
        {
            get => _panelScale;
            set
            {
                _panelScale = value;

                if (_panelSettings != null)
                    _panelSettings.scale = value;
            }
        }

        public VisualTreeAsset VisualTreeAsset
        {
            get => _visualTreeAsset;
            set
            {
                _visualTreeAsset = value;

                if (_uiDocument != null)
                    _uiDocument.visualTreeAsset = value;
            }
        }

        public int PanelWidth
        {
            get => _panelWidth;
            set
            {
                _panelWidth = value;
                RefreshPanelSize();
            }
        }

        public int PanelHeight
        {
            get => _panelHeight;
            set
            {
                _panelHeight = value;
                RefreshPanelSize();
            }
        }

        public float PixelsPerUnit
        {
            get => _pixelsPerUnit;
            set
            {
                _pixelsPerUnit = value;
                RefreshPanelSize();
            }
        }

        public PanelSettings PanelSettingsPrefab
        {
            get => _panelSettingsPrefab;
            set
            {
                _panelSettingsPrefab = value;
                RebuildPanel();
            }
        }

        public RenderTexture RenderTexturePrefab
        {
            get => _renderTexturePrefab;
            set
            {
                _renderTexturePrefab = value;
                RebuildPanel();
            }
        }

        protected MeshRenderer _meshRenderer;
        protected PanelEventHandler _panelEventHandler;

        // runtime rebuildable stuff
        protected UIDocument _uiDocument;
        protected PanelSettings _panelSettings;
        protected RenderTexture _renderTexture;
        protected Material _material;

        void Awake()
        {
            // so that you can have some mesh renderers on a quad, so you can see it in the editor
            // but then they are removed when the app is run
            RemoveExistingComponents();

            PixelsPerUnit = _pixelsPerUnit;

            // dynamically a MeshFilter, MeshRenderer and BoxCollider
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();

            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            _meshRenderer.sharedMaterial = null;
            _meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _meshRenderer.receiveShadows = false;
            _meshRenderer.allowOcclusionWhenDynamic = false;
            _meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            _meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            Vector3 size = boxCollider.size;
            size.z = 0;
            boxCollider.size = size;

            // set the primitive quad mesh to the mesh filter
            GameObject quadGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            meshFilter.sharedMesh = quadGo.GetComponent<MeshFilter>().sharedMesh;
            Destroy(quadGo);
        }

        void Start()
        {
            RebuildPanel();
        }

        /// <summary>
        /// Use this method to initialise the panel without triggering a rebuild (i.e.: when instantiating it from scripts). Start method
        /// will always trigger RebuildPanel(), but if you are calling this after the GameObject started you must call RebuildPanel() so the
        /// changes take effect.
        /// </summary>
        public void InitPanel(int panelWidth, int panelHeight, float panelScale, float pixelsPerUnit, VisualTreeAsset visualTreeAsset, PanelSettings panelSettingsPrefab, RenderTexture renderTexturePrefab)
        {
            _panelWidth = panelWidth;
            _panelHeight = panelHeight;
            _panelScale = panelScale;
            _pixelsPerUnit = pixelsPerUnit;
            _visualTreeAsset = visualTreeAsset;
            _panelSettingsPrefab = panelSettingsPrefab;
            _renderTexture = renderTexturePrefab;
        }

        /// <summary>
        /// Rebuilds the panel by destroy current assets and generating new ones based on the configuration.
        /// </summary>
        public void RebuildPanel()
        {
            DestroyGeneratedAssets();

            // generate render texture
            RenderTextureDescriptor textureDescriptor = _renderTexturePrefab.descriptor;
            textureDescriptor.width = _panelWidth;
            textureDescriptor.height = _panelHeight;
            _renderTexture = new RenderTexture(textureDescriptor);

            // generate panel settings
            _panelSettings = Instantiate(_panelSettingsPrefab);
            _panelSettings.targetTexture = _renderTexture;
            _panelSettings.clearColor = true; // ConstantPixelSize and clearColor are mandatory configs
            _panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
            _panelSettings.scale = _panelScale;
            _renderTexture.name = $"{name} - RenderTexture";
            _panelSettings.name = $"{name} - PanelSettings";

            // generate UIDocument
            _uiDocument = gameObject.AddComponent<UIDocument>();
            _uiDocument.panelSettings = _panelSettings;
            _uiDocument.visualTreeAsset = _visualTreeAsset;

            // generate material
            if (_panelSettings.colorClearValue.a < 1.0f)
                _material = new Material(Shader.Find("Unlit/Transparent"));
            else
                _material = new Material(Shader.Find("Unlit/Texture"));

            _material.SetTexture("_MainTex", _renderTexture);
            _meshRenderer.sharedMaterial = _material;
            _meshRenderer.material = _material;

            RefreshPanelSize();

            // find the automatically generated PanelEventHandler and PanelRaycaster for this panel and disable the raycaster
            PanelEventHandler[] handlers = FindObjectsOfType<PanelEventHandler>();
            foreach (PanelEventHandler handler in handlers)
            {
                if (handler.panel == _uiDocument.rootVisualElement.panel)
                {
                    _panelEventHandler = handler;
                    PanelRaycaster panelRaycaster = _panelEventHandler.GetComponent<PanelRaycaster>();
                    if (panelRaycaster != null)
                        panelRaycaster.enabled = false;

                    break;
                }
            }
        }

        private void RemoveExistingComponents()
        {
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer)
            {
                DestroyImmediate(meshRenderer);
            }

            var meshCollider = GetComponent<MeshCollider>();
            if (meshCollider)
            {
                DestroyImmediate(meshCollider);
            }

            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter)
            {
                DestroyImmediate(meshFilter);
            }
        }

        protected void RefreshPanelSize()
        {
            if (_renderTexture != null && (_renderTexture.width != _panelWidth || _renderTexture.height != _panelHeight))
            {
                _renderTexture.Release();
                _renderTexture.width = _panelWidth;
                _renderTexture.height = _panelHeight;
                _renderTexture.Create();

                if (_uiDocument != null)
                    _uiDocument.rootVisualElement?.MarkDirtyRepaint();
            }

            transform.localScale = new Vector3(_panelWidth / _pixelsPerUnit, _panelHeight / _pixelsPerUnit, 1.0f);
        }

        protected void DestroyGeneratedAssets()
        {
            if (_uiDocument) Destroy(_uiDocument);
            if (_renderTexture) Destroy(_renderTexture);
            if (_panelSettings) Destroy(_panelSettings);
            if (_material) Destroy(_material);
        }

        void OnDestroy()
        {
            DestroyGeneratedAssets();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (Application.isPlaying && _material != null && _uiDocument != null)
            {
                if (_uiDocument.visualTreeAsset != _visualTreeAsset)
                    VisualTreeAsset = _visualTreeAsset;
                if (Math.Abs(_panelScale - _panelSettings.scale) > 0.01f)
                    _panelSettings.scale = _panelScale;

                RefreshPanelSize();
            }
        }
#endif

///////////////////////// REDIRECTION OF EVENTS TO THE PANEL
        protected readonly HashSet<(BaseEventData, int)> _eventsProcessedInThisFrame = new HashSet<(BaseEventData, int)>();
        private Vector3 _positionOfEventInWorld;
        private Plane _planeForPanel;
        private Ray _raycastToPanel;
        private Vector3 _worldPositionOfHitOnPanel;

        void LateUpdate()
        {
            _eventsProcessedInThisFrame.Clear();
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            TransformPointerEventForUIToolkit(eventData);
            // Debug.Log($"OnPointerMove: {eventData}");
            if (_panelEventHandler != null) _panelEventHandler.OnPointerMove(eventData);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            TransformPointerEventForUIToolkit(eventData);
            Debug.Log($"OnPointerDown: {eventData}");
            if (_panelEventHandler != null) _panelEventHandler.OnPointerDown(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            TransformPointerEventForUIToolkit(eventData);
            if (_panelEventHandler != null) _panelEventHandler.OnPointerUp(eventData);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            Debug.Log($"OnSubmit: {eventData}");
            if (_panelEventHandler != null) _panelEventHandler.OnSubmit(eventData);
        }

        public void OnCancel(BaseEventData eventData)
        {
            if (_panelEventHandler != null) _panelEventHandler.OnCancel(eventData);
        }

        public void OnMove(AxisEventData eventData)
        {
            // Debug.Log($"OnMove: {eventData}");
            if (_panelEventHandler != null) _panelEventHandler.OnMove(eventData);
        }

        public void OnScroll(PointerEventData eventData)
        {
            TransformPointerEventForUIToolkit(eventData);
            if (_panelEventHandler != null) _panelEventHandler.OnScroll(eventData);
        }

        public void OnSelect(BaseEventData eventData)
        {
            Debug.Log($"OnSelect: {eventData}");
            if (_panelEventHandler != null) _panelEventHandler.OnSelect(eventData);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (_panelEventHandler != null) _panelEventHandler.OnDeselect(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (UseDragEventFix)
                OnPointerMove(eventData);
        }

        private void OnDrawGizmosSelected()
        {
            if (_positionOfEventInWorld != Vector3.zero)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(_positionOfEventInWorld, 0.01f);
            }

            Gizmos.color = Color.red;
            Handles.ArrowHandleCap(0, transform.position, Quaternion.LookRotation(transform.forward), 1f, EventType.Repaint);

            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(_raycastToPanel);

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(_worldPositionOfHitOnPanel, 0.015f);
        }

        protected void TransformPointerEventForUIToolkit(PointerEventData eventData)
        {
            if (RaycastSource == null)
            {
                return;
            }

            var raycastSourceComponent = RaycastSource.GetComponent<IRaycastSource>();
            if (raycastSourceComponent == null)
            {
                return;
            }

            var eventKey = (eventData, eventData.pointerId);

            if (!_eventsProcessedInThisFrame.Contains(eventKey))
            {
                _eventsProcessedInThisFrame.Add(eventKey);
                Camera eventCamera = Camera.main;//eventData.enterEventCamera ? eventData.enterEventCamera : eventData.pressEventCamera;

                if (eventCamera != null)
                {
                    // get current event position and create the ray from the event camera
                    if (raycastSourceComponent.TryGetCurrentRaycastHit(eventCamera, eventData, out var hit))
                    {
                        var position = hit.point;
                        _worldPositionOfHitOnPanel = position;

                        // If I were doing XR of this; I'd get the XR raycast hit point and do an inverse transform
                        position = transform.InverseTransformPoint(position);

                        // compute a fake pointer screen position so it results in the proper panel position when projected from the camera by the PanelEventHandler
                        // reason for offset: the local position is from -0.5-0.5 (x left to right), and -0.5-0.5 (y, top to bottom, so 0.5y == top of panel).
                        // by adding .5, you make the MINIMUM  x == 0, maximum x == 1
                        // by subtracting 0.5, you make the minimum y == -1, and maximum y == 0. 
                        // this has the effect of: a) making a CORNER of the quad, 0,0
                        // inverting the coordinate system.  Once the normalized value is converted to panel units, Screen.height is added, since .y ALWAYS goes -ve/down
                        // Debug.Log($"Local pos:{position}");
                        position.x += 0.5f;
                        position.y -= 0.5f;

                        // Go from a local space position, to 'panel sized', by taking into account the panel size
                        position = Vector3.Scale(position, new Vector3(_panelWidth, _panelHeight, 1.0f));
                        position.y += Screen.height;
                        // print(new Vector2(position.x, Screen.height - position.y)); // print actual computed position in panel UIToolkit coords

                        // update the event data with the new calculated position
                        // the position is supposed to be in ... Screen coordinate space?
                        eventData.position = position;

                        // Copy the events raycast & fake the screen position
                        RaycastResult raycastResult = eventData.pointerCurrentRaycast;
                        raycastResult.screenPosition = position;

                        // copy the fake raycast back to the event
                        eventData.pointerCurrentRaycast = raycastResult;

                        // do the same for the 'press' raycast.
                        raycastResult = eventData.pointerPressRaycast;
                        raycastResult.screenPosition = position;
                        eventData.pointerPressRaycast = raycastResult;
                    }
                }
            }
        }
    }
}