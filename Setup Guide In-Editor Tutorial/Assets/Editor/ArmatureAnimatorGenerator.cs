using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class ArmatureAnimatorGenerator : EditorWindow
{
    [MenuItem("Arena/Generate Armature Animator")]
    public static void GenerateAnimator()
    {
        string controllerPath = "Assets/Resources/Animations/ArmatureAnimator.controller";
        
        // Delete existing if broken
        if (File.Exists(controllerPath))
        {
            AssetDatabase.DeleteAsset(controllerPath);
            AssetDatabase.Refresh();
        }
        
        // Create directory if needed
        string dir = Path.GetDirectoryName(controllerPath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        
        // Create controller
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        
        // Add parameters
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Bool);
        controller.AddParameter("FreeFall", AnimatorControllerParameterType.Bool);
        controller.AddParameter("MotionSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsWeaponDrawn", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("AttackType", AnimatorControllerParameterType.Int);
        
        // Get base layer
        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;
        
        // Load animation clips
        AnimationClip idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/DoubleL/Demo/Anim/OneHand_Up_Idle.anim");
        AnimationClip walkF = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/DoubleL/Demo/Anim/OneHand_Up_Walk_F_InPlace.anim");
        AnimationClip walkB = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/DoubleL/Demo/Anim/OneHand_Up_Walk_B_InPlace.anim");
        AnimationClip walkL = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/DoubleL/Demo/Anim/OneHand_Up_Walk_L_InPlace.anim");
        AnimationClip walkR = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/DoubleL/Demo/Anim/OneHand_Up_Walk_R_InPlace.anim");
        
        // Idle state
        AnimatorState idleState = rootStateMachine.AddState("Idle", new Vector3(300, 100, 0));
        if (idleClip != null)
        {
            idleState.motion = idleClip;
            Debug.Log("[Generator] Assigned Idle clip");
        }
        else
        {
            Debug.LogWarning("[Generator] Idle clip not found!");
        }
        
        // Walk state with simple animation (not blend tree for now)
        AnimatorState walkState = rootStateMachine.AddState("Walk", new Vector3(300, 200, 0));
        if (walkF != null)
        {
            walkState.motion = walkF;
            Debug.Log("[Generator] Assigned Walk clip");
        }
        else
        {
            Debug.LogWarning("[Generator] Walk clip not found!");
        }
        
        // Create transition: Idle -> Walk (when Speed > 0.1)
        AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        idleToWalk.duration = 0.25f;
        idleToWalk.hasExitTime = false;
        
        // Create transition: Walk -> Idle (when Speed < 0.1)
        AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        walkToIdle.duration = 0.25f;
        walkToIdle.hasExitTime = false;
        
        // Set default state
        rootStateMachine.defaultState = idleState;
        
        // Save assets
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("[ArmatureAnimatorGenerator] Generated successfully at " + controllerPath);
        EditorUtility.DisplayDialog("Success", "ArmatureAnimator.controller generated!\n\nLocation: " + controllerPath, "OK");
    }
}