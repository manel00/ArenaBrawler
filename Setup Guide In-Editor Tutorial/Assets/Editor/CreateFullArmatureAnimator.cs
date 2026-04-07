using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class CreateFullArmatureAnimator : EditorWindow
{
    [MenuItem("Arena/Create Full Armature Animator (With Sword Attacks)")]
    public static void CreateAnimator()
    {
        string controllerPath = "Assets/Resources/Animations/ArmatureAnimator.controller";
        
        // Delete existing
        if (File.Exists(controllerPath))
        {
            AssetDatabase.DeleteAsset(controllerPath);
            AssetDatabase.Refresh();
        }
        
        string dir = Path.GetDirectoryName(controllerPath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        
        // Create controller
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        
        // Add ALL parameters needed
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Bool);
        controller.AddParameter("FreeFall", AnimatorControllerParameterType.Bool);
        controller.AddParameter("MotionSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsWeaponDrawn", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("AttackType", AnimatorControllerParameterType.Int);
        
        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;
        
        // Load all animation clips
        AnimationClip idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/DoubleL/Demo/Anim/OneHand_Up_Idle.anim");
        AnimationClip walkF = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/DoubleL/Demo/Anim/OneHand_Up_Walk_F_InPlace.anim");
        AnimationClip attack1 = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_1_InPlace.anim");
        AnimationClip attack2 = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_2_InPlace.anim");
        AnimationClip attack3 = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_3_InPlace.anim");
        
        string statusMessage = "=== ARMATURE ANIMATOR SETUP ===\n\n";
        
        // Create Idle state
        AnimatorState idleState = rootStateMachine.AddState("Idle", new Vector3(300, 100, 0));
        if (idleClip != null)
        {
            idleState.motion = idleClip;
            statusMessage += "✓ Idle: " + idleClip.name + "\n";
        }
        else
        {
            statusMessage += "✗ Idle: NOT FOUND\n";
        }
        
        // Create Walk state
        AnimatorState walkState = rootStateMachine.AddState("Walk", new Vector3(300, 200, 0));
        if (walkF != null)
        {
            walkState.motion = walkF;
            statusMessage += "✓ Walk: " + walkF.name + "\n";
        }
        else
        {
            statusMessage += "✗ Walk: NOT FOUND\n";
        }
        
        // Create Attack states
        AnimatorState attack1State = null;
        AnimatorState attack2State = null;
        AnimatorState attack3State = null;
        
        if (attack1 != null)
        {
            attack1State = rootStateMachine.AddState("Attack_1", new Vector3(500, 50, 0));
            attack1State.motion = attack1;
            attack1State.speed = 1.5f; // Faster attack
            statusMessage += "✓ Attack_1: " + attack1.name + "\n";
        }
        else
        {
            statusMessage += "✗ Attack_1: NOT FOUND\n";
        }
        
        if (attack2 != null)
        {
            attack2State = rootStateMachine.AddState("Attack_2", new Vector3(500, 150, 0));
            attack2State.motion = attack2;
            attack2State.speed = 1.5f;
            statusMessage += "✓ Attack_2: " + attack2.name + "\n";
        }
        else
        {
            statusMessage += "✗ Attack_2: NOT FOUND\n";
        }
        
        if (attack3 != null)
        {
            attack3State = rootStateMachine.AddState("Attack_3", new Vector3(500, 250, 0));
            attack3State.motion = attack3;
            attack3State.speed = 1.5f;
            statusMessage += "✓ Attack_3: " + attack3.name + "\n";
        }
        else
        {
            statusMessage += "✗ Attack_3: NOT FOUND\n";
        }
        
        // Create transitions
        if (idleState != null && walkState != null)
        {
            // Idle <-> Walk based on Speed
            AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            idleToWalk.duration = 0.25f;
            idleToWalk.hasExitTime = false;
            
            AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            walkToIdle.duration = 0.25f;
            walkToIdle.hasExitTime = false;
            
            statusMessage += "\n✓ Idle <-> Walk transitions created\n";
        }
        
        // Attack transitions from Idle (AnyState transitions for attacks)
        if (attack1State != null && idleState != null)
        {
            // Attack from Idle
            AnimatorStateTransition idleToAttack1 = idleState.AddTransition(attack1State);
            idleToAttack1.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            idleToAttack1.AddCondition(AnimatorConditionMode.Equals, 0, "AttackType");
            idleToAttack1.duration = 0.1f;
            idleToAttack1.hasExitTime = false;
            
            // Return to Idle after attack
            AnimatorStateTransition attack1ToIdle = attack1State.AddTransition(idleState);
            attack1ToIdle.duration = 0.1f;
            attack1ToIdle.hasExitTime = true;
            attack1ToIdle.exitTime = 0.9f;
            attack1ToIdle.hasFixedDuration = true;
            
            statusMessage += "✓ Attack_1 transitions created\n";
        }
        
        if (attack2State != null && idleState != null)
        {
            AnimatorStateTransition idleToAttack2 = idleState.AddTransition(attack2State);
            idleToAttack2.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            idleToAttack2.AddCondition(AnimatorConditionMode.Equals, 1, "AttackType");
            idleToAttack2.duration = 0.1f;
            idleToAttack2.hasExitTime = false;
            
            AnimatorStateTransition attack2ToIdle = attack2State.AddTransition(idleState);
            attack2ToIdle.duration = 0.1f;
            attack2ToIdle.hasExitTime = true;
            attack2ToIdle.exitTime = 0.9f;
            attack2ToIdle.hasFixedDuration = true;
            
            statusMessage += "✓ Attack_2 transitions created\n";
        }
        
        if (attack3State != null && idleState != null)
        {
            AnimatorStateTransition idleToAttack3 = idleState.AddTransition(attack3State);
            idleToAttack3.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            idleToAttack3.AddCondition(AnimatorConditionMode.Equals, 2, "AttackType");
            idleToAttack3.duration = 0.1f;
            idleToAttack3.hasExitTime = false;
            
            AnimatorStateTransition attack3ToIdle = attack3State.AddTransition(idleState);
            attack3ToIdle.duration = 0.1f;
            attack3ToIdle.hasExitTime = true;
            attack3ToIdle.exitTime = 0.9f;
            attack3ToIdle.hasFixedDuration = true;
            
            statusMessage += "✓ Attack_3 transitions created\n";
        }
        
        // Set default state
        rootStateMachine.defaultState = idleState;
        
        // Save
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log(statusMessage);
        EditorUtility.DisplayDialog("Armature Animator Created", statusMessage, "OK");
    }
}
