using System.IO;
using LastSeenWearing.Gameplay.Crowd;
using LastSeenWearing.UI.Watcher;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

namespace LastSeenWearing.Editor.Tools
{
    /// <summary>
    /// Records the walk readability test (docs/WALKTEST.md, P1.07) from <c>Sandbox_WalkTest</c> in play mode:
    /// the same <see cref="Seconds"/> of the crowd three times — near and far through the CCTV look, then
    /// the raw answer key — as MP4s in <c>docs/walktest/</c>. Game time is locked to the video's frame
    /// rate, so the videos play at real speed however slowly the editor renders.
    /// </summary>
    public static class WalkTestRecorder
    {
        public const float Seconds = 12f;
        private const float FrameRate = 30f;
        private const string Folder = "docs/walktest";

        private static readonly (WalkTestView.Take Take, string File)[] Takes =
        {
            (WalkTestView.Take.Near, "LSW_WalkTest_near"),
            (WalkTestView.Take.Far, "LSW_WalkTest_far"),
            (WalkTestView.Take.Key, "LSW_WalkTest_key"),
        };

        private static RecorderController _controller;
        private static int _take = -1;
        private static float _takeStarted;

        public static bool IsRecording => _take >= 0;

        [MenuItem("Last Seen Wearing/Art/Record Walk Test (in play mode)")]
        public static void RecordAll()
        {
            if (!EditorApplication.isPlaying || Object.FindFirstObjectByType<WalkTestView>() == null)
            {
                Debug.LogError("[WalkTestRecorder] Open Sandbox_WalkTest and enter play mode first.");
                return;
            }

            if (IsRecording)
            {
                return;
            }

            Directory.CreateDirectory(Path.Combine(Directory.GetParent(Application.dataPath).FullName, Folder));
            _take = 0;
            BeginTake();
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying)
            {
                Finish("play mode ended");
                return;
            }

            if (Time.time - _takeStarted < Seconds)
            {
                return;
            }

            _controller.StopRecording();
            Debug.Log($"[WalkTestRecorder] Wrote {Folder}/{Takes[_take].File}.mp4");
            _take++;
            if (_take >= Takes.Length)
            {
                Finish(null);
                return;
            }

            BeginTake();
        }

        private static void BeginTake()
        {
            var view = Object.FindFirstObjectByType<WalkTestView>();
            view.Show(Takes[_take].Take);
            Object.FindFirstObjectByType<WalkTestCrowd>().Restart();

            var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movie.name = Takes[_take].File;
            movie.Enabled = true;
            movie.EncoderSettings = new CoreEncoderSettings
            {
                Codec = CoreEncoderSettings.OutputCodec.MP4,
                EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
            };
            movie.CaptureAudio = false;
            movie.ImageInputSettings = new RenderTextureInputSettings
            {
                RenderTexture = view.Output,
                OutputWidth = view.Output.width,
                OutputHeight = view.Output.height,
            };
            movie.OutputFile = Path.Combine(Directory.GetParent(Application.dataPath).FullName, Folder, Takes[_take].File);

            var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            settings.AddRecorderSettings(movie);
            settings.SetRecordModeToManual();
            settings.FrameRatePlayback = FrameRatePlayback.Constant;
            settings.FrameRate = FrameRate;
            settings.CapFrameRate = true;

            _controller = new RecorderController(settings);
            _controller.PrepareRecording();
            _controller.StartRecording();
            _takeStarted = Time.time;
        }

        private static void Finish(string problem)
        {
            EditorApplication.update -= Tick;
            if (_controller != null && _controller.IsRecording())
            {
                _controller.StopRecording();
            }

            _controller = null;
            _take = -1;
            if (problem != null)
            {
                Debug.LogError($"[WalkTestRecorder] Stopped: {problem}.");
            }
            else
            {
                Debug.Log("[WalkTestRecorder] All three takes recorded.");
            }
        }
    }
}
