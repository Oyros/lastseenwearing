using System.Linq;
using LastSeenWearing.Gameplay.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastSeenWearing.Tests.Project
{
    /// <summary>
    /// P0.06: the generated controls carry both maps and both control schemes, and every action
    /// can be reached from a device (docs/DATA.md §7, CONVENTIONS.md §6).
    /// </summary>
    public sealed class InputActionsTests
    {
        private LastSeenWearingControls _controls;

        [SetUp]
        public void SetUp() => _controls = new LastSeenWearingControls();

        // The wrapper's Dispose calls Destroy, which edit mode refuses.
        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_controls.asset);

        [TestCase("Field")]
        [TestCase("Watcher")]
        public void TheMapExists(string mapName)
        {
            Assert.That(_controls.asset.FindActionMap(mapName), Is.Not.Null, $"no '{mapName}' action map");
        }

        [TestCase("KeyboardMouse")]
        [TestCase("Gamepad")]
        public void TheControlSchemeExists(string schemeName)
        {
            Assert.That(_controls.asset.controlSchemes.Any(s => s.name == schemeName), Is.True,
                $"no '{schemeName}' control scheme");
        }

        [Test]
        public void EveryActionHasABinding()
        {
            var unbound = _controls.asset.actionMaps
                .SelectMany(m => m.actions)
                .Where(a => !a.bindings.Any(b => !b.isComposite))
                .Select(a => $"{a.actionMap.name}/{a.name}")
                .ToArray();
            Assert.That(unbound, Is.Empty, "actions with no binding");
        }

        [Test]
        public void EveryBindingBelongsToAControlScheme()
        {
            var orphans = _controls.asset.actionMaps
                .SelectMany(m => m.bindings)
                .Where(b => !b.isComposite && string.IsNullOrEmpty(b.groups))
                .Select(b => $"{b.action} {b.path}")
                .ToArray();
            Assert.That(orphans, Is.Empty, "bindings in no control scheme");
        }
    }
}
