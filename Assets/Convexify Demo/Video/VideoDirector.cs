#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Doji.Convexify;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace Doji.ConvexifyDemo.Video {

    /// <summary>
    /// Plays a scripted walkthrough of the demo and records it to an MP4 file with ffmpeg.
    /// </summary>
    /// <remarks>
    /// Editor only. Open the ReleaseVideo scene, set the Game view to the output resolution, and enter Play mode.
    /// Time runs at a fixed <see cref="Fps"/> (Time.captureFramerate), so the video is smooth however long a frame
    /// takes to render, capture and encode. The decomposition still runs at its real speed: the status pill shows
    /// real timings.
    /// </remarks>
    public class VideoDirector : MonoBehaviour {

        public DemoApp App;
        public bool Record = true;
        public int Fps = 60;
        public string FFmpeg = "ffmpeg";
        public string OutputPath = "Recordings/convexify-release.mp4";
        public int Crf = 15;

        private VisualElement _demoRoot, _demoPanel, _status;
        private VisualElement _backdrop, _cursor, _caption, _activeCard;
        private Label _chapter, _headline, _subline;
        private readonly Dictionary<string, VisualElement> _cards = new Dictionary<string, VisualElement>();
        private Vector2 _cursorPos = new Vector2(1500, 700);
        private readonly List<GameObject> _balls = new List<GameObject>();
        private Material _ballMaterial;
        private float _autoYaw;

        private Process _ffmpeg;
        private Stream _ffmpegIn;
        private bool _recording;
        private int _frames;

        // ------------------------------------------------------------------ setup

        private IEnumerator Start() {
            Time.captureFramerate = Fps;
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 8;
            Time.fixedDeltaTime = 1f / Fps;

            // wait for the demo UI and the first model
            yield return null;
            while (App.IsLoading) {
                yield return null;
            }
            SetupDemoUI();
            CreateOverlay();

            _ballMaterial = new Material(Shader.Find("Convexify Demo/Hull"));
            _ballMaterial.SetFloat("_VertexColor", 0f);
            _ballMaterial.SetColor("_Color", new Color(0.96f, 0.97f, 1f));
            _ballMaterial.SetColor("_EdgeColor", Color.clear);

            // keep the lower part of the screen free for the captions
            App.ExtraScreenShift = new Vector2(0, 0.2f);

            // the opening state: torus, one hull, no panel
            App.LoadSample(1);
            yield return null;
            while (App.IsLoading) {
                yield return null;
            }
            Slider("max-hulls").SetValue(1);
            yield return WaitForHulls();
            _demoPanel.style.translate = new Translate(-440, 0);
            _demoPanel.style.opacity = 0;
            _status.style.opacity = 0;
            yield return null;
            App.Orbit.Frame(App.ModelBounds, true);
            App.Orbit.SetView(-30, 40, 0.95f, true);
            // let layout, camera and screen size settle
            for (int i = 0; i < 10; i++) {
                yield return null;
            }

            if (Record && !StartRecording()) {
                yield break;
            }
            yield return Script();
            StopRecording();
            UnityEditor.EditorApplication.isPlaying = false;
        }

        private void SetupDemoUI() {
            // the same 1920 x 1080 layout at any output size
            PanelSettings demoSettings = App.Document.panelSettings;
            demoSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            demoSettings.referenceResolution = new Vector2Int(1920, 1080);
            demoSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            demoSettings.match = 0.5f;

            _demoRoot = App.Document.rootVisualElement;
            _demoPanel = _demoRoot.Q("panel");
            _status = _demoRoot.Q("status");
            _demoRoot.Q("help").style.display = DisplayStyle.None;
        }

        private void CreateOverlay() {
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            PanelSettings demoSettings = App.Document.panelSettings;
            settings.themeStyleSheet = demoSettings.themeStyleSheet;
            settings.scaleMode = demoSettings.scaleMode;
            settings.referenceResolution = demoSettings.referenceResolution;
            settings.screenMatchMode = demoSettings.screenMatchMode;
            settings.match = demoSettings.match;
            settings.sortingOrder = 10;
            settings.clearColor = false;

            var doc = gameObject.AddComponent<UIDocument>();
            doc.panelSettings = settings;
            VisualElement root = doc.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            root.styleSheets.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/Convexify Demo/Video/VideoOverlay.uss"));

            var overlay = new VisualElement { pickingMode = PickingMode.Ignore };
            overlay.AddToClassList("v-root");
            root.Add(overlay);

            _backdrop = Add(overlay, "v-backdrop");

            _caption = Add(overlay, "v-caption");
            _chapter = AddLabel(_caption, "v-chapter", "");
            _headline = AddLabel(_caption, "v-headline", "");
            _subline = AddLabel(_caption, "v-subline", "");

            // title card
            VisualElement intro = Add(overlay, "v-card");
            Add(Add(intro, "v-mark"), "v-mark__inner");
            AddLabel(intro, "v-title", "Convexify");
            AddLabel(intro, "v-subtitle", "Convex compound colliders for any mesh. At runtime.");
            _cards["intro"] = intro;

            // code card
            VisualElement code = Add(overlay, "v-card");
            AddLabel(code, "v-chapter", "SIMPLE API");
            AddLabel(code, "v-headline", "Three lines of code.");
            VisualElement block = Add(code, "v-code");
            FontAsset mono = FontAsset.CreateFontAsset("Consolas", "Regular");
            const string k = "<color=#C792EA>", t = "<color=#82AAFF>", m = "<color=#FFCB6B>", c = "</color>";
            foreach (string line in new[] {
                         $"{k}var{c} vhacd = {k}new{c} {t}VHACD{c}();",
                         $"{t}ConvexDecomposition{c} hulls = vhacd.{m}Compute{c}(mesh, 0);",
                         $"vhacd.{m}GenerateColliders{c}(hulls, gameObject);",
                     }) {
                Label l = AddLabel(block, "v-code__line", line);
                if (mono != null) {
                    l.style.unityFontDefinition = FontDefinition.FromSDFFont(mono);
                }
            }
            AddLabel(code, "v-code__note", "Need it off the main thread? VHACD.Schedule runs the decomposition as Burst jobs.");
            _cards["code"] = code;

            // end card
            VisualElement outro = Add(overlay, "v-card");
            Add(Add(outro, "v-mark"), "v-mark__inner");
            AddLabel(outro, "v-title", "Convexify");
            AddLabel(outro, "v-subtitle", "Runtime convex decomposition for Unity");
            Add(outro, "v-spacer");
            VisualElement pills = Add(outro, "v-pills");
            foreach (string p in new[] { "Burst-compiled jobs", "Pure C#, no native plugins", "Every Unity platform, even the Web" }) {
                AddLabel(pills, "v-pill", p);
            }
            Add(outro, "v-spacer");
            AddLabel(outro, "v-line v-line--accent", "Available on the Unity Asset Store");
            AddLabel(outro, "v-line", "Try the live demo: doji-tech.com/convexify-demo");
            _cards["outro"] = outro;

            _cursor = Add(overlay, "v-cursor");
        }

        private static VisualElement Add(VisualElement parent, string classes) {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            foreach (string c in classes.Split(' ')) {
                e.AddToClassList(c);
            }
            parent.Add(e);
            return e;
        }

        private static Label AddLabel(VisualElement parent, string classes, string text) {
            var l = new Label(text) { pickingMode = PickingMode.Ignore };
            foreach (string c in classes.Split(' ')) {
                l.AddToClassList(c);
            }
            parent.Add(l);
            return l;
        }

        // ------------------------------------------------------------------ the video

        private IEnumerator Script() {
            // 1. title over the scene
            _backdrop.style.opacity = 1;
            _autoYaw = 10f;
            yield return Fade(_cards["intro"], 0, 1, 0.9f);
            yield return Wait(2.6f);
            StartCoroutine(Fade(_cards["intro"], 1, 0, 0.7f));
            yield return Fade(_backdrop, 1, 0, 1.0f);

            // 2. the problem: one convex hull
            yield return Caption("THE PROBLEM", "Dynamic rigidbodies need convex colliders.",
                "A non-kinematic Rigidbody can only use convex mesh colliders.");
            yield return Wait(3.4f);
            yield return Caption("THE PROBLEM", "A single convex hull fills every hole.",
                "So objects bounce off space that should be empty.");
            yield return MakeColliders();
            StartCoroutine(DropBalls(60, 3.2f));
            yield return Wait(4.6f);

            // 3. the fix: convex decomposition
            yield return HideCaption();
            ClearBalls();
            StartCoroutine(Tween(0.9f, Ease.OutCubic, x => {
                _demoPanel.style.translate = new Translate(Mathf.Lerp(-440, 0, x), 0);
                _demoPanel.style.opacity = x;
                _status.style.opacity = x;
            }));
            yield return Wait(0.5f);
            App.Orbit.Frame(App.ModelBounds, false, keepAngles: true);
            yield return Caption("CONVEXIFY", "Split any mesh into convex parts.",
                "V-HACD convex decomposition, computed at runtime.");
            yield return ShowCursor(true);
            yield return DragSlider("max-hulls", 24, 2.4f);
            yield return WaitForHulls();
            yield return Wait(0.6f);
            yield return Caption("CONVEXIFY", "Now physics matches the shape.",
                "One convex MeshCollider for each hull: a compound collider.");
            yield return MakeColliders();
            StartCoroutine(DropBalls(60, 3.2f));
            yield return Wait(4.8f);
            ClearBalls();

            // 4. real time
            yield return Caption("REAL TIME", "Results update as you drag.",
                "Burst-compiled jobs. No editor bake step.");
            yield return ClickChip(0);
            yield return Wait(0.8f);
            yield return DragSlider("max-hulls", 64, 1.8f);
            yield return Wait(0.4f);
            yield return DragSlider("resolution", 96, 2.6f);
            yield return WaitForHulls();
            yield return Wait(0.6f);
            yield return DragSlider("resolution", 48, 1.8f);
            yield return WaitForHulls();
            yield return Wait(0.8f);

            // 5. inspect
            yield return Caption("INSPECT", "See how the parts divide the shape.", "");
            yield return DragSlider("explode", 0.45f, 1.8f);
            yield return MoveCursorAway();
            _autoYaw = 22f;
            yield return Wait(4.5f);
            _autoYaw = 10f;
            yield return DragSlider("explode", 0f, 1.4f);
            yield return Wait(0.4f);

            // 6. accuracy
            yield return Caption("ACCURACY", "Check the fit against the source mesh.",
                "Tune resolution, volume error and vertex limits for quality or speed.");
            yield return ClickSwitch("show-model");
            yield return Wait(0.6f);
            yield return DragSlider("volume-error", 8f, 2.2f);
            yield return WaitForHulls();
            yield return Wait(1.0f);
            yield return DragSlider("volume-error", 0.5f, 2.2f);
            yield return WaitForHulls();
            yield return Wait(1.0f);
            yield return ClickSwitch("show-model");

            // 7. runtime content
            yield return Caption("RUNTIME CONTENT", "Imported, procedural or downloaded meshes.",
                "Make colliders for meshes that do not exist at design time.");
            yield return ClickChip(2);
            yield return Wait(2.0f);
            yield return ClickChip(1);
            yield return Wait(2.0f);
            yield return ClickChip(0);
            yield return Wait(1.6f);
            yield return ShowCursor(false);
            yield return HideCaption();

            // 8. code
            yield return ShowCard("code", 5.2f);

            // 9. end card
            _backdrop.style.opacity = 0;
            yield return Fade(_backdrop, 0, 1, 0.6f);
            yield return Fade(_cards["outro"], 0, 1, 0.8f);
            yield return Wait(4.2f);
            yield return Fade(_cards["outro"], 1, 0, 0.8f);
            yield return Wait(0.3f);
        }

        // ------------------------------------------------------------------ building blocks

        private void Update() {
            if (_autoYaw != 0f) {
                App.Orbit.AddYaw(_autoYaw * Time.deltaTime);
            }
            if (_cursor != null) {
                _cursor.style.left = _cursorPos.x;
                _cursor.style.top = _cursorPos.y;
            }
            for (int i = _balls.Count - 1; i >= 0; i--) {
                if (_balls[i].transform.position.y < App.ModelBounds.min.y - App.ModelBounds.size.magnitude * 3f) {
                    Destroy(_balls[i]);
                    _balls.RemoveAt(i);
                }
            }
        }

        private static IEnumerator Wait(float seconds) {
            float end = Time.time + seconds;
            while (Time.time < end) {
                yield return null;
            }
        }

        private enum Ease { Linear, InOutCubic, OutCubic }

        private static float Apply(Ease ease, float x) {
            switch (ease) {
                case Ease.InOutCubic: return x < 0.5f ? 4 * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 3) / 2;
                case Ease.OutCubic: return 1 - Mathf.Pow(1 - x, 3);
                default: return x;
            }
        }

        private static IEnumerator Tween(float duration, Ease ease, Action<float> apply) {
            float start = Time.time;
            while (true) {
                float x = Mathf.Clamp01((Time.time - start) / duration);
                apply(Apply(ease, x));
                if (x >= 1f) {
                    yield break;
                }
                yield return null;
            }
        }

        private static IEnumerator Fade(VisualElement e, float from, float to, float duration) {
            return Tween(duration, Ease.InOutCubic, x => e.style.opacity = Mathf.Lerp(from, to, x));
        }

        private IEnumerator Caption(string chapter, string headline, string subline) {
            if (_caption.resolvedStyle.opacity > 0.01f) {
                yield return HideCaption();
            }
            _chapter.text = chapter;
            _headline.text = headline;
            _subline.text = subline;
            _subline.style.display = string.IsNullOrEmpty(subline) ? DisplayStyle.None : DisplayStyle.Flex;
            // center in the screen area right of the demo panel
            _caption.style.left = Mathf.Max(0f, _demoPanel.worldBound.xMax);
            yield return Tween(0.55f, Ease.OutCubic, x => {
                _caption.style.opacity = x;
                _caption.style.translate = new Translate(0, Mathf.Lerp(26, 0, x));
            });
        }

        private IEnumerator HideCaption() {
            yield return Tween(0.3f, Ease.InOutCubic, x => {
                _caption.style.opacity = 1 - x;
                _caption.style.translate = new Translate(0, Mathf.Lerp(0, -12, x));
            });
        }

        private IEnumerator ShowCard(string name, float hold) {
            yield return Fade(_backdrop, 0, 1, 0.6f);
            yield return Fade(_cards[name], 0, 1, 0.6f);
            yield return Wait(hold);
            yield return Fade(_cards[name], 1, 0, 0.5f);
            yield return Fade(_backdrop, 1, 0, 0.4f);
        }

        private IEnumerator ShowCursor(bool show) {
            float from = _cursor.resolvedStyle.opacity;
            yield return Tween(0.3f, Ease.InOutCubic, x => _cursor.style.opacity = Mathf.Lerp(from, show ? 1 : 0, x));
        }

        private IEnumerator MoveCursor(Vector2 target, float duration) {
            Vector2 start = _cursorPos;
            // a slight arc looks more like a hand than a straight line
            Vector2 normal = Vector2.Perpendicular(target - start).normalized * Mathf.Min(60f, (target - start).magnitude * 0.12f);
            yield return Tween(duration, Ease.InOutCubic, x => {
                _cursorPos = Vector2.Lerp(start, target, x) + normal * Mathf.Sin(x * Mathf.PI);
            });
        }

        private IEnumerator MoveCursorAway() {
            StartCoroutine(MoveCursor(new Vector2(_demoPanel.worldBound.xMax + 90, _cursorPos.y + 40), 0.5f));
            yield return ShowCursor(false);
        }

        /// <summary>Fades the pointer in near <paramref name="target"/> if it is hidden.</summary>
        private IEnumerator EnsureCursor(Vector2 target) {
            if (_cursor.resolvedStyle.opacity < 0.5f) {
                _cursorPos = target + new Vector2(140, 60);
                yield return ShowCursor(true);
            }
        }

        private IEnumerator Press(bool down) {
            yield return Tween(0.12f, Ease.OutCubic, x => {
                float s = down ? Mathf.Lerp(1f, 0.78f, x) : Mathf.Lerp(0.78f, 1f, x);
                _cursor.style.scale = new Scale(new Vector3(s, s, 1));
                _cursor.style.backgroundColor = new Color(1, 1, 1, down ? Mathf.Lerp(0.18f, 0.45f, x) : Mathf.Lerp(0.45f, 0.18f, x));
            });
        }

        private ParamSlider Slider(string name) => _demoRoot.Q<ParamSlider>(name);

        private void ScrollTo(VisualElement e) {
            _demoRoot.Q<ScrollView>("scroll").ScrollTo(e);
        }

        private IEnumerator DragSlider(string name, float target, float duration) {
            ParamSlider slider = Slider(name);
            ScrollTo(slider);
            yield return null;
            SetHint(slider.Hint);
            yield return EnsureCursor(slider.Dragger.worldBound.center);
            yield return MoveCursor(slider.Dragger.worldBound.center, 0.55f);
            yield return Press(true);
            float from = slider.Value;
            yield return Tween(duration, Ease.InOutCubic, x => {
                slider.SetValue(Mathf.Lerp(from, target, x));
                _cursorPos = slider.Dragger.worldBound.center;
            });
            yield return null;
            _cursorPos = slider.Dragger.worldBound.center;
            yield return Press(false);
        }

        private IEnumerator ClickChip(int index) {
            VisualElement chip = _demoRoot.Q("samples")[index];
            ScrollTo(chip);
            yield return null;
            yield return EnsureCursor(chip.worldBound.center);
            yield return MoveCursor(chip.worldBound.center, 0.55f);
            yield return Press(true);
            App.LoadSample(index);
            yield return Press(false);
            yield return null;
            while (App.IsLoading) {
                yield return null;
            }
        }

        private IEnumerator ClickSwitch(string name) {
            var s = _demoRoot.Q<Switch>(name);
            ScrollTo(s);
            yield return null;
            SetHint(s.Hint);
            VisualElement track = s.Q(className: "switch__track");
            yield return EnsureCursor(track.worldBound.center);
            yield return MoveCursor(track.worldBound.center, 0.55f);
            yield return Press(true);
            s.value = !s.value;
            yield return Press(false);
        }

        private void SetHint(string hint) {
            _demoRoot.Q<Label>("hint").text = hint;
        }

        private IEnumerator WaitForHulls() {
            yield return null;
            while (App.Decomposition.IsRunning || App.Decomposition.IsDirty) {
                yield return null;
            }
        }

        // ------------------------------------------------------------------ physics

        private IEnumerator MakeColliders() {
            yield return WaitForHulls();
            GameObject target = App.HullObject;
            foreach (Collider c in target.GetComponentsInChildren<Collider>()) {
                Destroy(c);
            }
            yield return null;
            new VHACD().GenerateColliders(App.Decomposition.Result, target);
        }

        /// <summary>Drops a stream of balls onto the model over <paramref name="duration"/> seconds.</summary>
        private IEnumerator DropBalls(int count, float duration) {
            Bounds b = App.ModelBounds;
            float r = b.extents.magnitude;
            float ball = r * 0.075f;
            Random.InitState(7);
            float start = Time.time;
            for (int i = 0; i < count; i++) {
                while (Time.time < start + duration * i / count) {
                    yield return null;
                }
                GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                g.name = "Ball";
                g.transform.localScale = Vector3.one * ball;
                Vector2 d = Random.insideUnitCircle * r * 0.42f;
                g.transform.position = b.center + new Vector3(d.x, b.extents.y + r * (0.55f + Random.value * 0.1f), d.y);
                g.GetComponent<MeshRenderer>().sharedMaterial = _ballMaterial;
                var rb = g.AddComponent<Rigidbody>();
                rb.mass = 0.2f;
                _balls.Add(g);
            }
        }

        private void ClearBalls() {
            foreach (GameObject g in _balls) {
                Destroy(g);
            }
            _balls.Clear();
        }

        // ------------------------------------------------------------------ recording

        private bool StartRecording() {
            if (Screen.width % 2 != 0 || Screen.height % 2 != 0) {
                Debug.LogError($"VideoDirector: the frame size must be even, got {Screen.width} x {Screen.height}.");
                return false;
            }
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputPath));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var info = new ProcessStartInfo {
                FileName = FFmpeg,
                Arguments = $"-y -loglevel error -f rawvideo -pix_fmt rgba -s {Screen.width}x{Screen.height} -r {Fps} -i - " +
                            $"-vf vflip -c:v libx264 -preset slow -crf {Crf} -pix_fmt yuv420p -profile:v high " +
                            $"-movflags +faststart \"{output}\"",
                UseShellExecute = false,
                RedirectStandardInput = true,
                CreateNoWindow = true,
            };
            _ffmpeg = Process.Start(info);
            _ffmpegIn = _ffmpeg.StandardInput.BaseStream;
            _recording = true;
            StartCoroutine(CaptureFrames());
            Debug.Log($"VideoDirector: recording {Screen.width} x {Screen.height} at {Fps} fps to {output}");
            return true;
        }

        private IEnumerator CaptureFrames() {
            var endOfFrame = new WaitForEndOfFrame();
            var rgba = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
            while (_recording) {
                yield return endOfFrame;
                if (!_recording) {
                    break;
                }
                Texture2D frame = ScreenCapture.CaptureScreenshotAsTexture();
                if (frame.width != rgba.width || frame.height != rgba.height) {
                    Debug.LogError($"VideoDirector: the screen size changed to {frame.width} x {frame.height}.");
                    Destroy(frame);
                    _recording = false;
                    break;
                }
                if (frame.format != TextureFormat.RGBA32) {
                    rgba.SetPixels32(frame.GetPixels32());
                    _ffmpegIn.Write(rgba.GetRawTextureData<byte>().AsReadOnlySpan());
                } else {
                    _ffmpegIn.Write(frame.GetRawTextureData<byte>().AsReadOnlySpan());
                }
                Destroy(frame);
                _frames++;
            }
            Destroy(rgba);
        }

        private void StopRecording() {
            if (_ffmpeg == null) {
                return;
            }
            _recording = false;
            _ffmpegIn.Flush();
            _ffmpegIn.Close();
            _ffmpeg.WaitForExit();
            Debug.Log($"VideoDirector: wrote {_frames} frames ({_frames / (float)Fps:0.0} s), ffmpeg exit code {_ffmpeg.ExitCode}");
            _ffmpeg = null;
        }

        private void OnDestroy() {
            StopRecording();
        }
    }
}
#endif
