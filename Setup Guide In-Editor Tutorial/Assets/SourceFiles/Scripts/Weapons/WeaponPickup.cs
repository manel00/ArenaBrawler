using UnityEngine;

namespace WoW.Armas
{
    /// <summary>
    /// Componente para armas que están en el suelo y se pueden recoger
    /// </summary>
    public class WeaponPickup : MonoBehaviour
    {
        [Header("Configuración")]
        public WeaponData weaponData;
        
        // Propiedad pública para acceder a weaponData
        public WeaponData WeaponData => weaponData;
        
        [Header("Visuales")]
        [SerializeField] private SpriteRenderer weaponIcon;
        [SerializeField] private GameObject pickUpVFX;
        
        [Header("Spawn")]
        public Vector3 spawnPosition;
        public Quaternion spawnRotation;
        
        private int _currentDurability;
        private Vector3 _originalPosition;
        private float _bobTimer;
        private const float BOB_SPEED = 2f;
        private const float BOB_HEIGHT = 0.1f;

        // Caché estático de materiales para evitar memory leaks
        private static readonly System.Collections.Generic.Dictionary<Color, Material> _materialCache = new System.Collections.Generic.Dictionary<Color, Material>();
        
        // Events
        public System.Action<WeaponData, int> OnWeaponPickedUp;
        
        public int CurrentDurability => _currentDurability;
        public int MaxDurability => weaponData != null ? weaponData.maxDurability : 5;
        
        private void Awake()
        {
            _originalPosition = transform.position;
            _currentDurability = weaponData != null ? weaponData.maxDurability : 5;
            spawnPosition = _originalPosition;
            spawnRotation = transform.rotation;
        }
        
        private void Update()
        {
            // Animación de flotación
            _bobTimer += Time.deltaTime * BOB_SPEED;
            float yOffset = Mathf.Sin(_bobTimer) * BOB_HEIGHT;
            transform.position = _originalPosition + Vector3.up * yOffset;
            
            // Rotación suave
            transform.Rotate(Vector3.up, Time.deltaTime * 30f);
        }
        
        /// <summary>
        /// Configura el arma con datos específicos
        /// </summary>
        public void Setup(WeaponData data, int durability)
        {
            weaponData = data;
            _currentDurability = durability;
            
            // Aplicar color del arma usando caché
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null && data != null)
            {
                meshRenderer.material = GetCachedMaterial(data.weaponColor);
            }
            
            // Escalar según datos
            transform.localScale = data != null ? data.weaponScale : Vector3.one;
            
            // Spawn VFX
            if (pickUpVFX != null)
            {
                Instantiate(pickUpVFX, transform.position, Quaternion.identity);
            }
        }
        
        /// <summary>
        /// Se llama cuando un jugador recoge el arma
        /// </summary>
        public void PickUp()
        {
            OnWeaponPickedUp?.Invoke(weaponData, _currentDurability);
            
            // Efecto de recogida
            if (pickUpVFX != null)
            {
                Instantiate(pickUpVFX, transform.position, Quaternion.identity);
            }
            
            Destroy(gameObject);
        }
        
        /// <summary>
        /// Crea un arma en el suelo en una posición específica
        /// </summary>
        public static WeaponPickup CreatePickup(WeaponData data, Vector3 position, int durability = -1)
        {
            GameObject weaponObj = null;
            
            // Usar el prefab real si existe
            if (data.prefab != null)
            {
#if UNITY_EDITOR
                weaponObj = UnityEditor.PrefabUtility.InstantiatePrefab(data.prefab) as GameObject;
#endif
                if (weaponObj == null) weaponObj = Instantiate(data.prefab);
            }
            
            // Si no hay prefab, usar un cubo simple como fallback
            if (weaponObj == null)
            {
                weaponObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                weaponObj.transform.localScale = data.weaponScale * 0.3f;
            }
            
            weaponObj.name = $"WeaponPickup_{data.weaponName}";
            weaponObj.transform.position = position;
            
            // Configurar collider como trigger para detección
            var existingCollider = weaponObj.GetComponent<Collider>();
            if (existingCollider != null)
            {
                existingCollider.isTrigger = true;
            }
            else
            {
                // Añadir BoxCollider si no existe
                var boxCol = weaponObj.AddComponent<BoxCollider>();
                boxCol.isTrigger = true;
            }
            
            // Añadir Rigidbody para física
            var rb = weaponObj.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
            
            // Material con el color del arma (reutilizando caché)
            var renderer = weaponObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = GetCachedMaterial(data.weaponColor);
            }
            
            // Crear el componente WeaponPickup
            var pickup = weaponObj.AddComponent<WeaponPickup>();
            pickup.Setup(data, durability >= 0 ? durability : data.maxDurability);
            
            return pickup;
        }

        private static Material GetCachedMaterial(Color color)
        {
            if (_materialCache.TryGetValue(color, out Material mat) && mat != null)
            {
                return mat;
            }

            // Si no existe o se destruyó, crear uno nuevo
            var newMat = new Material(Shader.Find("Standard"));
            newMat.color = color;
            newMat.name = $"WeaponMat_{ColorUtility.ToHtmlStringRGB(color)}";
            _materialCache[color] = newMat;
            return newMat;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            // Verificar si es un jugador
            var combatant = other.GetComponent<ArenaEnhanced.ArenaCombatant>();
            if (combatant == null)
            {
                combatant = other.GetComponentInParent<ArenaEnhanced.ArenaCombatant>();
            }
            
            if (combatant != null && combatant.isPlayer)
            {
                // Notificar al sistema de inventario
                var weaponSystem = combatant.GetComponent<PlayerWeaponSystem>();
                if (weaponSystem != null)
                {
                    weaponSystem.TryPickUpWeapon(this);
                }
            }
        }
    }
}