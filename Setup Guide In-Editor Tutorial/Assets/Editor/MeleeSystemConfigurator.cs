#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace ArenaEnhanced.Editor
{
    /// <summary>
    /// Configurador completo del sistema melee - ejecutar despues de asignar animaciones
    /// </summary>
    public static class MeleeSystemConfigurator
    {
        [MenuItem("Tools/Configure Melee System")]
        public static void Configure()
        {
            // 1. Configurar PlayerRobot
            ConfigurePlayerRobot();
            
            // 2. Crear prefab de espada con hitbox
            CreateIceSwordPrefab();
            
            // 3. Configurar Animation Events
            ConfigureAnimationEvents();
            
            EditorUtility.DisplayDialog(
                "Melee System Configured",
                "Sistema melee configurado exitosamente!\n\n" +
                "- PlayerRobot configurado con WeaponMeleeSystem\n" +
                "- IceSword prefab creado con MeleeHitbox\n" +
                "- Animation events configurados\n\n" +
                "Ahora asigna el Animator Controller al modelo del personaje y prueba el sistema.",
                "OK");
        }
        
        private static void ConfigurePlayerRobot()
        {
            // Buscar PlayerRobot en la escena
            var playerRobot = GameObject.Find("PlayerRobot");
            if (playerRobot == null)
            {
                // Buscar instancia del prefab
                var players = GameObject.FindGameObjectsWithTag("Player");
                foreach (var p in players)
                {
                    if (p.GetComponent<ArenaCombatant>() != null)
                    {
                        playerRobot = p;
                        break;
                    }
                }
            }
            
            if (playerRobot == null)
            {
                Debug.LogWarning("[MeleeSystemConfigurator] No se encontro PlayerRobot en la escena");
                return;
            }
            
            // Asegurar componentes
            var weaponSystem = playerRobot.GetComponent<ArenaEnhanced.PlayerWeaponSystem>();
            if (weaponSystem == null)
                weaponSystem = playerRobot.AddComponent<ArenaEnhanced.PlayerWeaponSystem>();
                
            var meleeSystem = playerRobot.GetComponent<ArenaEnhanced.WeaponMeleeSystem>();
            if (meleeSystem == null)
                meleeSystem = playerRobot.AddComponent<ArenaEnhanced.WeaponMeleeSystem>();
            
            // Configurar WeaponHoldPoint
            var weaponHoldPoint = playerRobot.transform.Find("WeaponHoldPoint");
            if (weaponHoldPoint == null)
            {
                var holdPoint = new GameObject("WeaponHoldPoint");
                holdPoint.transform.SetParent(playerRobot.transform);
                holdPoint.transform.localPosition = new Vector3(0.35f, 1.2f, 0.45f);
                weaponHoldPoint = holdPoint.transform;
            }
            
            // Configurar SheathePoint
            var sheathePoint = playerRobot.transform.Find("SheathePoint");
            if (sheathePoint == null)
            {
                var sheath = new GameObject("SheathePoint");
                sheath.transform.SetParent(playerRobot.transform);
                sheath.transform.localPosition = new Vector3(0.15f, 0.8f, -0.25f);
                sheath.transform.localRotation = Quaternion.Euler(0, 90, 90);
                sheathePoint = sheath.transform;
            }
            
            // Configurar Animator Controller
            var animator = playerRobot.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    "Assets/Resources/Animations/PlayerMeleeAnimator.controller");
                if (controller != null)
                {
                    animator.runtimeAnimatorController = controller;
                    Debug.Log("[MeleeSystemConfigurator] Animator Controller asignado");
                }
            }
            
            // Cargar WeaponData de la espada
            var iceSwordData = AssetDatabase.LoadAssetAtPath<ArenaEnhanced.WeaponData>(
                "Assets/Resources/Weapons/IceSword_WeaponData.asset");
            if (iceSwordData != null && weaponSystem.currentWeaponData == null)
            {
                weaponSystem.currentWeaponData = iceSwordData;
                weaponSystem.currentAmmo = -1; // Infinite ammo for melee
                EditorUtility.SetDirty(weaponSystem);
            }
            
            EditorUtility.SetDirty(playerRobot);
            Debug.Log("[MeleeSystemConfigurator] PlayerRobot configurado");
        }
        
        private static void CreateIceSwordPrefab()
        {
            string prefabPath = "Assets/Prefabs/Weapons/IceSword_Melee.prefab";
            
            // Crear directorio si no existe
            System.IO.Directory.CreateDirectory("Assets/Prefabs/Weapons");
            
            // Verificar si ya existe
            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existingPrefab != null)
            {
                Debug.Log("[MeleeSystemConfigurator] IceSword prefab ya existe");
                return;
            }
            
            // Cargar modelo de espada
            var swordModel = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Models/Weapons/ice_sword_by_get3dmodels.glb");
            
            if (swordModel == null)
            {
                Debug.LogWarning("[MeleeSystemConfigurator] No se encontro el modelo ice_sword_by_get3dmodels.glb");
                return;
            }
            
            // Instanciar para editar
            GameObject swordInstance = (GameObject)PrefabUtility.InstantiatePrefab(swordModel);
            swordInstance.name = "IceSword_Melee";
            
            // Buscar o crear punto de hitbox
            Transform hitboxPoint = swordInstance.transform.Find("HitboxPoint");
            if (hitboxPoint == null)
            {
                var hitboxObj = new GameObject("HitboxPoint");
                hitboxObj.transform.SetParent(swordInstance.transform);
                hitboxObj.transform.localPosition = new Vector3(0, 0.5f, 0);
                hitboxObj.transform.localRotation = Quaternion.identity;
                hitboxPoint = hitboxObj.transform;
            }
            
            // Anadir MeleeHitbox
            var hitbox = hitboxPoint.gameObject.GetComponent<ArenaEnhanced.MeleeHitbox>();
            if (hitbox == null)
                hitbox = hitboxPoint.gameObject.AddComponent<ArenaEnhanced.MeleeHitbox>();
            
            // Configurar BoxCollider
            var collider = hitboxPoint.gameObject.GetComponent<BoxCollider>();
            if (collider == null)
                collider = hitboxPoint.gameObject.AddComponent<BoxCollider>();
            
            collider.size = new Vector3(0.15f, 0.8f, 0.05f);
            collider.center = Vector3.zero;
            collider.isTrigger = true;
            collider.enabled = false; // Desactivado por defecto
            
            // Guardar como prefab
            PrefabUtility.SaveAsPrefabAsset(swordInstance, prefabPath);
            Object.DestroyImmediate(swordInstance);
            
            AssetDatabase.Refresh();
            Debug.Log($"[MeleeSystemConfigurator] IceSword prefab creado en {prefabPath}");
        }
        
        private static void ConfigureAnimationEvents()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                "Assets/Resources/Animations/PlayerMeleeAnimator.controller");
            
            if (controller == null) return;
            
            // Obtener clips de animacion de DoubleL
            string[] attackClips = new string[]
            {
                "Assets/DoubleL/One Hand Up/Attack_A/InPlace/1Hand_Up_Attack_A_1_InPlace.fbx",
                "Assets/DoubleL/One Hand Up/Attack_A/InPlace/1Hand_Up_Attack_A_2_InPlace.fbx",
                "Assets/DoubleL/One Hand Up/Attack_A/InPlace/1Hand_Up_Attack_A_3_InPlace.fbx",
                "Assets/DoubleL/One Hand Up/Attack_B/InPlace/1Hand_Up_Attack_B_1_InPlace.fbx",
                "Assets/DoubleL/One Hand Up/Attack_B/InPlace/1Hand_Up_Attack_B_2_InPlace.fbx",
                "Assets/DoubleL/One Hand Up/Attack_B/InPlace/1Hand_Up_Attack_B_3_InPlace.fbx"
            };
            
            foreach (string path in attackClips)
            {
                AddAnimationEventsToClip(path);
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MeleeSystemConfigurator] Animation events configurados");
        }
        
        private static void AddAnimationEventsToClip(string fbxPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.Contains("__preview"))
                {
                    AnimationEvent[] existingEvents = AnimationUtility.GetAnimationEvents(clip);
                    
                    // Verificar si ya tiene eventos
                    bool hasEnable = false;
                    bool hasDisable = false;
                    
                    foreach (var evt in existingEvents)
                    {
                        if (evt.functionName == "EnableHitbox") hasEnable = true;
                        if (evt.functionName == "DisableHitbox") hasDisable = true;
                    }
                    
                    var events = new System.Collections.Generic.List<AnimationEvent>(existingEvents);
                    
                    if (!hasEnable)
                    {
                        AnimationEvent enableEvent = new AnimationEvent();
                        enableEvent.functionName = "EnableHitbox";
                        enableEvent.time = 0.2f; // Activar hitbox a 20% de la animacion
                        events.Add(enableEvent);
                    }
                    
                    if (!hasDisable)
                    {
                        AnimationEvent disableEvent = new AnimationEvent();
                        disableEvent.functionName = "DisableHitbox";
                        disableEvent.time = 0.8f; // Desactivar hitbox a 80% de la animacion
                        events.Add(disableEvent);
                    }
                    
                    AnimationUtility.SetAnimationEvents(clip, events.ToArray());
                    EditorUtility.SetDirty(clip);
                }
            }
        }
    }
}
#endif
