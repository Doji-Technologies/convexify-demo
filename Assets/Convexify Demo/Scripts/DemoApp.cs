using System;
using System.Collections;
using System.Globalization;
using System.IO;
using Doji.Convexify;
using Dummiesman;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Doji.ConvexifyDemo {

    /// <summary>
    /// The demo: loads an OBJ model, runs the convex decomposition live while the user edits the parameters,
    /// and shows the hulls.
    /// </summary>
    /// <remarks>
    /// The Web page sends dropped files to <see cref="OnFileSelected"/> on the GameObject named "App".
    /// </remarks>
    public class DemoApp : MonoBehaviour {

        private static readonly (string Name, string File)[] s_samples = {
            ("Teapot", "Samples/Teapot.obj"),
            ("Torus", "Samples/Torus.obj"),
            ("Stairs", "Samples/Stairs.obj"),
        };

        private static readonly Color s_modelColor = new Color(0.80f, 0.82f, 0.86f, 1f);

        private readonly LiveDecomposition _decomposition = new LiveDecomposition();
        private OrbitCamera _orbit;
        private GameObject _model;
        private string _modelName = "";
        private Bounds _modelBounds;
        private GameObject _hulls;
        private Mesh _hullMesh;
        private Material _hullMaterial;
        private Material _modelMaterial;
        private Coroutine _loading;

        // view state
        private bool _showHulls = true, _showEdges = true, _showModel;
        private float _explode;
        private bool _liveUpdate = true;

        // UI
        private VisualElement _app, _panel, _status, _loadingOverlay, _samples;
        private Label _statusText, _loadingText, _modelNameLabel, _modelStatsLabel, _hint;
        private VisualElement _statusDot;
        private Button _exportButton, _computeButton;
        private VisualElement _computeDot;
        private bool _stale;
        private ParamSlider _maxHulls, _resolution, _volumeError, _recursionDepth, _maxVertices;
        private Segmented _fillMode;
        private Switch _shrinkWrap;
        private int _dragButton = -1;
        private Vector2 _lastPointer;

        private const string DefaultHint = "Hover over a setting to see what it does.";

        // ---------------------------------------------------------------- scripting access (used by the video director)

        /// <summary>The UI document of the demo.</summary>
        public UIDocument Document { get; private set; }

        /// <summary>The decomposition that runs for the current model.</summary>
        public LiveDecomposition Decomposition => _decomposition;

        /// <summary>The object that renders the hulls. It has the transform of the model root.</summary>
        public GameObject HullObject => _hulls;

        public OrbitCamera Orbit => _orbit;

        /// <summary>The world bounds of the current model.</summary>
        public Bounds ModelBounds => _modelBounds;

        /// <summary>An offset added to the view center, in normalized device coordinates.</summary>
        public Vector2 ExtraScreenShift { get; set; }

        /// <summary>True while a model loads.</summary>
        public bool IsLoading => _loading != null;

        private void Awake() {
            name = "App"; // the Web page sends messages to this name
            Application.targetFrameRate = 60;

            Camera cam = Camera.main;
            if (!cam.TryGetComponent(out _orbit)) {
                _orbit = cam.gameObject.AddComponent<OrbitCamera>();
            }
            cam.clearFlags = CameraClearFlags.Skybox;
            RenderSettings.skybox = new Material(Shader.Find("Convexify Demo/Background"));

            Shader hullShader = Shader.Find("Convexify Demo/Hull");
            _hullMaterial = new Material(hullShader) { name = "Hulls" };
            _modelMaterial = new Material(hullShader) { name = "Model" };
            _modelMaterial.SetFloat("_VertexColor", 0f);
            _modelMaterial.SetColor("_EdgeColor", Color.clear);

            _hullMesh = new Mesh { name = "Convex Hulls" };
            _hullMesh.MarkDynamic();
            _hulls = new GameObject("Convex Hulls", typeof(MeshFilter), typeof(MeshRenderer));
            _hulls.GetComponent<MeshFilter>().sharedMesh = _hullMesh;
            var hullRenderer = _hulls.GetComponent<MeshRenderer>();
            hullRenderer.sharedMaterial = _hullMaterial;
            hullRenderer.shadowCastingMode = ShadowCastingMode.Off;
            hullRenderer.receiveShadows = false;

            _decomposition.Started += OnDecompositionStarted;
            _decomposition.Completed += OnDecompositionCompleted;

            CreateUI();
        }

        private void Start() {
            LoadSample(0);
        }

        private void Update() {
            _decomposition.Update();
            // pulse the status dot while a run is busy
            float a = _decomposition.IsRunning || _decomposition.IsDirty
                ? 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f)) : 1f;
            _statusDot.style.opacity = a;
            if (_stale) {
                _computeDot.style.opacity = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f));
            }
            _orbit.ScreenShift = FreeAreaShift() + ExtraScreenShift;
        }

        /// <summary>The offset that centers the 3D view in the screen area next to (or above) the panel.</summary>
        private Vector2 FreeAreaShift() {
            // world bounds include the translate of a sliding panel
            Rect app = _app.worldBound, panel = _panel.worldBound;
            if (float.IsNaN(app.width) || app.width <= 0 || app.height <= 0) {
                return Vector2.zero;
            }
            return _app.ClassListContains("app--compact")
                ? new Vector2(0, (app.height - panel.yMin) / app.height)
                : new Vector2(Mathf.Max(0f, panel.xMax - app.xMin) / app.width, 0);
        }

        private void OnDestroy() {
            _decomposition.Dispose();
        }

        // ---------------------------------------------------------------- model loading

        /// <summary>Called by the Web page with <c>{"url", "fileName", "size"}</c> for a dropped or picked file.</summary>
        public void OnFileSelected(string fileInfoJson) {
            var info = JsonUtility.FromJson<FileInfo>(fileInfoJson);
            Load(info.url, Path.GetFileNameWithoutExtension(info.fileName), -1);
        }

        [Serializable]
        private class FileInfo {
            public string url;
            public string fileName;
            public string size;
        }

        public void LoadSample(int index) {
            string path = Path.Combine(Application.streamingAssetsPath, s_samples[index].File);
            string url = path.Contains("://") ? path : new Uri(path).AbsoluteUri;
            Load(url, s_samples[index].Name, index);
        }

        private void Load(string url, string displayName, int sampleIndex) {
            if (_loading != null) {
                StopCoroutine(_loading);
            }
            _loading = StartCoroutine(LoadRoutine(url, displayName, sampleIndex));
        }

        private IEnumerator LoadRoutine(string url, string displayName, int sampleIndex) {
            ShowLoading($"Loading {displayName}");
            using UnityWebRequest request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success) {
                ShowLoading(null);
                SetStatus($"Could not load {displayName}: {request.error}", error: true);
                _loading = null;
                yield break;
            }
            // one frame to show the loading text before the parse blocks the main thread
            yield return null;
            try {
                SetModel(request.downloadHandler.data, displayName);
                for (int i = 0; i < _samples.childCount; i++) {
                    _samples[i].EnableInClassList("chip--active", i == sampleIndex);
                }
            } catch (Exception e) {
                Debug.LogException(e);
                SetStatus($"Could not read {displayName}.obj", error: true);
            }
            ShowLoading(null);
            _loading = null;
        }

        private void SetModel(byte[] objFile, string displayName) {
            GameObject obj;
            using (var stream = new MemoryStream(objFile)) {
                obj = new OBJLoader().Load(stream);
            }

            if (!MeshGather.Gather(obj, out NativeArray<float3> vertices, out NativeArray<int> indices)) {
                vertices.Dispose();
                indices.Dispose();
                DestroyModel(obj);
                throw new InvalidDataException("The file has no triangles.");
            }

            if (_model != null) {
                DestroyModel(_model);
            }
            _model = obj;
            _modelName = displayName;
            obj.name = displayName;

            _modelBounds = new Bounds(obj.transform.position, Vector3.zero);
            bool first = true;
            foreach (MeshRenderer r in obj.GetComponentsInChildren<MeshRenderer>(true)) {
                var materials = new Material[r.sharedMaterials.Length];
                Array.Fill(materials, _modelMaterial);
                r.sharedMaterials = materials;
                r.shadowCastingMode = ShadowCastingMode.Off;
                if (first) {
                    _modelBounds = r.bounds;
                    first = false;
                } else {
                    _modelBounds.Encapsulate(r.bounds);
                }
            }

            // the hulls are in the local space of the model root
            _hulls.transform.SetPositionAndRotation(obj.transform.position, obj.transform.rotation);
            _hulls.transform.localScale = obj.transform.localScale;
            _hullMaterial.SetVector("_ExplodeCenter", _hulls.transform.InverseTransformPoint(_modelBounds.center));
            _hullMesh.Clear();

            _modelNameLabel.text = displayName;
            _modelStatsLabel.text = $"{N(vertices.Length)} vertices  ·  {N(indices.Length / 3)} triangles";

            _orbit.Frame(_modelBounds, instant: true);
            // a new model always gets hulls, also without live update
            _decomposition.SetInput(vertices, indices);
            ApplyView();
        }

        private static void DestroyModel(GameObject obj) {
            foreach (MeshFilter mf in obj.GetComponentsInChildren<MeshFilter>(true)) {
                Destroy(mf.sharedMesh);
            }
            Destroy(obj);
        }

        // ---------------------------------------------------------------- decomposition

        private void OnParametersChanged() {
            if (_liveUpdate) {
                _decomposition.Invalidate();
            } else {
                SetStatus("Settings changed. Press Compute.", busy: true);
                SetStale(true);
            }
        }

        /// <summary>Marks the hulls as out of date with the settings: the Compute button asks for attention.</summary>
        private void SetStale(bool stale) {
            _stale = stale;
            _computeButton.EnableInClassList("compute-button--stale", stale);
            _computeButton.text = stale ? "Compute  ·  settings changed" : "Compute";
        }

        private void OnDecompositionStarted() {
            SetStatus("Computing", busy: true);
            SetStale(false);
        }

        private void OnDecompositionCompleted() {
            NativeConvexDecomposition result = _decomposition.Result;
            HullMeshBuilder.Build(result, _hullMesh);

            int vertexCount = 0;
            for (int h = 0; h < result.HullCount; h++) {
                vertexCount += result.Hulls[h].PointCount;
            }
            string hulls = result.HullCount == 1 ? "1 hull" : $"{N(result.HullCount)} hulls";
            SetStatus($"{hulls}  ·  {N(vertexCount)} vertices  ·  {FormatMs(_decomposition.LastDurationMs)}",
                busy: _decomposition.IsDirty);
            _exportButton.SetEnabled(result.HullCount > 0);
        }

        private void ExportHulls() {
            NativeConvexDecomposition result = _decomposition.Result;
            if (!result.IsCreated || result.HullCount == 0) {
                return;
            }
            byte[] file = OBJExporter.Serialize(result, _modelName);
            string fileName = $"{_modelName}_convex_hulls.obj";
#if UNITY_EDITOR
            string path = UnityEditor.EditorUtility.SaveFilePanel("Export convex hulls", "", fileName, "obj");
            if (!string.IsNullOrEmpty(path)) {
                File.WriteAllBytes(path, file);
            }
#else
            WebGLUtils.DownloadFile(file, fileName, "model/obj");
#endif
        }

        private void OpenFile() {
#if UNITY_EDITOR
            string path = UnityEditor.EditorUtility.OpenFilePanel("Open OBJ model", "", "obj");
            if (!string.IsNullOrEmpty(path)) {
                Load(new Uri(path).AbsoluteUri, Path.GetFileNameWithoutExtension(path), -1);
            }
#else
            WebGLUtils.OpenFilePicker(name, nameof(OnFileSelected), ".obj");
#endif
        }

        // ---------------------------------------------------------------- view

        private void ApplyView() {
            _hulls.SetActive(_showHulls);
            if (_model != null) {
                _model.SetActive(_showModel || !_showHulls);
            }
            _hullMaterial.SetColor("_EdgeColor", new Color(0.06f, 0.07f, 0.09f, _showEdges ? 0.38f : 0f));
            _hullMaterial.SetFloat("_Explode", _explode);

            // with the hulls visible, the model is a ghost that shows where it sticks out of the hulls
            bool ghost = _showHulls;
            _modelMaterial.SetColor("_Color", ghost ? new Color(s_modelColor.r, s_modelColor.g, s_modelColor.b, 0.22f) : s_modelColor);
            _modelMaterial.SetFloat("_SrcBlend", (float)(ghost ? BlendMode.SrcAlpha : BlendMode.One));
            _modelMaterial.SetFloat("_DstBlend", (float)(ghost ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
            _modelMaterial.SetFloat("_ZWrite", ghost ? 0f : 1f);
            _modelMaterial.SetFloat("_DepthOffset", ghost ? -1f : 0f);
            _modelMaterial.renderQueue = ghost ? (int)RenderQueue.Transparent : (int)RenderQueue.Geometry;
        }

        // ---------------------------------------------------------------- UI

        private void CreateUI() {
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.name = "Convexify Demo Panel";
            panelSettings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("ConvexifyDemo/Theme");
            panelSettings.scaleMode = PanelScaleMode.ConstantPhysicalSize;
            panelSettings.referenceDpi = 96;
            panelSettings.fallbackDpi = 96;
            panelSettings.clearColor = false;

            var document = gameObject.AddComponent<UIDocument>();
            Document = document;
            document.panelSettings = panelSettings;
            document.visualTreeAsset = Resources.Load<VisualTreeAsset>("ConvexifyDemo/Main");
            VisualElement root = document.rootVisualElement;

            _app = root.Q("app");
            _panel = root.Q("panel");
            _status = root.Q("status");
            _statusDot = root.Q("status-dot");
            _statusText = root.Q<Label>("status-text");
            _loadingOverlay = root.Q("loading");
            _loadingText = root.Q<Label>("loading-text");
            _modelNameLabel = root.Q<Label>("model-name");
            _modelStatsLabel = root.Q<Label>("model-stats");
            _hint = root.Q<Label>("hint");
            _hint.text = DefaultHint;
            _samples = root.Q("samples");

            _app.RegisterCallback<GeometryChangedEvent>(e => _app.EnableInClassList("app--compact", e.newRect.width < 720f));

            for (int i = 0; i < s_samples.Length; i++) {
                int index = i;
                var chip = new Button(() => LoadSample(index)) { text = s_samples[i].Name };
                chip.AddToClassList("chip");
                _samples.Add(chip);
            }
            root.Q<Button>("open-button").clicked += OpenFile;
            root.Q<Label>("drop-hint").style.display = WebGLUtils.IsSupported ? DisplayStyle.Flex : DisplayStyle.None;

            // decomposition parameters
            Parameters p = _decomposition.Parameters;
            _maxHulls = BindSlider(root, "max-hulls", v => p.MaxConvexHulls = (int)v);
            _resolution = BindSlider(root, "resolution", v => p.Resolution = (int)v);
            _volumeError = BindSlider(root, "volume-error", v => p.MinimumVolumePercentErrorAllowed = v);
            _recursionDepth = BindSlider(root, "recursion-depth", v => p.MaxRecursionDepth = (int)v);
            _maxVertices = BindSlider(root, "max-vertices", v => p.MaxNumVerticesPerCH = (int)v);
            _fillMode = root.Q<Segmented>("fill-mode");
            _fillMode.RegisterValueChangedCallback(e => { p.FillMode = (FillMode)e.newValue; OnParametersChanged(); });
            _shrinkWrap = root.Q<Switch>("shrink-wrap");
            _shrinkWrap.RegisterValueChangedCallback(e => { p.ShrinkWrap = e.newValue; OnParametersChanged(); });
            root.Q<Button>("reset-button").clicked += ResetParameters;

            _computeButton = root.Q<Button>("compute-button");
            _computeButton.clicked += () => _decomposition.Invalidate();
            _computeButton.style.display = DisplayStyle.None;
            _computeDot = new VisualElement { pickingMode = PickingMode.Ignore };
            _computeDot.AddToClassList("compute-button__dot");
            _computeButton.Add(_computeDot);
            root.Q<Switch>("live-update").RegisterValueChangedCallback(e => {
                _liveUpdate = e.newValue;
                _computeButton.style.display = _liveUpdate ? DisplayStyle.None : DisplayStyle.Flex;
                if (_liveUpdate) {
                    _decomposition.Invalidate();
                }
            });
            SyncParameterControls();

            // view
            BindSwitch(root, "show-hulls", _showHulls, v => _showHulls = v);
            BindSwitch(root, "show-edges", _showEdges, v => _showEdges = v);
            BindSwitch(root, "show-model", _showModel, v => _showModel = v);
            var explode = root.Q<ParamSlider>("explode");
            explode.SetValueWithoutNotify(_explode);
            explode.ValueChanged += v => { _explode = v; ApplyView(); };

            _exportButton = root.Q<Button>("export-button");
            _exportButton.clicked += ExportHulls;
            _exportButton.SetEnabled(false);

            // hints for every control that has one
            root.Query<VisualElement>().ForEach(e => {
                string hint = e switch {
                    ParamSlider s => s.Hint,
                    Switch s => s.Hint,
                    Segmented s => s.Hint,
                    _ => null,
                };
                if (!string.IsNullOrEmpty(hint)) {
                    e.RegisterCallback<PointerEnterEvent>(_ => _hint.text = hint);
                    e.RegisterCallback<PointerLeaveEvent>(_ => _hint.text = DefaultHint);
                }
            });

            RegisterViewportInput(root.Q("viewport"));
        }

        private ParamSlider BindSlider(VisualElement root, string elementName, Action<float> apply) {
            var slider = root.Q<ParamSlider>(elementName);
            slider.ValueChanged += v => { apply(v); OnParametersChanged(); };
            return slider;
        }

        private void BindSwitch(VisualElement root, string elementName, bool initial, Action<bool> apply) {
            var s = root.Q<Switch>(elementName);
            s.SetValueWithoutNotify(initial);
            s.RegisterValueChangedCallback(e => { apply(e.newValue); ApplyView(); });
        }

        private void SyncParameterControls() {
            Parameters p = _decomposition.Parameters;
            _maxHulls.SetValueWithoutNotify(p.MaxConvexHulls);
            _resolution.SetValueWithoutNotify(p.Resolution);
            _volumeError.SetValueWithoutNotify(p.MinimumVolumePercentErrorAllowed);
            _recursionDepth.SetValueWithoutNotify(p.MaxRecursionDepth);
            _maxVertices.SetValueWithoutNotify(p.MaxNumVerticesPerCH);
            _fillMode.SetValueWithoutNotify((int)p.FillMode);
            _shrinkWrap.SetValueWithoutNotify(p.ShrinkWrap);
        }

        private void ResetParameters() {
            Parameters p = _decomposition.Parameters;
            var defaults = new Parameters();
            p.MaxConvexHulls = defaults.MaxConvexHulls;
            p.Resolution = defaults.Resolution;
            p.MinimumVolumePercentErrorAllowed = defaults.MinimumVolumePercentErrorAllowed;
            p.MaxRecursionDepth = defaults.MaxRecursionDepth;
            p.MaxNumVerticesPerCH = defaults.MaxNumVerticesPerCH;
            p.FillMode = defaults.FillMode;
            p.ShrinkWrap = defaults.ShrinkWrap;
            SyncParameterControls();
            OnParametersChanged();
        }

        private void RegisterViewportInput(VisualElement viewport) {
            viewport.RegisterCallback<PointerDownEvent>(e => {
                if (e.clickCount == 2 && e.button == 0) {
                    _orbit.Frame(_modelBounds, instant: false);
                    return;
                }
                _dragButton = e.button;
                _lastPointer = e.position;
                viewport.CapturePointer(e.pointerId);
            });
            viewport.RegisterCallback<PointerMoveEvent>(e => {
                if (!viewport.HasPointerCapture(e.pointerId)) {
                    return;
                }
                Vector2 delta = (Vector2)e.position - _lastPointer;
                _lastPointer = e.position;
                if (_dragButton == 0 && !e.shiftKey) {
                    _orbit.Rotate(delta);
                } else {
                    _orbit.Pan(delta * (Screen.height / Mathf.Max(viewport.layout.height, 1f)));
                }
            });
            viewport.RegisterCallback<PointerUpEvent>(e => {
                if (viewport.HasPointerCapture(e.pointerId)) {
                    viewport.ReleasePointer(e.pointerId);
                }
                _dragButton = -1;
            });
            viewport.RegisterCallback<WheelEvent>(e => {
                _orbit.Zoom(Mathf.Clamp(e.delta.y, -3f, 3f) * 0.06f);
                e.StopPropagation();
            });
        }

        private void SetStatus(string text, bool busy = false, bool error = false) {
            _statusText.text = text;
            _status.EnableInClassList("status--busy", busy);
            _status.EnableInClassList("status--error", error);
        }

        private void ShowLoading(string text) {
            _loadingOverlay.EnableInClassList("loading--visible", text != null);
            if (text != null) {
                _loadingText.text = text;
            }
        }

        private static string N(int value) => value.ToString("N0", CultureInfo.InvariantCulture);

        private static string FormatMs(double ms) =>
            ms < 10 ? ms.ToString("0.0", CultureInfo.InvariantCulture) + " ms" : ms.ToString("N0", CultureInfo.InvariantCulture) + " ms";
    }
}
