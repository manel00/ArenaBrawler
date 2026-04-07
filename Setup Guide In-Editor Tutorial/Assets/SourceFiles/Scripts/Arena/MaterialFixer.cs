using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ArenaEnhanced
{
    /// <summary>
    /// Arregla los materiales magenta de KoreanTraditionalPattern_Effect
    /// convirtiéndolos a shaders URP estándar
    /// </summary>
    public class MaterialFixer : MonoBehaviour
    {
        [Header("Material Repair Settings")]
        public bool fixOnAwake = true;
        public bool logFixes = true;
        
        // Shader URP estándar para partículas
        private const string URP_PARTICLES_UNLIT = "Universal Render Pipeline/Particles/Unlit";
        private const string URP_PARTICLES_LIT = "Universal Render Pipeline/Particles/Lit";
        private const string URP_SPRITE_DEFAULT = "Sprites/Default";
        private const string URP_LIT = "Universal Render Pipeline/Lit";
        
        void Awake()
        {
            if (fixOnAwake)
            {
                FixAllMaterialsInChildren();
            }
        }
        
        [ContextMenu("Fix All Materials Now")]
        public void FixAllMaterialsInChildren()
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            int fixedCount = 0;
            
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterials == null) continue;
                
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    var mat = renderer.sharedMaterials[i];
                    if (mat == null) continue;
                    
                    // Verificar si el material está roto (magenta)
                    if (IsMaterialBroken(mat))
                    {
                        string originalShader = mat.shader?.name ?? "null";
                        
                        // Elegir shader según el uso (partículas vs mesh)
                        string shaderToUse = URP_PARTICLES_UNLIT;
                        if (renderer is ParticleSystemRenderer)
                            shaderToUse = URP_PARTICLES_UNLIT;
                        else if (renderer is SpriteRenderer)
                            shaderToUse = URP_SPRITE_DEFAULT;
                        
                        // Crear nueva instancia del material con shader URP
                        var newMat = new Material(Shader.Find(shaderToUse));
                        
                        // Copiar todas las texturas posibles
                        CopyTextureIfExists(mat, newMat, "_MainTex", "_BaseMap");
                        CopyTextureIfExists(mat, newMat, "_Flow", "_BaseMap");
                        CopyTextureIfExists(mat, newMat, "_Noise", "_BaseMap");
                        CopyTextureIfExists(mat, newMat, "_Mask", "_BaseMap");
                        CopyTextureIfExists(mat, newMat, "_EmissionMap", "_EmissionMap");
                        
                        // Copiar color
                        if (mat.HasProperty("_Color"))
                        {
                            var color = mat.GetColor("_Color");
                            newMat.SetColor("_BaseColor", color);
                            newMat.SetColor("_Color", color);
                        }
                        if (mat.HasProperty("_BaseColor"))
                        {
                            newMat.SetColor("_BaseColor", mat.GetColor("_BaseColor"));
                        }
                        
                        // Copiar emisión si existe
                        if (mat.HasProperty("_Emission"))
                        {
                            float emission = mat.GetFloat("_Emission");
                            newMat.SetFloat("_EmissionIntensity", emission);
                            newMat.EnableKeyword("_EMISSION");
                        }
                        
                        // Configurar para partículas aditivas
                        newMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        newMat.SetFloat("_Surface", 1); // Transparent
                        newMat.SetFloat("_Blend", 1); // Additive
                        newMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        newMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                        newMat.SetFloat("_ZWrite", 0);
                        newMat.SetFloat("_Cull", 0); // No culling
                        newMat.renderQueue = 3000;
                        
                        // Color de emisión naranja/rojo para fuego por defecto
                        if (!mat.HasProperty("_Color") && !mat.HasProperty("_BaseColor"))
                        {
                            newMat.SetColor("_BaseColor", new Color(1f, 0.4f, 0f, 0.8f));
                            newMat.SetColor("_EmissionColor", new Color(2f, 0.5f, 0f, 1f));
                        }
                        
                        // Aplicar el material arreglado
                        renderer.material = newMat;
                        
                        fixedCount++;
                        if (logFixes)
                        {
                            Debug.Log($"[MaterialFixer] Fixed material '{mat.name}' (was: {originalShader}) on {renderer.gameObject.name}");
                        }
                    }
                }
            }
            
            if (logFixes)
            {
                Debug.Log($"[MaterialFixer] Fixed {fixedCount} materials on {gameObject.name}");
            }
        }
        
        void CopyTextureIfExists(Material source, Material dest, string sourceProp, string destProp)
        {
            if (source.HasProperty(sourceProp))
            {
                var tex = source.GetTexture(sourceProp);
                if (tex != null)
                {
                    dest.SetTexture(destProp, tex);
                }
            }
        }
        
        bool IsMaterialBroken(Material mat)
        {
            if (mat == null || mat.shader == null)
                return true;
            
            string shaderName = mat.shader.name;
            
            // Shader de error explícito
            if (shaderName == "Hidden/InternalErrorShader" ||
                shaderName == "" ||
                !mat.shader.isSupported)
            {
                return true;
            }
            
            // Shaders personalizados de KoreanTraditionalPattern que no existen
            // Estos son GUIDs de shaders de Shader Graph que no están en el proyecto
            if (shaderName.Contains("5946") || 
                shaderName.Contains("Shader Graphs") ||
                shaderName.Contains("VFX") ||
                shaderName.Contains("Korean") ||
                shaderName.Contains("Pattern"))
            {
                return true;
            }
            
            // Verificar si el shader tiene nombre genérico de shader graph
            if (shaderName.StartsWith("Shader Graphs/") || 
                shaderName.StartsWith("Hidden/") ||
                shaderName.Contains("-9") ||  // GUID-style shader names
                shaderName.Contains("-8") ||
                shaderName.Contains("-7"))
            {
                return true;
            }
            
            return false;
        }
    }
}
