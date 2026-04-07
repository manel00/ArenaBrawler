using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ArenaEnhanced
{
    /// <summary>
    /// Inicializador automático de Fireball KCISA que configura todo en runtime.
    /// Agregar este script al jugador - se encarga de crear el ScriptableObject 
    /// y configurar el prefab Fly02-01 automáticamente.
    /// </summary>
    public class AutoFireballSetup : MonoBehaviour
    {
        [Header("KCISA Prefab")]
        [Tooltip("Prefab Fly02-01 de KoreanTraditionalPattern_Effect")]
        public GameObject kcisaFlyPrefab;
        
        [Header("Fireball Settings")]
        public float damage = 35f;
        public float speed = 15f;
        public float lifetime = 4f;
        public float cooldown = 2f;
        public float staminaCost = 15f;
        public float scale = 0.8f;
        
        private ProjectileAbilityData _fireballAbility;
        private GameObject _configuredPrefab;
        
        void Start()
        {
            SetupFireball();
        }
        
        void SetupFireball()
        {
            // 1. Buscar el prefab KCISA si no está asignado
            if (kcisaFlyPrefab == null)
            {
                kcisaFlyPrefab = FindKcisaPrefab();
            }
            
            if (kcisaFlyPrefab == null)
            {
                Debug.LogError("[AutoFireballSetup] No se encontró el prefab Fly02-01. " +
                    "Por favor asígnalo manualmente en el inspector.");
                return;
            }
            
            Debug.Log("[AutoFireballSetup] Prefab KCISA encontrado: " + kcisaFlyPrefab.name);
            
            // 2. Crear una copia configurada del prefab
            _configuredPrefab = ConfigureKcisaPrefab(kcisaFlyPrefab);
            
            // 3. Crear el ScriptableObject de habilidad en runtime
            _fireballAbility = CreateFireballAbility(_configuredPrefab);
            
            // 4. Asignar al sistema de habilidades
            AssignToAbilitySystem();
            
            Debug.Log("[AutoFireballSetup] Fireball KCISA configurado! Presiona 1 para usar.");
        }
        
        GameObject FindKcisaPrefab()
        {
            // Buscar en Resources
            GameObject prefab = Resources.Load<GameObject>("KoreanTraditionalPattern_Effect/Prefabs/Fly/Fly02-01");
            if (prefab != null) return prefab;
            
            prefab = Resources.Load<GameObject>("Fly02-01");
            if (prefab != null) return prefab;
            
#if UNITY_EDITOR
            // Buscar en AssetDatabase
            string[] guids = AssetDatabase.FindAssets("Fly02-01 t:Prefab", 
                new[] { "Assets/KoreanTraditionalPattern_Effect" });
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset != null) return asset;
            }
#endif
            return null;
        }
        
        GameObject ConfigureKcisaPrefab(GameObject original)
        {
#if UNITY_EDITOR
            // En editor, podemos modificar el prefab original
            string prefabPath = AssetDatabase.GetAssetPath(original);
            
            // Verificar si ya está configurado
            if (original.GetComponent<MagicProjectile>() != null)
            {
                Debug.Log("[AutoFireballSetup] Prefab ya tiene MagicProjectile");
                return original;
            }
            
            // Agregar componentes al prefab original
            MagicProjectile mp = original.AddComponent<MagicProjectile>();
            mp.speed = speed;
            mp.lifetime = lifetime;
            mp.damage = damage;
            
            // Agregar collider si no tiene
            SphereCollider col = original.GetComponent<SphereCollider>();
            if (col == null)
            {
                col = original.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 0.5f;
            }
            
            // Agregar Rigidbody kinematic
            Rigidbody rb = original.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = original.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            
            // Guardar cambios
            PrefabUtility.SavePrefabAsset(original);
            AssetDatabase.Refresh();
            
            Debug.Log("[AutoFireballSetup] Prefab configurado y guardado");
            return original;
#else
            // En runtime/build, crear un nuevo prefab configurado
            GameObject configured = new GameObject(original.name + "_Configured");
            configured.SetActive(false);
            
            // Copiar componentes de renderizado
            MeshRenderer[] renderers = original.GetComponentsInChildren<MeshRenderer>();
            TrailRenderer[] trails = original.GetComponentsInChildren<TrailRenderer>();
            ParticleSystem[] particles = original.GetComponentsInChildren<ParticleSystem>();
            
            // Crear estructura similar
            foreach (var rend in renderers)
            {
                GameObject child = new GameObject(rend.name);
                child.transform.SetParent(configured.transform);
                child.transform.localPosition = rend.transform.localPosition;
                child.transform.localRotation = rend.transform.localRotation;
                child.transform.localScale = rend.transform.localScale;
                
                MeshRenderer mr = child.AddComponent<MeshRenderer>();
                mr.materials = rend.materials;
                
                MeshFilter mf = child.AddComponent<MeshFilter>();
                mf.sharedMesh = rend.GetComponent<MeshFilter>()?.sharedMesh;
            }
            
            // Agregar MagicProjectile
            MagicProjectile mp = configured.AddComponent<MagicProjectile>();
            mp.speed = speed;
            mp.lifetime = lifetime;
            mp.damage = damage;
            
            // Agregar collider
            SphereCollider col = configured.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.5f;
            
            // Agregar Rigidbody
            Rigidbody rb = configured.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            
            Debug.Log("[AutoFireballSetup] Prefab runtime creado");
            return configured;
#endif
        }
        
        ProjectileAbilityData CreateFireballAbility(GameObject prefab)
        {
            // Crear ScriptableObject en runtime
            ProjectileAbilityData ability = ScriptableObject.CreateInstance<ProjectileAbilityData>();
            ability.name = "Fireball_KCISA_Runtime";
            
            // Configurar propiedades base
            ability.abilityName = "Fireball KCISA";
            ability.description = "Lanza una bola de fuego coreana tradicional";
            ability.cooldown = cooldown;
            ability.damage = damage;
            ability.range = 25f;
            ability.staminaCost = staminaCost;
            ability.manaCost = 0f;
            ability.canUseWhileMoving = false;
            ability.requiresTarget = false;
            ability.keyBinding = 1;
            
            // Configurar propiedades de proyectil
            ability.projectilePrefab = prefab;
            ability.projectileSpeed = speed;
            ability.projectileLifetime = lifetime;
            ability.projectileScale = scale;
            ability.spawnOffset = 1.5f;
            ability.spawnHeight = 1.2f;
            
            return ability;
        }
        
        void AssignToAbilitySystem()
        {
            // Buscar AbilitySystem en este GameObject
            AbilitySystem abilitySystem = GetComponent<AbilitySystem>();
            
            if (abilitySystem == null)
            {
                abilitySystem = gameObject.AddComponent<AbilitySystem>();
                Debug.Log("[AutoFireballSetup] AbilitySystem agregado al jugador");
            }
            
            // La asignación real se hace cuando se presiona la tecla
            // porque AbilitySystem requiere inicialización
            StartCoroutine(DelayedAssign(abilitySystem));
        }
        
        System.Collections.IEnumerator DelayedAssign(AbilitySystem system)
        {
            yield return new WaitForSeconds(0.5f);
            system.SetAbility(1, _fireballAbility);
            Debug.Log("[AutoFireballSetup] Fireball asignada al slot 1!");
        }
    }
}
