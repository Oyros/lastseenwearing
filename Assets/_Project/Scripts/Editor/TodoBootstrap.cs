using System;
using System.IO;
using Unity.Multiplayer.PlayMode;
using UnityEditor;
using UnityEngine;

namespace LastSeenWearing.Editor
{
    /// <summary>
    /// Creates the developer's personal <c>TODO.md</c> from <c>TODO.template.md</c> on first
    /// editor load, if it is missing.
    ///
    /// <c>TODO.md</c> is gitignored so that two developers' task lists never collide, which
    /// means a fresh clone does not have one. Doing this in an editor hook rather than a setup
    /// script means it works for anyone who opens the project — nobody has to remember a step.
    /// See docs/WORKFLOW.md §7.
    /// </summary>
    [InitializeOnLoad]
    internal static class TodoBootstrap
    {
        private const string TodoFileName = "TODO.md";
        private const string TemplateFileName = "TODO.template.md";

        static TodoBootstrap()
        {
            // A Multiplayer Play Mode clone is a copy under Library/VP with no TODO.template.md.
            if (!CurrentPlayer.IsMainEditor)
            {
                return;
            }

            // Application.dataPath is <project>/Assets — the files live one level up.
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                return;
            }

            var todoPath = Path.Combine(projectRoot, TodoFileName);
            if (File.Exists(todoPath))
            {
                return;
            }

            var templatePath = Path.Combine(projectRoot, TemplateFileName);
            if (!File.Exists(templatePath))
            {
                Debug.LogWarning(
                    $"[TodoBootstrap] {TemplateFileName} is missing from the project root, so " +
                    $"{TodoFileName} could not be created. It should be committed — see docs/WORKFLOW.md.");
                return;
            }

            try
            {
                File.Copy(templatePath, todoPath);
                Debug.Log(
                    $"[TodoBootstrap] Created your personal {TodoFileName} in the project root. " +
                    "It is gitignored and never shared — team progress goes in docs/STATUS.md.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TodoBootstrap] Could not create {TodoFileName}: {e.Message}");
            }
        }
    }
}
