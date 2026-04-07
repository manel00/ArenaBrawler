#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace ArenaEnhanced.Editor
{
    public static class MeleeAnimatorGenerator
    {
        [MenuItem("Tools/Create Melee Animator Controller")]
        public static void CreateMeleeAnimatorController()
        {
            // Create controller
            var controller = AnimatorController.CreateAnimatorControllerAtPath(
                "Assets/Resources/Animations/PlayerMeleeAnimator.controller");

            // Add parameters
            controller.AddParameter("IsWeaponDrawn", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("AttackType", AnimatorControllerParameterType.Int);
            controller.AddParameter("Sheathe", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Unsheathe", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("ComboCount", AnimatorControllerParameterType.Int);

            // Get base layer
            AnimatorControllerLayer baseLayer = controller.layers[0];
            AnimatorStateMachine stateMachine = baseLayer.stateMachine;

            // Load animation clips from DoubleL
            var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                "Assets/DoubleL/One Hand Up/Idle/1Hand_Up_Idle_1.fbx");
            var walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                "Assets/DoubleL/One Hand Up/Movement/Walk/1Hand_Up_Walk_F_Loop.fbx");
            var runClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                "Assets/DoubleL/One Hand Up/Movement/Run/1Hand_Up_Run_F_Loop.fbx");
            var attackA1 = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                "Assets/DoubleL/One Hand Up/Attack_A/InPlace/1Hand_Up_Attack_A_1_InPlace.fbx");
            var attackA2 = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                "Assets/DoubleL/One Hand Up/Attack_A/InPlace/1Hand_Up_Attack_A_2_InPlace.fbx");
            var attackA3 = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                "Assets/DoubleL/One Hand Up/Attack_A/InPlace/1Hand_Up_Attack_A_3_InPlace.fbx");
            var attackB1 = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                "Assets/DoubleL/One Hand Up/Attack_B/InPlace/1Hand_Up_Attack_B_1_InPlace.fbx");
            var attackB2 = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                "Assets/DoubleL/One Hand Up/Attack_B/InPlace/1Hand_Up_Attack_B_2_InPlace.fbx");
            var attackB3 = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                "Assets/DoubleL/One Hand Up/Attack_B/InPlace/1Hand_Up_Attack_B_3_InPlace.fbx");

            // Create states
            Vector3 position = stateMachine.entryPosition;
            
            // Entry state -> Unsheathe
            var unsheatheState = stateMachine.AddState("Unsheathe", new Vector3(position.x + 200, position.y));
            unsheatheState.motion = idleClip; // Use idle as placeholder for unsheathe animation
            
            // Idle Weapon Drawn state
            var idleDrawnState = stateMachine.AddState("Idle_WeaponDrawn", new Vector3(position.x + 400, position.y));
            idleDrawnState.motion = idleClip;
            
            // Movement blend tree state
            var moveState = stateMachine.AddState("Move_WeaponDrawn", new Vector3(position.x + 400, position.y + 100));
            
            // Create blend tree for movement
            var blendTree = new BlendTree();
            blendTree.name = "MovementBlend";
            blendTree.blendParameter = "MoveSpeed";
            blendTree.blendType = BlendTreeType.Simple1D;
            blendTree.AddChild(idleClip, 0f);
            blendTree.AddChild(walkClip, 0.5f);
            blendTree.AddChild(runClip, 1f);
            moveState.motion = blendTree;

            // Attack states
            var attackA1State = stateMachine.AddState("Attack_A_1", new Vector3(position.x + 600, position.y - 100));
            attackA1State.motion = attackA1;
            
            var attackA2State = stateMachine.AddState("Attack_A_2", new Vector3(position.x + 600, position.y - 200));
            attackA2State.motion = attackA2;
            
            var attackA3State = stateMachine.AddState("Attack_A_3", new Vector3(position.x + 600, position.y - 300));
            attackA3State.motion = attackA3;
            
            var attackB1State = stateMachine.AddState("Attack_B_1", new Vector3(position.x + 600, position.y + 100));
            attackB1State.motion = attackB1;
            
            var attackB2State = stateMachine.AddState("Attack_B_2", new Vector3(position.x + 600, position.y + 200));
            attackB2State.motion = attackB2;
            
            var attackB3State = stateMachine.AddState("Attack_B_3", new Vector3(position.x + 600, position.y + 300));
            attackB3State.motion = attackB3;

            // Sheathe state
            var sheatheState = stateMachine.AddState("Sheathe", new Vector3(position.x + 400, position.y - 100));
            sheatheState.motion = idleClip; // Placeholder

            // Idle (weapon sheathed) state
            var idleSheathedState = stateMachine.AddState("Idle_Sheathed", new Vector3(position.x + 200, position.y - 100));
            idleSheathedState.motion = idleClip;

            // Create transitions
            // Entry -> Unsheathe (on start)
            AnimatorTransition entryToUnsheathe = stateMachine.AddEntryTransition(unsheatheState);
            
            // Unshet -> Idle Drawn
            AnimatorStateTransition unsheatheToIdle = unsheatheState.AddTransition(idleDrawnState);
            unsheatheToIdle.exitTime = 0.9f;
            unsheatheToIdle.hasExitTime = true;
            unsheatheToIdle.hasFixedDuration = true;
            unsheatheToIdle.duration = 0.25f;

            // Idle Drawn <-> Movement
            AnimatorStateTransition idleToMove = idleDrawnState.AddTransition(moveState);
            idleToMove.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
            
            AnimatorStateTransition moveToIdle = moveState.AddTransition(idleDrawnState);
            moveToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");

            // Idle Drawn -> Attack A (when Attack trigger and AttackType = 0)
            AnimatorStateTransition idleToAttackA1 = idleDrawnState.AddTransition(attackA1State);
            idleToAttackA1.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            idleToAttackA1.AddCondition(AnimatorConditionMode.Equals, 0, "AttackType");

            // Idle Drawn -> Attack B (when Attack trigger and AttackType = 1)
            AnimatorStateTransition idleToAttackB1 = idleDrawnState.AddTransition(attackB1State);
            idleToAttackB1.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            idleToAttackB1.AddCondition(AnimatorConditionMode.Equals, 1, "AttackType");

            // Attack combos (A series)
            AnimatorStateTransition attackA1ToA2 = attackA1State.AddTransition(attackA2State);
            attackA1ToA2.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            attackA1ToA2.exitTime = 0.8f;
            
            AnimatorStateTransition attackA2ToA3 = attackA2State.AddTransition(attackA3State);
            attackA2ToA3.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            attackA2ToA3.exitTime = 0.8f;

            // Attack combos (B series)
            AnimatorStateTransition attackB1ToB2 = attackB1State.AddTransition(attackB2State);
            attackB1ToB2.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            attackB1ToB2.exitTime = 0.8f;
            
            AnimatorStateTransition attackB2ToB3 = attackB2State.AddTransition(attackB3State);
            attackB2ToB3.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            attackB2ToB3.exitTime = 0.8f;

            // Attacks -> Idle
            AnimatorState[] attackStates = { attackA1State, attackA2State, attackA3State, attackB1State, attackB2State, attackB3State };
            foreach (var state in attackStates)
            {
                AnimatorStateTransition toIdle = state.AddTransition(idleDrawnState);
                toIdle.exitTime = 0.9f;
                toIdle.hasExitTime = true;
            }

            // Idle Drawn -> Sheathe (after idle time)
            AnimatorStateTransition idleToSheathe = idleDrawnState.AddTransition(sheatheState);
            idleToSheathe.AddCondition(AnimatorConditionMode.If, 0, "Sheathe");

            // Sheathe -> Idle Sheathed
            AnimatorStateTransition sheatheToIdle = sheatheState.AddTransition(idleSheathedState);
            sheatheToIdle.exitTime = 0.9f;
            sheatheToIdle.hasExitTime = true;

            // Idle Sheathed -> Unsheathe (on attack input)
            AnimatorStateTransition idleSheathedToUnsheathe = idleSheathedState.AddTransition(unsheatheState);
            idleSheathedToUnsheathe.AddCondition(AnimatorConditionMode.If, 0, "Unsheathe");

            // Save
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Melee Animator Controller Created",
                "Controller created at:\nAssets/Resources/Animations/PlayerMeleeAnimator.controller\n\n" +
                "Now assign this controller to the Animator component on your PlayerRobot/Robot.",
                "OK");

            Selection.activeObject = controller;
        }
    }
}
#endif
