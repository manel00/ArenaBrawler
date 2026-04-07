using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ArenaEnhanced
{
    /// <summary>
    /// Módulo dedicado al spawneo de combatientes (jugador, bots, bosses)
    /// </summary>
    public static class ArenaFighterSpawner
    {
        public static ArenaCombatant SpawnPlayer(string playerName, Vector3 position, int team, GameObject modelPrefab = null)
        {
            return SpawnFighter(playerName, position, new Color(0.2f, 0.8f, 1f), team, true, modelPrefab);
        }

        public static ArenaCombatant SpawnAllyBot(string botName, Vector3 position, int team, int index, GameObject modelPrefab = null)
        {
            var bot = SpawnFighter(botName, position, new Color(0.4f, 1f, 0.4f), team, false, modelPrefab);
            if (bot != null)
            {
                bot.displayName = $"Ally Bot {index + 1}";
            }
            return bot;
        }

        public static ArenaCombatant SpawnBoss(Vector3 position)
        {
            var go = new GameObject("Mech_Boss");
            go.transform.position = position;

#if UNITY_EDITOR
            string robotPath = "Assets/Models/Characters/Mech/Mech_FinnTheFrog.fbx";
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(robotPath);

            if (modelPrefab != null)
            {
                var model = Object.Instantiate(modelPrefab, go.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                model.transform.localScale = Vector3.one * 1.25f;
                
                foreach (var c in model.GetComponentsInChildren<Collider>(true)) 
                    Object.DestroyImmediate(c);
            }
            else
            {
                Debug.LogError("[ArenaFighterSpawner] Boss Mech FBX not found at " + robotPath);
            }
#endif
            var col = go.AddComponent<CapsuleCollider>();
            col.height = 1.8f * 1.25f;
            col.radius = 0.4f * 1.25f;
            col.center = new Vector3(0, 0.9f * 1.25f, 0);

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 120f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            var combatant = go.AddComponent<ArenaCombatant>();
            combatant.displayName = "Mech Boss";
            combatant.teamId = 99;
            combatant.isPlayer = false;
            combatant.maxHp = 200f;
            combatant.hp = 200f;

            go.AddComponent<BossController>();
            
            var hpBar = go.AddComponent<WorldHPBar>();
            hpBar.combatant = combatant;
            hpBar.label = "Mech Boss";

            return combatant;
        }

        private static ArenaCombatant SpawnFighter(string fighterName, Vector3 position, Color color, int team, bool isPlayer, GameObject modelPrefab = null)
        {
            var go = new GameObject(fighterName);
            go.transform.position = position;

#if UNITY_EDITOR
            // Use DoubleL Armature (has all animations - One Hand Up)
            if (modelPrefab == null)
            {
                // Try DoubleL Armature first (the one with all movements and sword)
                modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DoubleL/Model/Armature.fbx");
                if (modelPrefab == null)
                {
                    // Fallback to PlayerRobot if not found
                    modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayerRobot.prefab");
                }
            }

            if (modelPrefab != null)
            {
                var model = Object.Instantiate(modelPrefab, go.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                model.transform.localScale = Vector3.one;
                
                if (!isPlayer)
                {
                    DisablePlayerComponents(model);
                    ApplyBotColor(model);
                }

                CleanupComponents(model);
                SetupAnimation(model);
                
                foreach (var c in model.GetComponentsInChildren<Collider>(true)) 
                    Object.DestroyImmediate(c);
            }
#else
            // RUNTIME FALLBACK: Crear personaje procedural para builds
            CreateProceduralCharacter(go, isPlayer, color);
#endif
            var col = go.AddComponent<CapsuleCollider>();
            col.height = 1.8f;
            col.radius = 0.4f;
            col.center = new Vector3(0, 0.9f, 0);

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 70f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            var combatant = go.AddComponent<ArenaCombatant>();
            combatant.displayName = fighterName;
            combatant.teamId = team;
            combatant.isPlayer = isPlayer;
            combatant.maxHp = 100f;
            combatant.hp = 100f;
            go.AddComponent<PlayerWeaponSystem>();

            if (isPlayer) 
            {
                go.tag = "Player";
                var pc = go.AddComponent<PlayerController>();
                pc.jumpForce = 25f;
                
                // Añadir KatanaWeapon para el jugador (espada)
                var katana = go.GetComponent<KatanaWeapon>();
                if (katana == null)
                    katana = go.AddComponent<KatanaWeapon>();
                
                // Auto-equip katana on spawn - no need to press K
                // The Start() method in KatanaWeapon will handle auto-equip
                    
                Debug.Log("[ArenaFighterSpawner] Player setup complete with KatanaWeapon (auto-equipped)");
            }
            else 
            {
                go.AddComponent<BotController>();
                var hpBar = go.AddComponent<WorldHPBar>();
                hpBar.combatant = combatant;
                hpBar.label = fighterName;
            }

            return combatant;
        }

#if UNITY_EDITOR
        private static void DisablePlayerComponents(GameObject model)
        {
            var compsToDisable = model.GetComponentsInChildren<Behaviour>(true);
            foreach (var c in compsToDisable)
            {
                string typeName = c.GetType().Name;
                if (typeName.Contains("Camera") || typeName.Contains("Listener") || 
                    typeName.Contains("Cinemachine") || typeName.Contains("ThirdPersonController") ||
                    typeName.Contains("StarterAssetsInputs") || typeName.Contains("PlayerInput") ||
                    typeName.Contains("RespawnPlayer") || typeName == "CharacterController" ||
                    typeName == "UniversalAdditionalCameraData")
                {
                    c.enabled = false;
                }
            }
        }

        private static void ApplyBotColor(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            Color rndColor = Random.ColorHSV(0, 1, 0.4f, 1, 0.4f, 1);
            foreach (var r in renderers)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = rndColor;
                r.material = mat;
            }
        }

        private static void CleanupComponents(GameObject model)
        {
            var allComps = model.GetComponentsInChildren<Component>(true);
            var toRemove = new System.Collections.Generic.List<Component>();

            foreach (var c in allComps)
            {
                if (c == null || c is Transform) continue;
                string typeName = c.GetType().Name;
                
                if (typeName.Contains("Camera") || typeName.Contains("AudioListener") || 
                    typeName.Contains("Cinemachine") || typeName.Contains("ThirdPersonController") ||
                    typeName.Contains("StarterAssetsInputs") || typeName.Contains("PlayerInput") ||
                    typeName.Contains("RespawnPlayer") || typeName == "CharacterController" ||
                    typeName == "UniversalAdditionalCameraData")
                {
                    toRemove.Add(c);
                }
            }

            foreach (var c in toRemove) {
                if (c != null && c is MonoBehaviour) {
                    try { Object.DestroyImmediate(c); } catch { }
                }
            }
            foreach (var c in toRemove) {
                if (c != null) {
                    try { Object.DestroyImmediate(c); } catch { }
                }
            }

            var cameraRigNames = new[] { "RobotCamera", "PlayerFollowCamera", "PlayerCameraRoot", "CinemachineTarget" };
            foreach (var rigName in cameraRigNames)
            {
                var rigTransform = model.transform.Find(rigName);
                if (rigTransform != null)
                    Object.DestroyImmediate(rigTransform.gameObject);
            }
        }

        private static void SetupAnimation(GameObject model)
        {
            // Use ArmatureAnimator with One Hand Up animations
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Resources/Animations/ArmatureAnimator.controller");
            
            // Fallback to StarterAssets if not found
            if (controller == null)
            {
                controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    "Assets/SourceFiles/StarterAssets/ThirdPersonController/Character/Animations/StarterAssetsThirdPerson.controller");
            }
            
            // Try to get avatar from the model itself first
            Avatar avatar = null;
            var modelAnimator = model.GetComponentInChildren<Animator>(true);
            if (modelAnimator != null && modelAnimator.avatar != null)
            {
                avatar = modelAnimator.avatar;
            }
            
            // Fallback to DoubleL Armature avatar
            if (avatar == null)
            {
                avatar = AssetDatabase.LoadAssetAtPath<Avatar>(
                    "Assets/DoubleL/Model/Armature.fbx");
            }
            
            // Last fallback to StarterAssets avatar
            if (avatar == null)
            {
                avatar = AssetDatabase.LoadAssetAtPath<Avatar>(
                    "Assets/SourceFiles/StarterAssets/ThirdPersonController/Character/Models/Armature.fbx");
            }

            foreach (var anim in model.GetComponentsInChildren<Animator>(true))
            {
                // Assign the controller if found
                if (controller != null)
                {
                    anim.runtimeAnimatorController = controller;
                }
                
                // Assign humanoid avatar if found
                if (avatar != null)
                {
                    anim.avatar = avatar;
                }
                
                // Ensure animator is enabled
                anim.enabled = true;
                
                // Add AnimationEventReceiver for combat events
                if (anim.gameObject.GetComponent<AnimationEventReceiver>() == null)
                    anim.gameObject.AddComponent<AnimationEventReceiver>();
            }
        }
#else
        // RUNTIME FALLBACK: Crear personaje procedural para builds standalone
        private static void CreateProceduralCharacter(GameObject parent, bool isPlayer, Color color)
        {
            // Cuerpo principal (Capsule)
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(parent.transform);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
            
            // Material
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = isPlayer ? new Color(0.2f, 0.8f, 1f) : color;
            body.GetComponent<Renderer>().material = mat;
            
            // Destruir collider del body (usamos el del parent)
            Object.DestroyImmediate(body.GetComponent<Collider>());
            
            // Cabeza (Sphere)
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(parent.transform);
            head.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            head.transform.localScale = Vector3.one * 0.4f;
            head.GetComponent<Renderer>().material = mat;
            Object.DestroyImmediate(head.GetComponent<Collider>());
            
            // Ojos
            var leftEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leftEye.name = "LeftEye";
            leftEye.transform.SetParent(head.transform);
            leftEye.transform.localPosition = new Vector3(-0.12f, 0.05f, 0.15f);
            leftEye.transform.localScale = Vector3.one * 0.15f;
            var eyeMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            eyeMat.color = Color.black;
            leftEye.GetComponent<Renderer>().material = eyeMat;
            Object.DestroyImmediate(leftEye.GetComponent<Collider>());
            
            var rightEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rightEye.name = "RightEye";
            rightEye.transform.SetParent(head.transform);
            rightEye.transform.localPosition = new Vector3(0.12f, 0.05f, 0.15f);
            rightEye.transform.localScale = Vector3.one * 0.15f;
            rightEye.GetComponent<Renderer>().material = eyeMat;
            Object.DestroyImmediate(rightEye.GetComponent<Collider>());
            
            // Añadir Animator básico
            var animator = parent.AddComponent<Animator>();
            animator.applyRootMotion = false;
            
            Debug.Log($"[ArenaFighterSpawner] Procedural character created for {(isPlayer ? "Player" : "Bot")}");
        }
#endif
    }
}
