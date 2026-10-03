using System;
using System.IO;
using LastSeenWearing.Core.Crowd;
using LastSeenWearing.Gameplay.Crowd;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastSeenWearing.Editor.Tools
{
    /// <summary>
    /// Renders the walk-trait lineup the CCTV readability gate asks for (docs/LOOKDEV.md §2, P1.05):
    /// eight crowd bodies side by side — no trait, then each trait (and each side of a two-sided one)
    /// at <see cref="Weight"/> — walking on the spot, seen from a camera 6 m up and 35° down, greyscale,
    /// 320×180, at four points of the cycle. Writes the frames stacked into <c>docs/lookdev/</c>, plus a
    /// ×3 copy for review. Rendered in a preview scene: the open scenes are never touched.
    /// </summary>
    public static class WalkLineup
    {
        public const float Weight = 0.5f;
        public const string OutputPath = "docs/lookdev/walk_lineup_cctv.png";
        public const string ReviewPath = "docs/lookdev/walk_lineup_cctv_x3.png";

        private const string NpcPrefabPath = "Assets/_Project/Prefabs/Characters/CrowdNpc.prefab";
        private const int Width = 320;
        private const int Height = 180;
        private const int ReviewScale = 3;
        private const float Spacing = 1.1f;
        private const float CameraHeight = 6f;
        private const float CameraPitch = 35f;
        private const float EvaluateStep = 1f / 60f;
        private const float SunIntensity = 2f;
        private static readonly float[] Phases = { 0f, 0.25f, 0.5f, 0.75f };

        // Column order, left to right: label and how to set it.
        private static readonly (string Label, WalkTrait? Trait, float Strength)[] Columns =
        {
            ("none", null, 0f),
            ("limp L", WalkTrait.Limp, -Weight),
            ("limp R", WalkTrait.Limp, Weight),
            ("hunch", WalkTrait.Hunch, Weight),
            ("sway", WalkTrait.Sway, Weight),
            ("bounce", WalkTrait.Bounce, Weight),
            ("stiff arms", WalkTrait.ArmSwing, -Weight),
            ("big arms", WalkTrait.ArmSwing, Weight),
        };

        [MenuItem("Last Seen Wearing/Art/Render Walk Lineup")]
        public static void RenderFromMenu()
        {
            Render();
            Debug.Log($"[WalkLineup] Wrote {OutputPath} and {ReviewPath}. Columns: {string.Join(", ", Array.ConvertAll(Columns, c => c.Label))}.");
        }

        public static void Render()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var target = new RenderTexture(Width, Height, 24);
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NpcPrefabPath);
                var animators = new Animator[Columns.Length];
                for (var i = 0; i < Columns.Length; i++)
                {
                    var npc = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    npc.transform.position = new Vector3((i - (Columns.Length - 1) / 2f) * Spacing, 0f, 0f);
                    var animator = npc.GetComponentInChildren<Animator>();
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    // Outside the player loop nothing re-skins between manual renders; without this every
                    // frame of the sheet shows the first pose.
                    foreach (var skin in npc.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        skin.forceMatrixRecalculationPerRender = true;
                    }

                    animator.Rebind();
                    var column = Columns[i];
                    if (column.Trait is WalkTrait trait)
                    {
                        animator.SetLayerWeight(animator.GetLayerIndex(trait.ToString()), Mathf.Abs(column.Strength));
                        if (trait == WalkTrait.Limp || trait == WalkTrait.ArmSwing)
                        {
                            animator.SetFloat(CrowdAgent.SideParameter(trait), Mathf.Sign(column.Strength));
                        }
                    }

                    animators[i] = animator;
                }

                var light = new GameObject("Sun", typeof(Light)).GetComponent<Light>();
                SceneManager_Move(light.gameObject, scene);
                light.type = LightType.Directional;
                light.intensity = SunIntensity;
                // From behind the camera, so the faces and fronts the camera sees are lit.
                light.transform.rotation = Quaternion.Euler(50f, 160f, 0f);

                var camera = new GameObject("CCTV", typeof(Camera)).GetComponent<Camera>();
                SceneManager_Move(camera.gameObject, scene);
                camera.scene = scene;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.45f, 0.45f, 0.45f);
                camera.fieldOfView = 40f;
                camera.targetTexture = target;
                // The bodies walk toward the camera (+Z faces it from −Z): 6 m up, 35° down, framed on the row.
                var distance = CameraHeight / Mathf.Tan(CameraPitch * Mathf.Deg2Rad);
                camera.transform.position = new Vector3(0f, CameraHeight, distance);
                camera.transform.rotation = Quaternion.Euler(CameraPitch, 180f, 0f);

                var sheet = new Texture2D(Width, Height * Phases.Length, TextureFormat.RGB24, false);
                for (var f = 0; f < Phases.Length; f++)
                {
                    foreach (var animator in animators)
                    {
                        animator.SetFloat(CrowdAgent.PhaseParameter, Phases[f]);
                        // A zero step does not re-evaluate a time-parameter state; any positive step does,
                        // and the pose depends on Phase alone.
                        animator.Update(EvaluateStep);
                    }

                    camera.Render();
                    var frame = Grey(target);
                    sheet.SetPixels(0, Height * (Phases.Length - 1 - f), Width, Height, frame.GetPixels());
                    Object.DestroyImmediate(frame);
                }

                sheet.Apply();
                Write(OutputPath, sheet);
                Write(ReviewPath, Upscale(sheet, ReviewScale));
                Object.DestroyImmediate(sheet);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                target.Release();
                Object.DestroyImmediate(target);
            }
        }

        private static void SceneManager_Move(GameObject go, UnityEngine.SceneManagement.Scene scene)
        {
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
        }

        private static Texture2D Grey(RenderTexture source)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = source;
            var texture = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            RenderTexture.active = previous;
            var pixels = texture.GetPixels();
            for (var i = 0; i < pixels.Length; i++)
            {
                var y = pixels[i].grayscale;
                pixels[i] = new Color(y, y, y);
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D Upscale(Texture2D source, int scale)
        {
            var result = new Texture2D(source.width * scale, source.height * scale, TextureFormat.RGB24, false);
            for (var y = 0; y < result.height; y++)
            {
                for (var x = 0; x < result.width; x++)
                {
                    result.SetPixel(x, y, source.GetPixel(x / scale, y / scale));
                }
            }

            result.Apply();
            return result;
        }

        private static void Write(string relativePath, Texture2D texture)
        {
            var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }
    }
}
