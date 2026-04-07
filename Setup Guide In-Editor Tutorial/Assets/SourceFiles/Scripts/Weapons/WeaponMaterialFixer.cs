using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Utilidad para arreglar materiales violetas en armas importadas (.obj, .fbx)
    /// Aplica el material correcto del WeaponData a todos los renderers
    /// </summary>
    public static class WeaponMaterialFixer
    {
        private static Shader _urpLitShader;
        
        private static Shader GetURPLitShader()
        {
            if (_urpLitShader == null)
                _urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            return _urpLitShader;
        }
        
        /// <summary>
        /// Arregla los materiales de un arma aplicando el material correcto del WeaponData
        /// </summary>
        public static void FixWeaponMaterials(GameObject weaponObj, WeaponData data)
        {
            if (weaponObj == null || data == null) return;
            
            var renderers = weaponObj.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Debug.LogWarning($"[WeaponMaterialFixer] No renderers found on {weaponObj.name}");
                return;
            }
            
            foreach (var renderer in renderers)
            {
                // Verificar si el material es violeto (indica shader missing)
                bool hasPurpleMaterial = IsPurpleMaterial(renderer.material);
                
                if (hasPurpleMaterial)
                {
                    Debug.Log($"[WeaponMaterialFixer] Detected purple material on {renderer.name}, fixing...");
                }
                
                // Aplicar el material del WeaponData
                if (data.weaponMaterial != null)
                {
                    // Verificar que el material tenga shader válido
                    if (data.weaponMaterial.shader == null || data.weaponMaterial.shader.name.Contains("Hidden/InternalErrorShader"))
                    {
                        Debug.LogError($"[WeaponMaterialFixer] Weapon material {data.weaponMaterial.name} has missing shader!");
                        // Crear material temporal con color
                        ApplyFallbackMaterial(renderer, data.weaponColor);
                    }
                    else
                    {
                        renderer.material = data.weaponMaterial;
                        Debug.Log($"[WeaponMaterialFixer] Applied material {data.weaponMaterial.name} to {renderer.name}");
                    }
                }
                else
                {
                    // Crear material con color
                    ApplyFallbackMaterial(renderer, data.weaponColor);
                }
            }
        }
        
        /// <summary>
        /// Verifica si un material es violeto (indica shader missing/error)
        /// </summary>
        private static bool IsPurpleMaterial(Material mat)
        {
            if (mat == null) return true;
            if (mat.shader == null) return true;
            if (mat.shader.name.Contains("Hidden/InternalErrorShader")) return true;
            if (mat.shader.name.Contains("Error")) return true;
            
            // Verificar color magenta/violeta
            Color col = mat.color;
            if (col.r > 0.9f && col.g < 0.1f && col.b > 0.9f)
                return true;
                
            return false;
        }
        
        /// <summary>
        /// Aplica un material fallback con el color especificado
        /// </summary>
        private static void ApplyFallbackMaterial(Renderer renderer, Color color)
        {
            Shader shader = GetURPLitShader();
            if (shader == null)
            {
                Debug.LogError("[WeaponMaterialFixer] URP Lit shader not found! Using Standard fallback.");
                shader = Shader.Find("Standard");
            }
            
            Material mat = new Material(shader);
            mat.color = color;
            mat.name = $"FixedWeaponMat_{color.r:F2}_{color.g:F2}_{color.b:F2}";
            renderer.material = mat;
            
            Debug.Log($"[WeaponMaterialFixer] Applied fallback material with color {color} to {renderer.name}");
        }
        
        /// <summary>
        /// Verifica y arregla el material del AssaultRifle específicamente
        /// </summary>
        public static void FixAssaultRifleMaterial(GameObject rifleObj)
        {
            if (rifleObj == null) return;
            
            // El AssaultRifle usa color azul oscuro
            Color rifleColor = new Color(0.25f, 0.55f, 1f);
            
            var renderers = rifleObj.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                ApplyFallbackMaterial(renderer, rifleColor);
            }
            
            Debug.Log($"[WeaponMaterialFixer] Fixed AssaultRifle materials on {rifleObj.name}");
        }
    }
}
