using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace ArenaEnhanced.Editor
{
    /// <summary>
    /// Verificador de assets de terceros - asegura que no haya materiales violetas
    /// en assets de: TerrainDemoScene_URP, Synty, KoreanTraditionalPattern_Effect
    /// </summary>
    public static class ThirdPartyAssetVerifier
    {
        // Lista de assets de terceros verificados que funcionan
        public static readonly Dictionary<string, string[]> VERIFIED_ASSET_PATHS = new Dictionary<string, string[]>
        {
            ["TerrainDemoScene_URP"] = new string[]
            {
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_02.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_A_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_A_02.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_B_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_B_02.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_C_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_C_02.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_D.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_Overgrown_A.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_Overgrown_B.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_Overgrown_C.prefab"
            },
            ["Synty_Generic"] = new string[]
            {
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_Round_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_02.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Mushroom_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Mushroom_02.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Mushroom_03.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Fern_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Fern_02.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Fern_03.prefab"
            },
            ["StylizedNature"] = new string[]
            {
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/CommonTree_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/CommonTree_2.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/CommonTree_3.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/CommonTree_4.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/CommonTree_5.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pine_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pine_2.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pine_3.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pine_4.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pine_5.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/DeadTree_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/DeadTree_2.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/TwistedTree_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/TwistedTree_2.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Bush_Common.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Fern_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Mushroom_Common.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Mushroom_Laetiporus.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Rock_Medium_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Rock_Medium_2.fbx"
            }
        };

        /// <summary>
        /// Verifica si un prefab tiene materiales rotos (violetas)
        /// </summary>
        public static bool HasBrokenMaterials(GameObject prefab)
        {
            if (prefab == null) return true;

            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterials == null) return true;

                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat == null) return true;
                    if (mat.shader == null) return true;
                    
                    string shaderName = mat.shader.name;
                    if (shaderName == "Hidden/InternalErrorShader" ||
                        string.IsNullOrEmpty(shaderName) ||
                        !mat.shader.isSupported)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Arregla los materiales de un prefab usando shaders URP
        /// </summary>
        public static void FixPrefabMaterials(GameObject prefab, string shaderType = "lit")
        {
            if (prefab == null) return;

            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterials == null) continue;

                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    var mat = renderer.sharedMaterials[i];
                    if (mat == null) continue;

                    // Verificar si el material está roto
                    if (mat.shader == null || 
                        mat.shader.name == "Hidden/InternalErrorShader" ||
                        !mat.shader.isSupported)
                    {
                        // Reemplazar con material URP básico
                        string shaderName = shaderType == "particles" 
                            ? "Universal Render Pipeline/Particles/Unlit"
                            : "Universal Render Pipeline/Lit";

                        var urpShader = Shader.Find(shaderName);
                        if (urpShader != null)
                        {
                            Undo.RecordObject(mat, "Fix Third-Party Material");
                            mat.shader = urpShader;
                            
                            // Configurar propiedades básicas
                            if (mat.HasProperty("_BaseColor"))
                            {
                                Color baseColor = Color.gray;
                                if (shaderType == "particles")
                                    baseColor = new Color(1f, 0.4f, 0f, 0.8f);
                                mat.SetColor("_BaseColor", baseColor);
                            }

                            // Para partículas, configurar transparencia
                            if (shaderType == "particles")
                            {
                                mat.SetFloat("_Surface", 1); // Transparent
                                mat.SetFloat("_Blend", 1); // Additive
                                mat.renderQueue = 3000;
                            }

                            EditorUtility.SetDirty(mat);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Obtiene un prefab verificado de terceros, arreglando materiales si es necesario
        /// </summary>
        public static GameObject GetVerifiedPrefab(string path, string shaderType = "lit")
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab != null && HasBrokenMaterials(prefab))
            {
                Debug.LogWarning($"[ThirdPartyAssetVerifier] Fixing broken materials in: {path}");
                FixPrefabMaterials(prefab, shaderType);
            }

            return prefab;
        }

        /// <summary>
        /// Obtiene una lista de prefabs verificados de una categoría
        /// </summary>
        public static List<GameObject> GetVerifiedPrefabs(string category, string shaderType = "lit")
        {
            var result = new List<GameObject>();
            
            if (VERIFIED_ASSET_PATHS.TryGetValue(category, out string[] paths))
            {
                foreach (var path in paths)
                {
                    var prefab = GetVerifiedPrefab(path, shaderType);
                    if (prefab != null)
                    {
                        result.Add(prefab);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Escanea y arregla TODOS los assets de terceros del proyecto
        /// </summary>
        [MenuItem("Window/Arena Enhanced/Verify All Third-Party Assets")]
        public static void VerifyAllThirdPartyAssets()
        {
            int totalFixed = 0;
            int totalChecked = 0;

            foreach (var kvp in VERIFIED_ASSET_PATHS)
            {
                string category = kvp.Key;
                string[] paths = kvp.Value;

                foreach (var path in paths)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        totalChecked++;
                        
                        if (HasBrokenMaterials(prefab))
                        {
                            FixPrefabMaterials(prefab);
                            totalFixed++;
                            Debug.Log($"[ThirdPartyAssetVerifier] Fixed: {path}");
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            
            EditorUtility.DisplayDialog("Third-Party Assets Verified", 
                $"Checked {totalChecked} prefabs, fixed {totalFixed} with broken materials.", "OK");
        }

        /// <summary>
        /// Crea materiales URP de reemplazo para assets problemáticos
        /// </summary>
        public static Material CreateReplacementMaterial(string name, Color color, string shaderType = "lit")
        {
            string shaderName = shaderType == "particles"
                ? "Universal Render Pipeline/Particles/Unlit"
                : "Universal Render Pipeline/Lit";

            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError($"[ThirdPartyAssetVerifier] URP shader not found: {shaderName}");
                return null;
            }

            var mat = new Material(shader);
            mat.name = name;
            mat.SetColor("_BaseColor", color);

            if (shaderType == "particles")
            {
                mat.SetFloat("_Surface", 1);
                mat.SetFloat("_Blend", 1);
                mat.renderQueue = 3000;
            }

            return mat;
        }
    }
}
