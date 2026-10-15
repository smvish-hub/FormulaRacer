using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace FormulaRacer.Editor
{
    public static class PrototypeBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/TestTrack.unity";
        private const string Root = "Assets/_Game";
        private const string PrefabPath = Root + "/Prefabs/FormulaCar.prefab";

        [MenuItem("FormulaRacer/Milestone 1/Rebuild Test Track")]
        public static void Rebuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before rebuilding.");
            Scene existing = SceneManager.GetSceneByPath(ScenePath);
            // Never silently discard edits, including edits to the generated scene.
            if (existing.IsValid() && existing.isDirty)
                throw new InvalidOperationException("Save or close the modified TestTrack scene before rebuilding.");

            foreach (string folder in new[] { "Scripts", "Editor", "Scenes", "Prefabs", "Materials" })
            {
                if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder("Assets", "_Game");
                if (!AssetDatabase.IsValidFolder(Root + "/" + folder)) AssetDatabase.CreateFolder(Root, folder);
            }
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            // Create the replacement first: Unity cannot close its last loaded scene.
            if (existing.IsValid()) EditorSceneManager.CloseScene(existing, true);
            SceneManager.SetActiveScene(scene);
            try
            {
                Material asphalt = Material("Asphalt", new Color(0.12f, 0.14f, 0.17f));
                Material grass = Material("Grass", new Color(0.18f, 0.35f, 0.17f));
                Material white = Material("White", new Color(0.88f, 0.9f, 0.92f));
                Material red = Material("CarRed", new Color(0.8f, 0.035f, 0.05f));
                Material black = Material("Rubber", new Color(0.025f, 0.025f, 0.03f));
                PhysicsMaterial contact = ContactMaterial();
                GameObject track = new GameObject("Test Circuit");
                Primitive("Ground", PrimitiveType.Cube, track.transform,
                    new Vector3(0f, -0.5f, 0f), new Vector3(116f, 1f, 86f), grass, true, contact);
                // Four road strips form an unbroken rectangular ring on a single ground collider.
                Primitive("South Straight", PrimitiveType.Cube, track.transform, new Vector3(0f, 0.015f, -28.5f), new Vector3(104f, 0.02f, 17f), asphalt);
                Primitive("North Straight", PrimitiveType.Cube, track.transform, new Vector3(0f, 0.015f, 28.5f), new Vector3(104f, 0.02f, 17f), asphalt);
                Primitive("West Straight", PrimitiveType.Cube, track.transform, new Vector3(-43.5f, 0.015f, 0f), new Vector3(17f, 0.02f, 40f), asphalt);
                Primitive("East Straight", PrimitiveType.Cube, track.transform, new Vector3(43.5f, 0.015f, 0f), new Vector3(17f, 0.02f, 40f), asphalt);
                Barrier("Outer West", track.transform, new Vector3(-52.5f, 0.6f, 0f), new Vector3(1f, 1.2f, 76f), white, contact);
                Barrier("Outer East", track.transform, new Vector3(52.5f, 0.6f, 0f), new Vector3(1f, 1.2f, 76f), white, contact);
                Barrier("Outer South", track.transform, new Vector3(0f, 0.6f, -37.5f), new Vector3(104f, 1.2f, 1f), white, contact);
                Barrier("Outer North", track.transform, new Vector3(0f, 0.6f, 37.5f), new Vector3(104f, 1.2f, 1f), white, contact);
                Barrier("Inner West", track.transform, new Vector3(-35f, 0.6f, 0f), new Vector3(1f, 1.2f, 41f), red, contact);
                Barrier("Inner East", track.transform, new Vector3(35f, 0.6f, 0f), new Vector3(1f, 1.2f, 41f), red, contact);
                Barrier("Inner South", track.transform, new Vector3(0f, 0.6f, -20f), new Vector3(70f, 1.2f, 1f), red, contact);
                Barrier("Inner North", track.transform, new Vector3(0f, 0.6f, 20f), new Vector3(70f, 1.2f, 1f), red, contact);
                for (int i = 0; i < 16; i++)
                    Primitive("Start Stripe " + i, PrimitiveType.Cube, track.transform,
                        new Vector3(-28f + (i % 2) * 0.6f, 0.035f, -35.5f + (i / 2) * 2f),
                        new Vector3(0.6f, 0.02f, 2f), (i + i / 2) % 2 == 0 ? white : black);

                GameObject source = BuildCar(red, black, white, contact);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, PrefabPath);
                UnityEngine.Object.DestroyImmediate(source);
                GameObject car = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                car.name = "Player Formula Car";
                car.transform.SetPositionAndRotation(new Vector3(-35f, 0.55f, -28.5f), Quaternion.Euler(0f, 90f, 0f));

                GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(ChaseCamera));
                cameraObject.tag = "MainCamera";
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.fieldOfView = 65f;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 300f;
                cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
                cameraObject.GetComponent<ChaseCamera>().Target = car.transform;
                cameraObject.GetComponent<ChaseCamera>().CockpitAnchor = car.transform.Find("CockpitAnchor");
                cameraObject.transform.position = car.transform.TransformPoint(new Vector3(0f, 4f, -7f));
                cameraObject.transform.LookAt(car.transform.position + Vector3.up * 0.7f);

                GameObject sunObject = new GameObject("Sun", typeof(Light), typeof(UniversalAdditionalLightData));
                Light sun = sunObject.GetComponent<Light>();
                sun.type = LightType.Directional;
                sun.intensity = 1.5f;
                sun.shadows = LightShadows.Soft;
                sunObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                RenderSettings.sun = sun;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.55f, 0.6f, 0.7f);
                RenderSettings.skybox = null;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.35f, 0.58f, 0.8f);

                ValidateScene(scene);
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save TestTrack.");
                // Append only. Preserve order, enabled states, and existing scene entries.
                if (!EditorBuildSettings.scenes.Any(s => s.path == ScenePath))
                    EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
                // Do not flush unrelated dirty assets or package-owned project settings.
                foreach (string assetPath in AssetDatabase.FindAssets("", new[] { Root + "/Materials" })
                    .Select(AssetDatabase.GUIDToAssetPath))
                    AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(assetPath));
                Debug.Log("FormulaRacer: generated and validated " + ScenePath);
            }
            finally
            {
                // Preserve the user's previously active scene and its unsaved state.
                if (previous.IsValid() && previous.isLoaded)
                {
                    SceneManager.SetActiveScene(previous);
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        [MenuItem("FormulaRacer/Milestone 1/Open Test Track")]
        public static void Open()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        // Entry point for a fresh-process batch validation of the saved scene.
        [MenuItem("FormulaRacer/Milestone 1/Validate Saved Test Track")]
        public static void ValidateSaved()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.IsValid();
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try { ValidateScene(scene); Debug.Log("FormulaRacer: saved scene validation passed."); }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        private static void ValidateScene(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var bodies = roots.SelectMany(r => r.GetComponentsInChildren<Rigidbody>(true)).ToArray();
            var cameras = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).ToArray();
            var listeners = roots.SelectMany(r => r.GetComponentsInChildren<AudioListener>(true)).ToArray();
            if (bodies.Length != 1 || cameras.Length != 1 || listeners.Length != 1)
                throw new InvalidOperationException("Expected exactly one Rigidbody, Camera and AudioListener.");
            Rigidbody body = bodies[0];
            if (body.isKinematic || !body.useGravity || body.GetComponent<CarInput>() == null ||
                body.GetComponent<ArcadeCarController>() == null || body.GetComponent<BoxCollider>() == null ||
                body.interpolation != RigidbodyInterpolation.Interpolate ||
                body.collisionDetectionMode != CollisionDetectionMode.ContinuousDynamic ||
                (body.constraints & RigidbodyConstraints.FreezeRotationX) == 0 ||
                (body.constraints & RigidbodyConstraints.FreezeRotationZ) == 0)
                throw new InvalidOperationException("Car physics/input configuration is incomplete.");
            ChaseCamera chase = cameras[0].GetComponent<ChaseCamera>();
            if (chase == null || chase.Target != body.transform)
                throw new InvalidOperationException("Chase camera target is missing or incorrect.");
            Transform cockpitAnchor = body.transform.Find("CockpitAnchor");
            if (cockpitAnchor == null || chase.CockpitAnchor != cockpitAnchor ||
                body.GetComponentsInChildren<Transform>(true).Count(t => t.name == "CockpitAnchor") != 1 ||
                cockpitAnchor.GetComponentsInChildren<Camera>(true).Length != 0 ||
                cockpitAnchor.GetComponentsInChildren<AudioListener>(true).Length != 0)
                throw new InvalidOperationException("Expected one cockpit anchor and a reference from the existing camera, with no duplicate cameras/listeners.");
            Transform visual = body.transform.Find("Visual");
            if (visual == null || visual.GetComponentsInChildren<Collider>().Length != 0)
                throw new InvalidOperationException("Visual geometry must be separate from physics.");
            var colliders = roots.SelectMany(r => r.GetComponentsInChildren<Collider>()).ToArray();
            if (colliders.Length != 10 || !colliders.Any(c => c.name == "Ground") ||
                colliders.Any(c => c.isTrigger || c.sharedMaterial == null || c.sharedMaterial.bounciness != 0f))
                throw new InvalidOperationException("Expected ground, eight solid barriers, and one car collider with zero bounce.");
            Physics.SyncTransforms();
            if (!Physics.Raycast(body.position + Vector3.up * 0.1f, Vector3.down, 0.72f, 1, QueryTriggerInteraction.Ignore))
                throw new InvalidOperationException("No ground beneath the start position.");
            foreach (Collider obstacle in colliders.Where(c => c.attachedRigidbody == null))
                if (Physics.ComputePenetration(body.GetComponent<BoxCollider>(), body.position, body.rotation,
                    obstacle, obstacle.transform.position, obstacle.transform.rotation, out _, out _))
                    throw new InvalidOperationException("Start position overlaps " + obstacle.name);
            Debug.Log("FormulaRacer validation: one car, 10 solid colliders, ground under spawn, no spawn overlap, one camera/listener, assigned target.");
        }

        private static GameObject BuildCar(Material red, Material black, Material white, PhysicsMaterial contact)
        {
            GameObject car = new GameObject("FormulaCar");
            car.layer = 2; // Ignore Raycast: car must not ground itself.
            Rigidbody body = car.AddComponent<Rigidbody>();
            body.mass = 650f;
            body.linearDamping = 0.05f;
            body.angularDamping = 4f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.centerOfMass = new Vector3(0f, -0.28f, 0f);
            BoxCollider collider = car.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, -0.15f, 0f);
            collider.size = new Vector3(1.85f, 0.6f, 4.1f);
            collider.sharedMaterial = contact;
            car.AddComponent<CarInput>();
            car.AddComponent<ArcadeCarController>();
            Milestone2Setup.EnsureAnchor(car.transform);
            Transform visual = new GameObject("Visual").transform;
            visual.SetParent(car.transform, false);
            Primitive("Chassis", PrimitiveType.Cube, visual, Vector3.zero, new Vector3(1.05f, 0.4f, 2.6f), red);
            Primitive("Nose", PrimitiveType.Cube, visual, new Vector3(0f, -0.08f, 1.45f), new Vector3(0.5f, 0.24f, 1.1f), red);
            Primitive("Cockpit", PrimitiveType.Sphere, visual, new Vector3(0f, 0.23f, -0.25f), new Vector3(0.6f, 0.35f, 0.8f), black);
            Primitive("Front Wing", PrimitiveType.Cube, visual, new Vector3(0f, -0.24f, 1.8f), new Vector3(1.8f, 0.12f, 0.45f), white);
            Primitive("Rear Wing", PrimitiveType.Cube, visual, new Vector3(0f, 0.48f, -1.6f), new Vector3(1.8f, 0.12f, 0.5f), white);
            foreach (float x in new[] { -0.78f, 0.78f })
                foreach (float z in new[] { -1.2f, 1.2f })
                {
                    GameObject wheel = Primitive("Wheel", PrimitiveType.Cylinder, visual, new Vector3(x, -0.1f, z), new Vector3(0.7f, 0.17f, 0.7f), black);
                    wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }
            foreach (Transform child in car.GetComponentsInChildren<Transform>()) child.gameObject.layer = 2;
            return car;
        }

        private static Material Material(string name, Color color)
        {
            string path = Root + "/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader unavailable.");
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.25f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static PhysicsMaterial ContactMaterial()
        {
            string path = Root + "/Materials/PrototypeContact.physicMaterial";
            PhysicsMaterial material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (material == null) { material = new PhysicsMaterial("PrototypeContact"); AssetDatabase.CreateAsset(material, path); }
            material.dynamicFriction = 0.15f;
            material.staticFriction = 0.15f;
            material.bounciness = 0f;
            material.frictionCombine = PhysicsMaterialCombine.Minimum;
            material.bounceCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void Barrier(string name, Transform parent, Vector3 position, Vector3 scale, Material material, PhysicsMaterial contact)
        {
            GameObject wall = Primitive(name, PrimitiveType.Cube, parent, position, scale, material, true, contact);
            wall.layer = 2; // Ground probes must not treat barrier tops as track.
        }

        private static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 position,
            Vector3 scale, Material material, bool solid = false, PhysicsMaterial contact = null)
        {
            GameObject item = GameObject.CreatePrimitive(type);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = scale;
            item.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = item.GetComponent<Collider>();
            if (solid) collider.sharedMaterial = contact;
            else UnityEngine.Object.DestroyImmediate(collider);
            return item;
        }
    }
}
