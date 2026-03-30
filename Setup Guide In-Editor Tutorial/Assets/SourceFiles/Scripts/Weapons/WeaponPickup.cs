using ArenaEnhanced;
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
        
        private int _currentAmmo;
        private Vector3 _originalPosition;
        private float _bobTimer;
        private const float BOB_SPEED = 2f;
        private const float BOB_HEIGHT = 0.1f;
        private const float GROUND_PICKUP_SCALE_MULTIPLIER = 0.25f;

        // Caché estático de materiales para evitar memory leaks
        private static readonly System.Collections.Generic.Dictionary<Color, Material> _materialCache = new System.Collections.Generic.Dictionary<Color, Material>();
        
        // Events
        public System.Action<WeaponData, int> OnWeaponPickedUp;
        
        public int CurrentAmmo => _currentAmmo;
        public int MaxAmmo => weaponData != null ? weaponData.DefaultAmmo : 0;
        
        private void Awake()
        {
            _originalPosition = transform.position;
            _currentAmmo = weaponData != null ? weaponData.DefaultAmmo : 0;
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
        public void Setup(WeaponData data, int ammo)
        {
            weaponData = data;
            _currentAmmo = data != null && data.UsesAmmo ? ammo : -1;
            
            ApplyAppearance();
            
            // Escalar según datos (25% del tamaño actual para armas en suelo)
            transform.localScale = data != null ? data.weaponScale * GROUND_PICKUP_SCALE_MULTIPLIER : Vector3.one * GROUND_PICKUP_SCALE_MULTIPLIER;
            
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
            OnWeaponPickedUp?.Invoke(weaponData, _currentAmmo);
            
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
        public static WeaponPickup CreatePickup(WeaponData data, Vector3 position, int ammo = -1)
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
                weaponObj.transform.localScale = data.weaponScale;
            }
            
            weaponObj.name = $"WeaponPickup_{data.weaponName}";
            weaponObj.transform.position = position;
            
            // Aplicar material/textura del arma ANTES de crear el pickup
            ApplyMaterialToWeapon(weaponObj, data);
            
            // Aplicar rotación del WeaponData para que apunte hacia adelante
            weaponObj.transform.rotation = Quaternion.Euler(data.rotationOffset);
            
            // Configurar collider como trigger para detección
            var existingColliders = weaponObj.GetComponentsInChildren<Collider>(true);
            if (existingColliders != null && existingColliders.Length > 0)
            {
                foreach (var collider in existingColliders)
                {
                    collider.isTrigger = true;
                }
            }
            else
            {
                // Añadir BoxCollider si no existe
                var boxCol = weaponObj.AddComponent<BoxCollider>();
                boxCol.isTrigger = true;
            }
            
            // Añadir Rigidbody para física
            var rb = weaponObj.GetComponent<Rigidbody>();
            if (rb == null) rb = weaponObj.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
            
            // Crear el componente WeaponPickup
            var pickup = weaponObj.AddComponent<WeaponPickup>();
            pickup.Setup(data, ammo >= 0 ? ammo : data.DefaultAmmo);
            
            return pickup;
        }

        private static void ApplyMaterialToWeapon(GameObject weaponObj, WeaponData data)
        {
            if (weaponObj == null || data == null) return;
            
            var renderers = weaponObj.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (data.weaponMaterial != null)
                {
                    renderer.material = data.weaponMaterial;
                    Debug.Log($"[WeaponPickup] Applied material {data.weaponMaterial.name} to {data.weaponName}");
                }
                else if (data.weaponTexture != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = data.weaponColor;
                    mat.mainTexture = data.weaponTexture;
                    renderer.material = mat;
                    Debug.Log($"[WeaponPickup] Applied texture to {data.weaponName}");
                }
                else
                {
                    // Crear material con color específico
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = data.weaponColor;
                    renderer.material = mat;
                    Debug.Log($"[WeaponPickup] Applied color {data.weaponColor} to {data.weaponName}");
                }
            }
        }

        private void ApplyAppearance()
        {
            if (weaponData == null) return;

            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (weaponData.weaponMaterial != null)
                {
                    renderer.material = weaponData.weaponMaterial;
                }
                else if (weaponData.weaponTexture != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = weaponData.weaponColor;
                    mat.mainTexture = weaponData.weaponTexture;
                    renderer.material = mat;
                }
                else
                {
                    renderer.material = GetCachedMaterial(weaponData.weaponColor);
                }
            }
        }

        private static Material GetCachedMaterial(Color color)
        {
            if (_materialCache.TryGetValue(color, out Material mat) && mat != null)
            {
                return mat;
            }

            // Si no existe o se destruyó, crear uno nuevo
            var newMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
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
            
            if (combatant != null)
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