using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace FormulaRacer.Editor
{
    public static class Milestone3Setup
    {
        public const string PrefabPath = "Assets/_Game/Prefabs/MobileControls.prefab";

        [MenuItem("FormulaRacer/Milestone 3/Configure Mobile Controls")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            Scene scene = SceneManager.GetSceneByPath(PrototypeBuilder.ScenePath);
            bool opened = !scene.IsValid();
            if (!opened && scene.isDirty) throw new InvalidOperationException("Save or close modified TestTrack before setup.");
            if (opened) scene = EditorSceneManager.OpenScene(PrototypeBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                EnsureScene(scene);
                ValidateScene(scene);
                ConfigureLandscape();
                if (scene.isDirty && !EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save touch controls.");
                Debug.Log("FormulaRacer Milestone 3 setup PASSED: six controls, one Canvas/EventSystem, input wired, landscape-only; build order unchanged.");
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        public static void ConfigureLandscape()
        {
            if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.AutoRotation) PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            if (PlayerSettings.allowedAutorotateToPortrait) PlayerSettings.allowedAutorotateToPortrait = false;
            if (PlayerSettings.allowedAutorotateToPortraitUpsideDown) PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            if (!PlayerSettings.allowedAutorotateToLandscapeLeft) PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            if (!PlayerSettings.allowedAutorotateToLandscapeRight) PlayerSettings.allowedAutorotateToLandscapeRight = true;
            // Batch-mode shutdown does not reliably flush PlayerSettings. Serialize only
            // this settings object, preserving all other settings and avoiding SaveAssets().
            const string path = "ProjectSettings/ProjectSettings.asset";
            UnityEngine.Object[] objects = UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(path);
            try
            {
                var settings = new SerializedObject(objects[0]);
                settings.FindProperty("defaultScreenOrientation").intValue = (int)UIOrientation.AutoRotation;
                settings.FindProperty("allowedAutorotateToPortrait").boolValue = false;
                settings.FindProperty("allowedAutorotateToPortraitUpsideDown").boolValue = false;
                settings.FindProperty("allowedAutorotateToLandscapeLeft").boolValue = true;
                settings.FindProperty("allowedAutorotateToLandscapeRight").boolValue = true;
                if (settings.ApplyModifiedPropertiesWithoutUndo())
                    UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(objects, path, true);
            }
            finally { foreach (var item in objects) UnityEngine.Object.DestroyImmediate(item); }
        }

        public static void EnsureScene(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var input = roots.SelectMany(r => r.GetComponentsInChildren<CarInput>(true)).Single();
            var overlays = roots.SelectMany(r => r.GetComponentsInChildren<MobileControls>(true)).ToArray();
            if (overlays.Length > 1) throw new InvalidOperationException("Duplicate touch overlays exist; refusing to modify them.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null) CreatePrefab(scene);
            MobileControls overlay = overlays.SingleOrDefault();
            if (overlay == null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
                overlay = instance.GetComponent<MobileControls>();
                EditorSceneManager.MarkSceneDirty(scene);
            }
            if (overlay.Input != input)
            {
                overlay.Input = input;
                PrefabUtility.RecordPrefabInstancePropertyModifications(overlay);
                EditorSceneManager.MarkSceneDirty(scene);
            }
            var systems = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<EventSystem>(true)).ToArray();
            if (systems.Length > 1) throw new InvalidOperationException("Duplicate EventSystems exist.");
            if (systems.Length == 0)
            {
                var go = new GameObject("Mobile EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                SceneManager.MoveGameObjectToScene(go, scene);
                EventSystem events = go.GetComponent<EventSystem>();
                events.sendNavigationEvents = false;
                InputSystemUIInputModule module = go.GetComponent<InputSystemUIInputModule>();
                module.AssignDefaultActions();
                module.move = module.submit = module.cancel = null;
                module.pointerBehavior = UIPointerBehavior.AllPointersAsIs;
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        private static void CreatePrefab(Scene scene)
        {
            var root = new GameObject("MobileControls", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup),
                typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster), typeof(MobileControls));
            SceneManager.MoveGameObjectToScene(root, scene);
            try
            {
                root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                root.GetComponent<Canvas>().sortingOrder = 10;
                var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
                var safe = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaPanel));
                safe.transform.SetParent(root.transform, false);
                safe.GetComponent<SafeAreaPanel>().Apply();
                var owner = root.GetComponent<MobileControls>();
                owner.Controls = new PointerHoldButton[6];
                foreach (MobileControl kind in Enum.GetValues(typeof(MobileControl)))
                {
                    var button = new GameObject(kind.ToString(), typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(PointerHoldButton));
                    button.transform.SetParent(safe.transform, false);
                    var image = button.GetComponent<UnityEngine.UI.Image>();
                    image.color = kind == MobileControl.Accelerator ? new Color(0.07f, 0.45f, 0.2f) :
                        kind == MobileControl.Brake ? new Color(0.65f, 0.18f, 0.08f) : new Color(0.07f, 0.1f, 0.16f);
                    image.raycastTarget = true;
                    var hold = button.GetComponent<PointerHoldButton>();
                    hold.Owner = owner;
                    hold.Control = kind;
                    owner.Controls[(int)kind] = hold;
                    var icon = new GameObject("Icon", typeof(RectTransform), typeof(MobileControlIcon));
                    icon.transform.SetParent(button.transform, false);
                    var rect = icon.GetComponent<RectTransform>();
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                    var graphic = icon.GetComponent<MobileControlIcon>();
                    graphic.Control = kind;
                    graphic.color = Color.white;
                    graphic.raycastTarget = false;
                }
                // ExecuteAlways applies deterministic Inspector-configured presentation.
                owner.SendMessage("Update");
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [MenuItem("FormulaRacer/Milestone 3/Validate Mobile Controls")]
        public static void ValidateSaved()
        {
            Scene scene = SceneManager.GetSceneByPath(PrototypeBuilder.ScenePath);
            bool opened = !scene.IsValid();
            if (opened) scene = EditorSceneManager.OpenScene(PrototypeBuilder.ScenePath, OpenSceneMode.Additive);
            try { ValidateScene(scene); Debug.Log("FormulaRacer mobile scene validation PASSED."); }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        public static void ValidateScene(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var overlays = roots.SelectMany(r => r.GetComponentsInChildren<MobileControls>(true)).ToArray();
            var canvases = roots.SelectMany(r => r.GetComponentsInChildren<Canvas>(true)).ToArray();
            var systems = roots.SelectMany(r => r.GetComponentsInChildren<EventSystem>(true)).ToArray();
            if (overlays.Length != 1 || canvases.Length != 1 || systems.Length != 1)
                throw new InvalidOperationException("Expected exactly one MobileControls, Canvas, and EventSystem.");
            var ui = overlays[0];
            var module = systems[0].GetComponent<InputSystemUIInputModule>();
            if (ui.Input == null || ui.Input.gameObject.scene != scene || ui.Controls == null || ui.Controls.Length != 6 ||
                ui.Controls.Any(c => c == null || c.Owner != ui || c.GetComponent<UnityEngine.UI.Image>() == null) ||
                ui.Controls.Select(c => c.Control).Distinct().Count() != 6 ||
                ui.GetComponentsInChildren<PointerHoldButton>(true).Length != 6 ||
                ui.GetComponentsInChildren<SafeAreaPanel>(true).Length != 1 ||
                ui.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null || module == null ||
                module.point == null || module.leftClick == null || systems[0].sendNavigationEvents ||
                module.pointerBehavior != UIPointerBehavior.AllPointersAsIs)
                throw new InvalidOperationException("Touch controls, safe area, or Input System UI wiring is incomplete.");
        }

        [MenuItem("FormulaRacer/Milestone 3/Validate Repeated Setup")]
        public static void ValidateRepeatedSetup()
        {
            string buildOrder = string.Join("|", EditorBuildSettings.scenes.Select(s => s.path + ":" + s.enabled));
            Configure();
            byte[] scene = File.ReadAllBytes(PrototypeBuilder.ScenePath);
            byte[] prefab = File.ReadAllBytes(PrefabPath);
            Configure();
            Configure();
            if (!scene.SequenceEqual(File.ReadAllBytes(PrototypeBuilder.ScenePath)) || !prefab.SequenceEqual(File.ReadAllBytes(PrefabPath)))
                throw new InvalidOperationException("Repeated setup changed existing scene/prefab bytes.");
            if (buildOrder != string.Join("|", EditorBuildSettings.scenes.Select(s => s.path + ":" + s.enabled)))
                throw new InvalidOperationException("Global build scene order changed.");
            PrototypeBuilder.ValidateSaved();
            ValidateSaved();
            if (PlayerSettings.allowedAutorotateToPortrait || PlayerSettings.allowedAutorotateToPortraitUpsideDown ||
                !PlayerSettings.allowedAutorotateToLandscapeLeft || !PlayerSettings.allowedAutorotateToLandscapeRight)
                throw new InvalidOperationException("Landscape settings are incorrect.");
            Debug.Log("FormulaRacer repeated setup validation PASSED: three runs, identical scene/prefab bytes, one Canvas/EventSystem/six controls, physics/camera checks, landscape, unchanged global build order.");
        }

        [MenuItem("FormulaRacer/Milestone 3/Play TestTrack (Editor)")]
        public static void PlayTestTrack()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(PrototypeBuilder.ScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("FormulaRacer/Milestone 3/Build TestTrack Android APK")]
        public static void BuildAndroid()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Android build support is unavailable. No modules will be installed automatically.");
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string output = "Builds/Android/FormulaRacer-TestTrack.apk";
            Directory.CreateDirectory("Builds/Android");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { PrototypeBuilder.ScenePath },
                locationPathName = output, target = BuildTarget.Android, options = BuildOptions.Development });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException("Android TestTrack build failed: " + report.summary.result);
            Debug.Log("FormulaRacer TestTrack APK built at " + Path.GetFullPath(output) + "; global scene order unchanged.");
        }
    }
}
