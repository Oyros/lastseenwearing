using System.Linq;
using LastSeenWearing.Core.Config;
using LastSeenWearing.Editor.Import;
using LastSeenWearing.Gameplay.Player;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace LastSeenWearing.Tests.Art
{
    /// <summary>
    /// P1.12: the patrol moves and aims — a uniformed crowd-contract body, first-person arms on their own
    /// generic rig (PL.19), a sprint faster than the fugitive's (GDD §03), and an aim that finds the character
    /// under the crosshair.
    /// </summary>
    public sealed class PatrolTests
    {
        private const string PatrolPath = "Assets/_Project/Prefabs/Characters/Patrol.prefab";
        private const string ArmsPrefabPath = "Assets/_Project/Prefabs/Characters/FP_Arms.prefab";
        private const string NpcPath = "Assets/_Project/Prefabs/Characters/CrowdNpc.prefab";
        private const string FugitivePath = "Assets/_Project/Prefabs/Characters/Fugitive.prefab";
        private const string PatrolModel = "Assets/_Project/Art/Models/Patrol/LSW_Patrol_M.fbx";

        [Test]
        public void ThePatrolIsAnOwnerMovedUniformedBody()
        {
            var patrol = AssetDatabase.LoadAssetAtPath<GameObject>(PatrolPath);
            Assert.That(patrol.GetComponent<NetworkObject>(), Is.Not.Null);
            Assert.That(patrol.GetComponent<NetworkTransform>().AuthorityMode, Is.EqualTo(NetworkTransform.AuthorityModes.Owner));
            Assert.That(patrol.GetComponent<CharacterController>(), Is.Not.Null);

            var serialized = new SerializedObject(patrol.GetComponent<PatrolController>());
            foreach (var field in new[] { "_movement", "_camera", "_crowd", "_fpArmsPrefab" })
            {
                Assert.That(serialized.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            }

            var animator = patrol.GetComponentInChildren<Animator>();
            var npcAnimator = AssetDatabase.LoadAssetAtPath<GameObject>(NpcPath).GetComponentInChildren<Animator>();
            Assert.That(animator.avatar.isHuman, Is.True, "the uniform is on the crowd contract");
            Assert.That(animator.runtimeAnimatorController, Is.SameAs(npcAnimator.runtimeAnimatorController), "it walks on the crowd's animator");
            Assert.That(AssetDatabase.GetAssetPath(animator.avatar), Is.EqualTo(PatrolModel));

            var list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>("Assets/DefaultNetworkPrefabs.asset");
            Assert.That(list.PrefabList.Any(p => p.Prefab == patrol), Is.True, "registered network prefab");
        }

        [Test]
        public void TheArmsAreAGenericRigWithTheirOwnClips()
        {
            Assert.That(HumanoidImportPostprocessor.IsRigged(FpArmsAnimatorBuilder.ArmsPath), Is.True);
            Assert.That(HumanoidImportPostprocessor.Applies(FpArmsAnimatorBuilder.ArmsPath), Is.False, "no Hips: not humanoid");
            Assert.That(HumanoidImportPostprocessor.Applies(PatrolModel), Is.True);

            var importer = (ModelImporter)AssetImporter.GetAtPath(FpArmsAnimatorBuilder.ArmsPath);
            Assert.That(importer.animationType, Is.EqualTo(ModelImporterAnimationType.Generic));

            var clips = AssetDatabase.LoadAllAssetsAtPath(FpArmsAnimatorBuilder.ArmsPath).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview")).ToDictionary(c => c.name);
            Assert.That(clips.Keys, Is.SupersetOf(new[] { "FP_Idle", "FP_Walk", "FP_Stop", "FP_Cuffs" }));
            Assert.That(AnimationUtility.GetAnimationClipSettings(clips["FP_Walk"]).loopTime, Is.True);
            Assert.That(AnimationUtility.GetAnimationClipSettings(clips["FP_Stop"]).loopTime, Is.False, "gestures play once");

            var arms = AssetDatabase.LoadAssetAtPath<GameObject>(ArmsPrefabPath).GetComponent<Animator>();
            Assert.That(AssetDatabase.GetAssetPath(arms.runtimeAnimatorController), Is.EqualTo(FpArmsAnimatorBuilder.ControllerPath));
            Assert.That(arms.avatar.isHuman, Is.False);
        }

        [Test]
        public void TheArmsSitWithTheirEyeOnTheCamera()
        {
            var eye = new GameObject("Eye");
            try
            {
                eye.transform.SetPositionAndRotation(new Vector3(3f, 1.68f, -2f), Quaternion.Euler(10f, 40f, 0f));
                var arms = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ArmsPrefabPath), eye.transform, false);
                PatrolController.SeatArmsAtTheEye(arms.transform);
                var bone = arms.GetComponentsInChildren<Transform>().Single(t => t.name == "Camera");
                Assert.That(Vector3.Distance(bone.position, eye.transform.position), Is.LessThan(1e-4f));
                foreach (var hand in arms.GetComponentsInChildren<Renderer>().Where(r => r.name.Contains("Hand")))
                {
                    Assert.That(eye.transform.InverseTransformPoint(hand.bounds.center).z, Is.GreaterThan(0.1f), "hands in front of the eye");
                }
            }
            finally
            {
                Object.DestroyImmediate(eye);
            }
        }

        [Test]
        public void ThePatrolOutrunsTheFugitive()
        {
            var movement = AssetDatabase.LoadAssetAtPath<MovementConfig>("Assets/_Project/Data/Config/MovementConfig.asset");
            Assert.That(movement.PatrolRunSpeed, Is.GreaterThan(movement.FugitiveRunSpeed), "GDD §03: the patrol is fast");
            Assert.That(movement.PatrolWalkSpeed, Is.LessThan(movement.PatrolRunSpeed));
        }

        [Test]
        public void EveryCharacterCanBeAimedAt()
        {
            foreach (var path in new[] { PatrolPath, NpcPath, FugitivePath })
            {
                var hitbox = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentInChildren<CharacterHitbox>();
                Assert.That(hitbox, Is.Not.Null, path);
                Assert.That(hitbox.gameObject.layer, Is.EqualTo(LayerMask.NameToLayer(CharacterHitbox.Layer)), path);
                Assert.That(hitbox.GetComponent<Collider>().isTrigger, Is.True, "aim only, no shove: " + path);
            }
        }

        [Test]
        public void TheAimFindsTheFirstCharacterAndNotThroughWalls()
        {
            var crowd = new GameObject("Crowd"); // NPCs live under a parent: the aim must name the NPC, not it
            var self = Character("Self", Vector3.zero);
            var near = Character("Near", new Vector3(0f, 0f, 5f));
            near.transform.SetParent(crowd.transform, true);
            var far = Character("Far", new Vector3(0f, 0f, 10f));
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var eye = new Vector3(0f, 1.5f, 0f);
                wall.transform.position = new Vector3(0f, 1.5f, 7.5f);
                Physics.SyncTransforms();
                Assert.That(AimProbe.Find(eye, Vector3.forward, 30f, self)?.Character, Is.SameAs(near), "the nearest, not ourselves");

                near.SetActive(false);
                Physics.SyncTransforms();
                Assert.That(AimProbe.Find(eye, Vector3.forward, 30f, self), Is.Null, "the wall hides the far one");

                wall.SetActive(false);
                Physics.SyncTransforms();
                Assert.That(AimProbe.Find(eye, Vector3.forward, 30f, self)?.Character, Is.SameAs(far));
                Assert.That(AimProbe.Find(eye, Vector3.forward, 8f, self), Is.Null, "out of range");
            }
            finally
            {
                Object.DestroyImmediate(self);
                Object.DestroyImmediate(near);
                Object.DestroyImmediate(far);
                Object.DestroyImmediate(wall);
                Object.DestroyImmediate(crowd);
            }
        }

        private static GameObject Character(string name, Vector3 position)
        {
            var root = new GameObject(name);
            root.transform.position = position;
            var hitbox = new GameObject("Hitbox", typeof(CapsuleCollider), typeof(CharacterHitbox));
            hitbox.layer = LayerMask.NameToLayer(CharacterHitbox.Layer);
            hitbox.transform.SetParent(root.transform, false);
            var capsule = hitbox.GetComponent<CapsuleCollider>();
            capsule.isTrigger = true;
            capsule.center = new Vector3(0f, 0.9f, 0f);
            capsule.height = 1.8f;
            capsule.radius = 0.3f;
            return root;
        }
    }
}
