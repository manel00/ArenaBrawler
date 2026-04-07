using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class FixArmatureAnimator : EditorWindow
{
    [MenuItem("Arena/Fix Armature Animator (Regenerate)")]
    public static void FixAnimator()
    {
        string controllerPath = "Assets/Resources/Animations/ArmatureAnimator.controller";
        
        // Delete existing
        if (File.Exists(controllerPath))
        {
            AssetDatabase.DeleteAsset(controllerPath);
            AssetDatabase.Refresh();
            Debug.Log("[FixArmatureAnimator] Deleted old controller");
        }
        
        // Create directory
        string dir = Path.GetDirectoryName(controllerPath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        
        // Create new controller
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        
        // Add parameters
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Bool);
        controller.AddParameter("MotionSpeed", AnimatorControllerParameterType.Float);
        
        // Get state machine
        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;
        
        // Load clips
        AnimationClip idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/DoubleL/Demo/Anim/OneHand_Up_Idle.anim");
        AnimationClip walkF = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/DoubleL/Demo/Anim/OneHand_Up_Walk_F_InPlace.anim");
        
        // Create Idle state
        AnimatorState idleState = rootStateMachine.AddState("Idle", new Vector3(300, 100, 0));
        if (idleClip != null)
        {
            idleState.motion = idleClip;
            Debug.Log("[FixArmatureAnimator] Idle assigned: " + idleClip.name);
        }
        else
        {
            Debug.LogError("[FixArmatureAnimator] OneHand_Up_Idle.anim NOT FOUND!");
        }
        
        // Create Walk state
        AnimatorState walkState = rootStateMachine.AddState("Walk", new Vector3(300, 200, 0));
        if (walkF != null)
        {
            walkState.motion = walkF;
            Debug.Log("[FixArmatureAnimator] Walk assigned: " + walkF.name);
        }
        else
        {
            Debug.LogError("[FixArmatureAnimator] OneHand_Up_Walk_F_InPlace.anim NOT FOUND!");
        }
        
        // Transitions
        if (idleState != null && walkState != null)
        {
            // Idle -> Walk
            AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            idleToWalk.duration = 0.1f;
            idleToWalk.hasExitTime = false;
            
            // Walk -> Idle
            AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            walkToIdle.duration = 0.1f;
            walkToIdle.hasExitTime = false;
            
            Debug.Log("[FixArmatureAnimator] Transitions created");
        }
        
        // Set default
        rootStateMachine.defaultState = idleState;
        
        // Save
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        if (idleClip != null && walkF != null)
        {
            EditorUtility.DisplayDialog("Success", 
                "ArmatureAnimator.controller regenerated successfully!\n\n" +
                "Idle: " + idleClip.name + "\n" +
                "Walk: " + walkF.name, "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Error", 
                "Some animations were not found. Check the Console for details.", "OK");
        }
    }
}
