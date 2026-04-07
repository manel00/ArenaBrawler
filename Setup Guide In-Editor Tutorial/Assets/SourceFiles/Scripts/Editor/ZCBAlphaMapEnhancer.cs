using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace ArenaEnhanced.Editor
{
    /// <summary>
    /// MEJORADOR DEL MAPA ZCB-ALPHA
    /// Mejora el mapa usando SOLO assets de terceros (sin Synty)
    /// Assets permitidos: TerrainDemoScene_URP, Stylized Nature MegaKit, KoreanTraditionalPattern_Effect
    /// </summary>
    public class ZCBAlphaMapEnhancer : EditorWindow
    {
        private bool enhanceTerrain = true;
        private bool addVegetation = true;
        private bool addRocks = true;
        private bool enhanceLighting = true;
        private int treeCount = 80;
        private int rockCount = 40;

        [MenuItem("Window/Arena Enhanced/ENHANCE ZCB-ALPHA MAP")]
        public static void ShowWindow()
        {
            var window = GetWindow<ZCBAlphaMapEnhancer>("ENHANCE ZCB-ALPHA", true);
            window.minSize = new Vector2(400, 350);
            window.maxSize = new Vector2(500, 400);
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            
            var titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 16;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("ZCB-ALPHA MAP ENHANCER", titleStyle);
            
            GUILayout.Space(5);
            
            EditorGUILayout.HelpBox(
                "Mejora el mapa ZCB-ALPHA usando:\n" +
                "• TerrainDemoScene_URP\n" +
                "• Stylized Nature MegaKit\n" +
                "• KoreanTraditionalPattern_Effect (fixed)\n\n" +
                "NO Synty - Solo assets verificados sin violetas", 
                MessageType.Info);
            
            GUILayout.Space(15);
            
            // Opciones
            GUILayout.Label("Enhancement Options:", EditorStyles.boldLabel);
            
            enhanceTerrain = GUILayout.Toggle(enhanceTerrain, "Enhance Terrain (TerrainDemoScene)");
            addVegetation = GUILayout.Toggle(addVegetation, "Add Vegetation (Stylized Nature)");
            addRocks = GUILayout.Toggle(addRocks, "Add Rocks (TerrainDemoScene)");
            enhanceLighting = GUILayout.Toggle(enhanceLighting, "Enhance Lighting & Atmosphere");
            
            GUILayout.Space(10);
            
            // Cantidades
            if (addVegetation)
            {
                GUILayout.Label($"Tree Count: {treeCount}");
                treeCount = Mathf.RoundToInt(GUILayout.HorizontalSlider(treeCount, 20, 150));
            }
            
            if (addRocks)
            {
                GUILayout.Label($"Rock Count: {rockCount}");
                rockCount = Mathf.RoundToInt(GUILayout.HorizontalSlider(rockCount, 10, 80));
            }
            
            GUILayout.Space(20);
            
            // Botón principal
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            if (GUILayout.Button("ENHANCE ZCB-ALPHA MAP NOW", GUILayout.Height(50)))
            {
                EnhanceMap();
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.Space(10);
            
            // Botón de verificación
            if (GUILayout.Button("Check for Violet Materials", GUILayout.Height(30)))
            {
                CheckForVioletMaterials();
            }
        }

        private void EnhanceMap()
        {
            int addedObjects = 0;
            
            // Crear grupo padre
            var mapGroup = new GameObject("ZCB_ALPHA_Enhanced");
            Undo.RegisterCreatedObjectUndo(mapGroup, "Enhance ZCB-ALPHA Map");
            
            // 1. Mejorar terreno
            if (enhanceTerrain)
            {
                addedObjects += EnhanceTerrain(mapGroup);
            }
            
            // 2. Añadir vegetación
            if (addVegetation)
            {
                addedObjects += AddVegetation(mapGroup);
            }
            
            // 3. Añadir rocas
            if (addRocks)
            {
                addedObjects += AddRocks(mapGroup);
            }
            
            // 4. Mejorar iluminación
            if (enhanceLighting)
            {
                EnhanceLighting();
            }
            
            EditorUtility.DisplayDialog("Map Enhanced!", 
                $"Successfully enhanced ZCB-ALPHA map!\n\n" +
                $"Added {addedObjects} objects.\n\n" +
                $"Terrain: {(enhanceTerrain ? "YES" : "NO")}\n" +
                $"Vegetation: {(addVegetation ? treeCount + " trees" : "NO")}\n" +
                $"Rocks: {(addRocks ? rockCount + " rocks" : "NO")}\n" +
                $"Lighting: {(enhanceLighting ? "YES" : "NO")}", 
                "OK");
            
            Debug.Log($"[ZCBAlphaMapEnhancer] Map enhanced with {addedObjects} objects");
        }

        private int EnhanceTerrain(GameObject parent)
        {
            int count = 0;
            
            // Usar TerrainDemoScene_URP para terreno base
            string[] terrainPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_02.prefab"
            };
            
            // Crear suelo base central (zona segura)
            var groundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(terrainPaths[0]);
            if (groundPrefab != null)
            {
                var ground = PrefabUtility.InstantiatePrefab(groundPrefab, parent.transform) as GameObject;
                ground.name = "ZCB_Ground_Center";
                ground.transform.position = Vector3.zero;
                ground.transform.localScale = new Vector3(30f, 1f, 30f);
                
                // Aplicar material de pasto verde
                var grassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Stylized_Grass.mat");
                if (grassMat == null)
                    grassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Material_Grass.mat");
                
                var renderers = ground.GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers)
                {
                    if (grassMat != null)
                    {
                        var mat = new Material(grassMat);
                        mat.SetColor("_BaseColor", new Color(0.35f, 0.6f, 0.35f));
                        rend.material = mat;
                    }
                }
                
                ground.tag = "Ground";
                count++;
            }
            
            // Crear anillos de terreno (zona de transición)
            for (int i = 0; i < 4; i++)
            {
                float angle = (i / 4f) * Mathf.PI * 2f;
                float radius = 25f;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(terrainPaths[i % 2]);
                if (prefab != null)
                {
                    var tile = PrefabUtility.InstantiatePrefab(prefab, parent.transform) as GameObject;
                    tile.name = $"ZCB_Ground_Mid_{i}";
                    tile.transform.position = pos;
                    tile.transform.localScale = new Vector3(20f, 1f, 20f);
                    
                    // Material mutado
                    var mutMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mutMat.SetColor("_BaseColor", new Color(0.4f, 0.55f, 0.4f));
                    
                    var renderers = tile.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers) rend.material = mutMat;
                    
                    tile.tag = "Ground";
                    count++;
                }
            }
            
            return count;
        }

        private int AddVegetation(GameObject parent)
        {
            int count = 0;
            
            // Usar Stylized Nature MegaKit
            string[] treePaths = {
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/CommonTree_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/CommonTree_2.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/CommonTree_3.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/CommonTree_4.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/CommonTree_5.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pine_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pine_2.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pine_3.fbx"
            };
            
            string[] bushPaths = {
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Bush_Common.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Fern_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Grass_Common_Short.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Grass_Common_Tall.fbx"
            };
            
            // Árboles - zona segura y transición
            for (int i = 0; i < treeCount; i++)
            {
                string path = treePaths[Random.Range(0, treePaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                if (prefab != null)
                {
                    // Posición aleatoria, evitando centro
                    Vector3 pos = GetRandomPositionAvoidingCenter(15f, 45f);
                    
                    var tree = PrefabUtility.InstantiatePrefab(prefab, parent.transform) as GameObject;
                    tree.name = $"ZCB_Tree_{i}";
                    tree.transform.position = pos;
                    tree.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                    
                    float s = Random.Range(0.8f, 1.6f);
                    tree.transform.localScale = new Vector3(s, s * Random.Range(0.9f, 1.3f), s);
                    
                    // Añadir componente destructible si existe
                    if (tree.GetComponent<Collider>() == null)
                    {
                        tree.AddComponent<MeshCollider>().convex = true;
                    }
                    
                    count++;
                }
            }
            
            // Arbustos/hierba
            for (int i = 0; i < treeCount / 2; i++)
            {
                string path = bushPaths[Random.Range(0, bushPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                if (prefab != null)
                {
                    Vector3 pos = GetRandomPositionAvoidingCenter(12f, 48f);
                    
                    var bush = PrefabUtility.InstantiatePrefab(prefab, parent.transform) as GameObject;
                    bush.name = $"ZCB_Bush_{i}";
                    bush.transform.position = pos;
                    bush.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                    bush.transform.localScale = Vector3.one * Random.Range(0.7f, 1.4f);
                    
                    count++;
                }
            }
            
            return count;
        }

        private int AddRocks(GameObject parent)
        {
            int count = 0;
            
            // Usar TerrainDemoScene_URP para rocas
            string[] rockPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_A_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_A_02.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_B_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_B_02.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_C_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_C_02.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_Overgrown_A.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_Overgrown_B.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_Overgrown_C.prefab"
            };
            
            for (int i = 0; i < rockCount; i++)
            {
                string path = rockPaths[Random.Range(0, rockPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                if (prefab != null)
                {
                    Vector3 pos = GetRandomPositionAvoidingCenter(10f, 50f);
                    
                    var rock = PrefabUtility.InstantiatePrefab(prefab, parent.transform) as GameObject;
                    rock.name = $"ZCB_Rock_{i}";
                    rock.transform.position = pos;
                    rock.transform.rotation = Quaternion.Euler(
                        Random.Range(-15f, 15f), 
                        Random.Range(0, 360), 
                        Random.Range(-15f, 15f)
                    );
                    
                    float s = Random.Range(0.6f, 2f);
                    rock.transform.localScale = Vector3.one * s;
                    
                    // Asegurar que tenga collider
                    if (rock.GetComponent<Collider>() == null)
                    {
                        rock.AddComponent<MeshCollider>().convex = true;
                    }
                    
                    count++;
                }
            }
            
            return count;
        }

        private void EnhanceLighting()
        {
            // Configurar iluminación ambiental
            RenderSettings.ambientLight = new Color(0.3f, 0.45f, 0.3f, 1f);
            RenderSettings.ambientIntensity = 0.8f;
            
            // Configurar niebla
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.4f, 0.6f, 0.45f, 1f);
            RenderSettings.fogDensity = 0.008f;
            RenderSettings.fogMode = FogMode.Exponential;
            
            // Buscar o crear luz direccional principal
            var sun = GameObject.Find("Directional Light") ?? GameObject.Find("Sun");
            if (sun == null)
            {
                sun = new GameObject("ZCB_DirectionalLight");
                var light = sun.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                light.color = new Color(1f, 0.95f, 0.8f);
                light.shadows = LightShadows.Soft;
                sun.transform.rotation = Quaternion.Euler(50, 30, 0);
            }
            
            Debug.Log("[ZCBAlphaMapEnhancer] Lighting enhanced");
        }

        private void CheckForVioletMaterials()
        {
            int violetCount = 0;
            var allRenderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include);
            
            foreach (var renderer in allRenderers)
            {
                if (renderer.sharedMaterials == null) continue;
                
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat != null && mat.shader != null)
                    {
                        if (mat.shader.name == "Hidden/InternalErrorShader" ||
                            string.IsNullOrEmpty(mat.shader.name) ||
                            !mat.shader.isSupported)
                        {
                            violetCount++;
                            Debug.LogWarning($"VIOLET MATERIAL FOUND: {mat.name} on {renderer.gameObject.name}");
                        }
                    }
                }
            }
            
            if (violetCount > 0)
            {
                EditorUtility.DisplayDialog("Violet Materials Found", 
                    $"Found {violetCount} violet materials in scene!\n\n" +
                    "Use 'Window > Arena Enhanced > ELIMINATE VIOLET MATERIALS' to fix them.", 
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("No Violet Materials", 
                    "No violet materials found! All materials are working correctly.", "OK");
            }
        }

        private Vector3 GetRandomPositionAvoidingCenter(float minRadius, float maxRadius)
        {
            for (int i = 0; i < 50; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float radius = Random.Range(minRadius, maxRadius);
                
                Vector3 pos = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0,
                    Mathf.Sin(angle) * radius
                );
                
                if (pos.magnitude > minRadius)
                    return pos;
            }
            
            return new Vector3(maxRadius, 0, 0);
        }
    }
}
