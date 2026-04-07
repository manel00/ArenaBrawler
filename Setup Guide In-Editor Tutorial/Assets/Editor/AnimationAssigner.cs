#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace ArenaEnhanced.Editor
{
    public static class AnimationAssigner
    {
        [MenuItem("Tools/Assign DoubleL Animations")]
        public static void AssignAnimations()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                "Assets/Resources/Animations/PlayerMeleeAnimator.controller");
            
            if (controller == null)
            {
                EditorUtility.DisplayDialog("Error", "Animator Controller not found!", "OK");
                return;
            }

            // Get all states from the controller
            var stateMachine = controller.layers[0].stateMachine;
            
            // Load animation clips - DoubleL clips are typically named like "Take 001" in the FBX
            // We'll load the FBX files and get their clip names
            string[] fbxPaths = new string[]
            {
                "Assets/DoubleL/One Hand Up/Idle/1Hand_Up_Idle_1.fbx",
                "Assets/DoubleL/One Hand Up/Movement/Walk/1Hand_Up_Walk_F_Loop.fbx",
                "Assets/DoubleL/One Hand Up/Movement/Run/1Hand_Up_Run_F_Loop.fbx",
                "Assets/DoubleL/One Hand Up/Attack_A/InPlace/1Hand_Up_Attack_A_1_InPlace.fbx",
                "Assets/DoubleL/One Hand Up/Attack_A/InPlace/1Hand_Up_Attack_A_2_InPlace.fbx",
                "Assets/DoubleL/One Hand Up/Attack_A/InPlace/1Hand_Up_Attack_A_3_InPlace.fbx",
                "Assets/DoubleL/One Hand Up/Attack_B/InPlace/1Hand_Up_Attack_B_1_InPlace.fbx",
                "Assets/DoubleL/One Hand Up/Attack_B/InPlace/1Hand_Up_Attack_B_2_InPlace.fbx",
                "Assets/DoubleL/One Hand Up/Attack_B/InPlace/1Hand_Up_Attack_B_3_InPlace.fbx"
            };

            int assignedCount = 0;
            
            foreach (var state in stateMachine.states)
            {
                string stateName = state.state.name;
                AnimationClip clip = null;
                
                switch (stateName)
                {
                    case "Idle_WeaponDrawn":
                    case "Idle_Sheathed":
                        clip = LoadClipFromFBX(fbxPaths[0]);
                        break;
                    case "Move_WeaponDrawn":
                        // Create blend tree for movement
                        var blendTree = new BlendTree();
                        blendTree.name = "MovementBlend";
                        blendTree.blendParameter = "MoveSpeed";
                        blendTree.blendType = BlendTreeType.Simple1D;
                        blendTree.useAutomaticThresholds = true;
                        blendTree.AddChild(LoadClipFromFBX(fbxPaths[0]), 0f); // Idle at speed 0
                        blendTree.AddChild(LoadClipFromFBX(fbxPaths[1]), 0.5f); // Walk at speed 0.5
                        blendTree.AddChild(LoadClipFromFBX(fbxPaths[2]), 1f); // Run at speed 1
                        state.state.motion = blendTree;
                        assignedCount++;
                        continue;
                    case "Attack_A_1":
                        clip = LoadClipFromFBX(fbxPaths[3]);
                        break;
                    case "Attack_A_2":
                        clip = LoadClipFromFBX(fbxPaths[4]);
                        break;
                    case "Attack_A_3":
                        clip = LoadClipFromFBX(fbxPaths[5]);
                        break;
                    case "Attack_B_1":
                        clip = LoadClipFromFBX(fbxPaths[6]);
                        break;
                    case "Attack_B_2":
                        clip = LoadClipFromFBX(fbxPaths[7]);
                        break;
                    case "Attack_B_3":
                        clip = LoadClipFromFBX(fbxPaths[8]);
                        break;
                }
                
                if (clip != null)
                {
                    state.state.motion = clip;
                    assignedCount++;
                    Debug.Log($"[AnimationAssigner] Assigned {clip.name} to state {stateName}");
                }
            }
            
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Success", 
                $"Assigned animations to {assignedCount} states!\n\nAnimator Controller is ready to use.", 
                "OK");
        }
        
        private static AnimationClip LoadClipFromFBX(string path)
        {
            var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (obj == null)
            {
                Debug.LogWarning($"[AnimationAssigner] Could not load: {path}");
                return null;
            }
            
            // Get all objects from the FBX
            Object[] allObjects = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object o in allObjects)
            {
                if (o is AnimationClip clip)
                {
                    // Skip the '__preview' clip that Unity generates
                    if (!clip.name.Contains("__preview") && !clip.name.Contains("Avatar"))
                    {
                        return clip;
                    }
                }
            }
            
            return null;
        }
    }
}
#endif
