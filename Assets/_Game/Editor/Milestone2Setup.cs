using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FormulaRacer.Editor
{
    public static class Milestone2Setup
    {
        [MenuItem("FormulaRacer/Milestone 2/Toggle Camera Preview (Play Mode)")]
        private static void TogglePreview()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode to preview cameras.");
            ChaseCamera follow = UnityEngine.Object.FindFirstObjectByType<ChaseCamera>();
            if (follow == null || !follow.SetCockpitView(!follow.IsCockpitView))
                throw new InvalidOperationException("Camera preview requires a configured car and cockpit anchor.");
        }

        public static Transform EnsureAnchor(Transform car)
        {
            Transform anchor = car.Find("CockpitAnchor");
            if (anchor != null) return anchor; // Preserve subsequent Inspector tuning.
            anchor = new GameObject("CockpitAnchor").transform;
            anchor.SetParent(car, false);
            anchor.localPosition = new Vector3(0f, 0.62f, -0.3f);
            anchor.localRotation = Quaternion.identity;
            anchor.gameObject.layer = car.gameObject.layer;
            return anchor;
        }

        [MenuItem("FormulaRacer/Milestone 2/Configure Cockpit Camera")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before configuring cameras.");
            Scene scene = SceneManager.GetSceneByPath(PrototypeBuilder.ScenePath);
            bool opened = !scene.IsValid();
            if (!opened && scene.isDirty)
                throw new InvalidOperationException("Save or close your modified TestTrack first; setup will not save unrelated edits.");
            if (opened) scene = EditorSceneManager.OpenScene(PrototypeBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var cars = roots.SelectMany(r => r.GetComponentsInChildren<ArcadeCarController>(true)).ToArray();
                var cameras = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).ToArray();
                var listeners = roots.SelectMany(r => r.GetComponentsInChildren<AudioListener>(true)).ToArray();
                if (cars.Length != 1 || cameras.Length != 1 || listeners.Length != 1)
                    throw new InvalidOperationException("Expected the existing single car, camera, and listener.");
                ChaseCamera follow = cameras[0].GetComponent<ChaseCamera>();
                if (follow == null || follow.Target != cars[0].transform)
                    throw new InvalidOperationException("Existing chase camera wiring is invalid.");
                const string path = "Assets/_Game/Prefabs/FormulaCar.prefab";
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (contents.transform.Find("CockpitAnchor") == null)
                    {
                        EnsureAnchor(contents.transform);
                        PrefabUtility.SaveAsPrefabAsset(contents, path);
                    }
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
                // The existing scene car inherits the new anchor from its prefab.
                Transform anchor = cars[0].transform.Find("CockpitAnchor");
                if (anchor == null) throw new InvalidOperationException("Scene car did not inherit CockpitAnchor from FormulaCar.");
                if (follow.CockpitAnchor != anchor)
                {
                    follow.CockpitAnchor = anchor;
                    EditorUtility.SetDirty(follow);
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Unable to save camera reference.");
                }
                Debug.Log("FormulaRacer Milestone 2: cockpit configured; one camera/listener; existing car and chase settings preserved.");
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
