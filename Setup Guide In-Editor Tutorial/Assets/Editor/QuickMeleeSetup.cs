#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace ArenaEnhanced.Editor
{
    public class QuickMeleeSetup : EditorWindow
    {
        [MenuItem("ArenaEnhanced/Quick Melee Setup")]
        public static void ShowWindow()
        {
            GetWindow<QuickMeleeSetup>("Melee Setup");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Configuración Rápida Melee", EditorStyles.boldLabel);
            
            if (GUILayout.Button("CONFIGURAR PLAYER ROBOT", GUILayout.Height(40)))
            {
                SetupPlayerRobot();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("PLAY - Probar Ahora!", GUILayout.Height(40)))
            {
                EditorApplication.isPlaying = true;
            }
        }
        
        void SetupPlayerRobot()
        {
            GameObject player = GameObject.Find("PlayerRobot");
            if (player == null)
            {
                EditorUtility.DisplayDialog("Error", "No se encontró PlayerRobot en la escena", "OK");
                return;
            }
            
            // Componentes
            var melee = player.GetComponent<ArenaEnhanced.WeaponMeleeSystem>();
            if (melee == null) melee = player.AddComponent<ArenaEnhanced.WeaponMeleeSystem>();
            
            // Puntos
            Transform hold = player.transform.Find("WeaponHoldPoint");
            if (hold == null)
            {
                GameObject h = new GameObject("WeaponHoldPoint");
                h.transform.SetParent(player.transform);
                h.transform.localPosition = new Vector3(0.35f, 1.2f, 0.45f);
                hold = h.transform;
            }
            
            Transform sheath = player.transform.Find("SheathePoint");
            if (sheath == null)
            {
                GameObject s = new GameObject("SheathePoint");
                s.transform.SetParent(player.transform);
                s.transform.localPosition = new Vector3(0.15f, 0.8f, -0.25f);
                sheath = s.transform;
            }
            
            // Configurar
            SerializedObject so = new SerializedObject(melee);
            so.FindProperty("handHoldPoint").objectReferenceValue = hold;
            so.FindProperty("sheathePoint").objectReferenceValue = sheath;
            
            Animator anim = player.GetComponentInChildren<Animator>();
            so.FindProperty("animator").objectReferenceValue = anim;
            so.ApplyModifiedProperties();
            
            // Animator Controller
            var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(
                "Assets/Resources/Animations/PlayerMeleeAnimator.controller");
            if (controller != null && anim != null)
            {
                anim.runtimeAnimatorController = controller;
            }
            
            EditorUtility.SetDirty(player);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            
            EditorUtility.DisplayDialog("Listo!", "PlayerRobot configurado!\n\nPresiona PLAY para probar", "OK");
        }
    }
}
#endif
