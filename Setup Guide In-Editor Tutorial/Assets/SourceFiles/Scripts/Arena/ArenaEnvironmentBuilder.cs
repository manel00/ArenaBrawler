using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ArenaEnhanced
{
    /// <summary>
    /// Módulo dedicado a la construcción de entornos de mapas
    /// </summary>
    public static class ArenaEnvironmentBuilder
    {
        // Materiales compartidos para entornos
        private static Material groundMat;
        private static Material rockyMat;
        private static Material terrainMat;
        private static Material stoneMat;
        private static Material blackSandMat;
        private static Material mossMat;
        
        public static void BuildEnvironment(string mapId)
        {
            switch (mapId)
            {
                case "forest":
                case "forestarena":
                case "forestvalley":
                    // Usar versión runtime que funciona en builds
                    BuildForestEnvironmentRuntime();
                    break;
                case "rocky":
                case "rockycanyon":
                    BuildRockyCanyonEnvironment();
                    break;
                case "deadwoods":
                    BuildDeadWoodsEnvironment();
                    break;
                case "mushroom":
                case "mushroomgrove":
                    BuildMushroomGroveEnvironment();
                    break;
                case "water":
                case "waterarena":
                    BuildHydroEnvironmentPremium();
                    break;
                case "korean":
                case "koreantemple":
                    BuildSanctumEnvironmentPremium();
                    break;
                case "volcanic":
                    BuildVolcanicEnvironmentPremium();
                    break;
                case "original":
                case "alpha":
                default:
                    BuildAlphaEnvironmentPremium();
                    break;
            }
        }

        private static void BuildOriginalEnvironment()
        {
#if UNITY_EDITOR
            var envGroup = new GameObject("Environment_ForestValley");
            
            // Low-poly forest atmosphere
            RenderSettings.ambientLight = new Color(0.3f, 0.45f, 0.3f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.4f, 0.6f, 0.45f, 1f);
            RenderSettings.fogDensity = 0.008f;
            RenderSettings.fogMode = FogMode.Exponential;
            
            // Ground materials for valley
            var grassMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            grassMat.color = new Color(0.35f, 0.6f, 0.35f);
            
            var riverMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            riverMat.color = new Color(0.2f, 0.5f, 0.7f, 0.8f);
            riverMat.SetFloat("_Smoothness", 0.8f);
            
            var mountainMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mountainMat.color = new Color(0.25f, 0.5f, 0.3f);
            
            // Main valley floor
            string[] groundPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_02.prefab"
            };
            
            var groundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(groundPaths[0]);
            if (groundPrefab != null)
            {
                var ground = Object.Instantiate(groundPrefab, Vector3.zero, Quaternion.identity, envGroup.transform);
                ground.name = "ValleyFloor";
                ground.transform.localScale = new Vector3(70f, 1f, 70f);
                var renderers = ground.GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers) rend.material = grassMat;
                ground.tag = "Ground";
            }
            
            // Build surrounding mountains (circular range)
            BuildLowPolyMountains(envGroup, mountainMat);
            
            // Build winding river through center
            BuildWindingRiver(envGroup, riverMat);
            
            // Build lakes/ponds
            BuildForestLakes(envGroup, riverMat);
            
            // Dense forest vegetation
            BuildDenseForest(envGroup);
            
            // Add rocks and details
            BuildForestDetails(envGroup);
#endif
        }
        
        private static void BuildLowPolyMountains(GameObject parent, Material mountainMat)
        {
            // Create mountains around the perimeter using rocks from TerrainDemoScene
            string[] mountainPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_A_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_A_02.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_B_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_B_02.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_C_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_C_02.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_D.prefab"
            };
            
            // Circular mountain range
            int mountainCount = 40;
            float baseRadius = 55f;
            
            for (int i = 0; i < mountainCount; i++)
            {
                float angle = (i / (float)mountainCount) * Mathf.PI * 2f;
                float radius = baseRadius + Random.Range(-8f, 15f);
                float height = Random.Range(15f, 40f);
                
                Vector3 pos = new Vector3(
                    Mathf.Cos(angle) * radius,
                    height / 2f,
                    Mathf.Sin(angle) * radius
                );
                
                var mountainPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(mountainPaths[Random.Range(0, mountainPaths.Length)]);
                if (mountainPrefab != null)
                {
                    var mountain = Object.Instantiate(mountainPrefab, pos, Quaternion.Euler(
                        Random.Range(-25f, 25f), 
                        angle * Mathf.Rad2Deg + Random.Range(-30f, 30f), 
                        Random.Range(-15f, 15f)
                    ), parent.transform);
                    
                    mountain.name = "Mountain_" + i;
                    float scaleX = Random.Range(8f, 18f);
                    float scaleY = Random.Range(12f, 25f);
                    float scaleZ = Random.Range(8f, 18f);
                    mountain.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
                    
                    var renderers = mountain.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var mat = new Material(mountainMat);
                        // Vary mountain colors slightly
                        float variation = Random.Range(-0.1f, 0.1f);
                        mat.color = new Color(
                            mountainMat.color.r + variation,
                            mountainMat.color.g + variation,
                            mountainMat.color.b + variation
                        );
                        rend.material = mat;
                    }
                }
            }
        }
        
        private static void BuildWindingRiver(GameObject parent, Material riverMat)
        {
            // Create a winding river through the center using terrain tiles
            string[] waterPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_02.prefab"
            };
            
            // River follows a curve through the arena
            int riverSegments = 12;
            float riverWidth = 8f;
            
            for (int i = 0; i < riverSegments; i++)
            {
                float t = i / (float)(riverSegments - 1);
                
                // Create a winding path
                float x = Mathf.Lerp(-40f, 40f, t);
                float z = Mathf.Sin(t * Mathf.PI * 2f) * 20f + Mathf.Cos(t * Mathf.PI * 1.5f) * 10f;
                
                Vector3 pos = new Vector3(x, -0.3f, z);
                
                var waterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(waterPaths[i % 2]);
                if (waterPrefab != null)
                {
                    var riverSegment = Object.Instantiate(waterPrefab, pos, Quaternion.identity, parent.transform);
                    riverSegment.name = "RiverSegment_" + i;
                    riverSegment.transform.localScale = new Vector3(riverWidth, 0.8f, 12f);
                    
                    var renderers = riverSegment.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers) rend.material = riverMat;
                }
            }
        }
        
        private static void BuildForestLakes(GameObject parent, Material lakeMat)
        {
            // Add 2-3 small lakes/ponds
            string[] waterPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_02.prefab"
            };
            
            Vector3[] lakePositions = {
                new Vector3(-25f, -0.2f, -25f),
                new Vector3(30f, -0.2f, 20f),
                new Vector3(-15f, -0.2f, 35f)
            };
            
            float[] lakeSizes = { 12f, 10f, 8f };
            
            for (int i = 0; i < lakePositions.Length; i++)
            {
                var lakePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(waterPaths[i % 2]);
                if (lakePrefab != null)
                {
                    var lake = Object.Instantiate(lakePrefab, lakePositions[i], Quaternion.identity, parent.transform);
                    lake.name = "ForestLake_" + i;
                    float size = lakeSizes[i];
                    lake.transform.localScale = new Vector3(size, 0.8f, size * Random.Range(0.7f, 1.3f));
                    
                    // Make it circular-ish
                    lake.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                    
                    var renderers = lake.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var mat = new Material(lakeMat);
                        mat.color = new Color(0.25f, 0.55f, 0.75f, 0.85f);
                        rend.material = mat;
                    }
                }
            }
        }
        
        private static void BuildDenseForest(GameObject parent)
        {
            // Dense forest with varied trees
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
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Fern_1.fbx"
            };
            
            // Dense trees - 150 trees
            for (int i = 0; i < 150; i++)
            {
                string path = treePaths[Random.Range(0, treePaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    // Keep center area clear for combat (30m radius)
                    Vector3 pos = GetRandomPositionWithoutCenter(30f, 50f);
                    
                    // Avoid placing trees in the river area (roughly)
                    if (Mathf.Abs(pos.z) < 15f && Mathf.Abs(pos.x) < 40f) continue;
                    
                    var tree = Object.Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), parent.transform);
                    tree.name = "ForestTree_" + i;
                    
                    float scale = Random.Range(0.9f, 1.8f);
                    float heightScale = scale * Random.Range(0.8f, 1.4f);
                    tree.transform.localScale = new Vector3(scale, heightScale, scale);
                    
                    tree.AddComponent<DestructibleEnvironment>();
                }
            }
            
            // Underbrush - 80 bushes/ferns
            for (int i = 0; i < 80; i++)
            {
                string path = bushPaths[Random.Range(0, bushPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    Vector3 pos = GetRandomPositionWithoutCenter(25f, 50f);
                    
                    // Avoid river
                    if (Mathf.Abs(pos.z) < 12f && Mathf.Abs(pos.x) < 35f) continue;
                    
                    var bush = Object.Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), parent.transform);
                    bush.name = "Underbrush_" + i;
                    bush.transform.localScale = Vector3.one * Random.Range(0.7f, 1.4f);
                    bush.AddComponent<DestructibleEnvironment>();
                }
            }
        }
        
        private static void BuildForestDetails(GameObject parent)
        {
            // Add rocks and flowers scattered around
            string[] rockPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_Overgrown_A.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_Overgrown_B.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_Overgrown_C.prefab",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Rock_Medium_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Rock_Medium_2.fbx"
            };
            
            string[] flowerPaths = {
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Flower_1_Group.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Flower_2_Group.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Flower_3_Group.fbx"
            };
            
            // Rocks - 40 scattered
            for (int i = 0; i < 40; i++)
            {
                string path = rockPaths[Random.Range(0, rockPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    Vector3 pos = GetRandomPositionWithoutCenter(20f, 55f);
                    var rock = Object.Instantiate(prefab, pos, Quaternion.Euler(
                        Random.Range(-20f, 20f), 
                        Random.Range(0, 360), 
                        Random.Range(-20f, 20f)
                    ), parent.transform);
                    
                    rock.name = "ForestRock_" + i;
                    rock.transform.localScale = Vector3.one * Random.Range(0.8f, 2f);
                    rock.AddComponent<DestructibleEnvironment>();
                }
            }
            
            // Flowers - 50 patches
            for (int i = 0; i < 50; i++)
            {
                string path = flowerPaths[Random.Range(0, flowerPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    Vector3 pos = GetRandomPositionWithoutCenter(15f, 50f);
                    var flowers = Object.Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), parent.transform);
                    
                    flowers.name = "Wildflowers_" + i;
                    flowers.transform.localScale = Vector3.one * Random.Range(0.6f, 1.2f);
                }
            }
        }


        private static void BuildForestArenaEnvironment()
        {
#if UNITY_EDITOR
            var envGroup = new GameObject("Environment_ForestArena");
            
            RenderSettings.ambientLight = new Color(0.2f, 0.35f, 0.2f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.15f, 0.25f, 0.18f, 1f);
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.fogMode = FogMode.Exponential;
            
            // Use TerrainDemoScene terrain prefabs for ground instead of procedural plane
            string[] groundPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_02.prefab"
            };
            
            var groundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(groundPaths[0]);
            if (groundPrefab != null)
            {
                var ground = Object.Instantiate(groundPrefab, Vector3.zero, Quaternion.identity, envGroup.transform);
                ground.name = "ForestArenaGround";
                ground.transform.localScale = new Vector3(55f, 1f, 55f);
                if (groundMat != null)
                {
                    var renderers = ground.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers) rend.material = groundMat;
                }
            }

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
            
            for (int i = 0; i < 100; i++)
            {
                string path = treePaths[Random.Range(0, treePaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    Vector3 pos = GetRandomPositionWithoutCenter(12f, 50f);
                    var tree = Object.Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), envGroup.transform);
                    float s = Random.Range(0.9f, 1.6f);
                    tree.transform.localScale = new Vector3(s, s * Random.Range(0.9f, 1.3f), s);
                    
                    tree.AddComponent<DestructibleEnvironment>();
                }
            }

            string[] bushPaths = {
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Bush_Common.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Fern_1.fbx"
            };
            
            for (int i = 0; i < 40; i++)
            {
                string path = bushPaths[Random.Range(0, bushPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    Vector3 pos = GetRandomPositionWithoutCenter(10f, 48f);
                    var bush = Object.Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), envGroup.transform);
                    float s = Random.Range(0.6f, 1.2f);
                    bush.transform.localScale = Vector3.one * s;
                    bush.AddComponent<DestructibleEnvironment>();
                }
            }
            
            Debug.Log("[ArenaEnvironmentBuilder] Forest Arena environment built.");
#endif
        }

        private static void BuildRockyCanyonEnvironment()
        {
#if UNITY_EDITOR
            var envGroup = new GameObject("Environment_RockyCanyon");
            
            RenderSettings.ambientLight = new Color(0.25f, 0.22f, 0.18f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.18f, 0.15f, 0.12f, 1f);
            RenderSettings.fogDensity = 0.018f;
            RenderSettings.fogMode = FogMode.Exponential;
            
            // Use TerrainDemoScene terrain prefabs for rocky ground
            string[] groundPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_02.prefab"
            };
            
            var groundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(groundPaths[0]);
            if (groundPrefab != null)
            {
                var ground = Object.Instantiate(groundPrefab, Vector3.zero, Quaternion.identity, envGroup.transform);
                ground.name = "RockyCanyonGround";
                ground.transform.localScale = new Vector3(60f, 1f, 60f);
                
                var renderers = ground.GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers) rend.material = rockyMat;
            }

            string[] rockPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_A_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_A_02.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_B_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_B_02.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_C_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_C_02.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_D.prefab",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Rock_Medium_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Rock_Medium_2.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Rock_Medium_3.fbx"
            };
            
            for (int i = 0; i < 90; i++)
            {
                string path = rockPaths[Random.Range(0, rockPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    Vector3 pos = GetRandomPositionWithoutCenter(8f, 52f);
                    float yRot = Random.Range(0, 360);
                    float xRot = Random.Range(-15f, 15f);
                    float zRot = Random.Range(-15f, 15f);
                    
                    var rock = Object.Instantiate(prefab, pos, Quaternion.Euler(xRot, yRot, zRot), envGroup.transform);
                    float s = Random.Range(0.8f, 3f);
                    rock.transform.localScale = new Vector3(s, s * Random.Range(0.6f, 1.8f), s);
                    
                    rock.AddComponent<DestructibleEnvironment>();
                }
            }

            // Use Synty floor assets for elevated platforms instead of procedural cubes
            string[] platformPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_Round_01.prefab"
            };
            
            for (int i = 0; i < 15; i++)
            {
                Vector3 pos = GetRandomPositionWithoutCenter(15f, 45f);
                float height = Random.Range(2f, 6f);
                
                var platformPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(platformPaths[Random.Range(0, platformPaths.Length)]);
                if (platformPrefab != null)
                {
                    var platform = Object.Instantiate(platformPrefab, pos + Vector3.up * (height / 2f), Quaternion.identity, envGroup.transform);
                    platform.name = "ElevatedPlatform";
                    platform.transform.localScale = new Vector3(Random.Range(4f, 8f), height, Random.Range(4f, 8f));
                    
                    var renderers = platform.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers) rend.material = rockyMat;
                    
                    platform.AddComponent<DestructibleEnvironment>();
                }
            }
            
            Debug.Log("[ArenaEnvironmentBuilder] Rocky Canyon environment built.");
#endif
        }

        private static void BuildDeadWoodsEnvironment()
        {
#if UNITY_EDITOR
            var envGroup = new GameObject("Environment_DeadWoods");
            
            RenderSettings.ambientLight = new Color(0.08f, 0.08f, 0.1f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.06f, 0.06f, 0.08f, 1f);
            RenderSettings.fogDensity = 0.035f;
            RenderSettings.fogMode = FogMode.Exponential;
            
            // Use TerrainDemoScene terrain prefabs for ground instead of procedural cube
            string[] groundPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_02.prefab"
            };
            
            var groundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(groundPaths[0]);
            if (groundPrefab != null)
            {
                var ground = Object.Instantiate(groundPrefab, Vector3.zero, Quaternion.identity, envGroup.transform);
                ground.name = "DeadWoodsGround";
                ground.transform.localScale = new Vector3(120f, 1f, 120f);
                if (terrainMat != null)
                {
                    var renderers = ground.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers) rend.material = terrainMat;
                }
                else
                {
                    var renderers = ground.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        Material groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        groundMat.color = new Color(0.12f, 0.1f, 0.08f);
                        rend.material = groundMat;
                    }
                }
                
                // Ensure solid collider
                var cols = ground.GetComponentsInChildren<Collider>();
                foreach (var col in cols) if (col != null) col.enabled = true;
                
                ground.tag = "Ground";
            }

            string[] deadTreePaths = {
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/DeadTree_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/DeadTree_2.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/DeadTree_3.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/DeadTree_4.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/DeadTree_5.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/TwistedTree_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/TwistedTree_2.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/TwistedTree_3.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/TwistedTree_4.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/TwistedTree_5.fbx"
            };
            
            for (int i = 0; i < 80; i++)
            {
                string path = deadTreePaths[Random.Range(0, deadTreePaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    Vector3 pos = GetRandomPositionWithoutCenter(10f, 48f);
                    float rotY = Random.Range(0, 360);
                    float rotX = Random.Range(-10f, 10f);
                    float rotZ = Random.Range(-10f, 10f);
                    
                    var tree = Object.Instantiate(prefab, pos, Quaternion.Euler(rotX, rotY, rotZ), envGroup.transform);
                    float s = Random.Range(0.8f, 1.8f);
                    tree.transform.localScale = new Vector3(s, s * Random.Range(0.7f, 1.4f), s);
                    
                    tree.AddComponent<DestructibleEnvironment>();
                }
            }

            string[] pebblePaths = {
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Round_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Square_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Square_2.fbx"
            };
            
            for (int i = 0; i < 60; i++)
            {
                string path = pebblePaths[Random.Range(0, pebblePaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    Vector3 pos = GetRandomPositionWithoutCenter(5f, 50f);
                    var pebble = Object.Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), envGroup.transform);
                    float s = Random.Range(0.5f, 1.5f);
                    pebble.transform.localScale = Vector3.one * s;
                }
            }
            
            Debug.Log("[ArenaEnvironmentBuilder] Dead Woods environment built.");
#endif
        }

        private static void BuildMushroomGroveEnvironment()
        {
#if UNITY_EDITOR
            var envGroup = new GameObject("Environment_MushroomGrove");
            
            RenderSettings.ambientLight = new Color(0.25f, 0.2f, 0.35f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.2f, 0.15f, 0.25f, 1f);
            RenderSettings.fogDensity = 0.02f;
            RenderSettings.fogMode = FogMode.Exponential;
            
            // Use TerrainDemoScene terrain prefabs for ground instead of procedural plane
            string[] groundPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_02.prefab"
            };
            
            var groundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(groundPaths[0]);
            if (groundPrefab != null)
            {
                var ground = Object.Instantiate(groundPrefab, Vector3.zero, Quaternion.identity, envGroup.transform);
                ground.name = "MushroomGroveGround";
                ground.transform.localScale = new Vector3(50f, 1f, 50f);
                if (groundMat != null)
                {
                    var renderers = ground.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers) rend.material = groundMat;
                }
            }

            string[] mushroomPaths = {
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Mushroom_Common.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Mushroom_Laetiporus.fbx"
            };
            
            for (int cluster = 0; cluster < 25; cluster++)
            {
                Vector3 clusterCenter = GetRandomPositionWithoutCenter(12f, 45f);
                int mushroomsInCluster = Random.Range(3, 8);
                
                for (int i = 0; i < mushroomsInCluster; i++)
                {
                    string path = mushroomPaths[Random.Range(0, mushroomPaths.Length)];
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        Vector3 offset = Random.insideUnitSphere * 4f;
                        offset.y = 0;
                        Vector3 pos = clusterCenter + offset;
                        
                        var mushroom = Object.Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), envGroup.transform);
                        float s = Random.Range(1.2f, 2.5f);
                        mushroom.transform.localScale = Vector3.one * s;
                        
                        var capCol = mushroom.AddComponent<CapsuleCollider>();
                        capCol.radius = 0.5f;
                        capCol.height = 2f;
                        capCol.center = new Vector3(0, 1f, 0);
                        mushroom.AddComponent<DestructibleEnvironment>();
                    }
                }
            }

            string[] flowerPaths = {
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Flower_3_Group.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Flower_4_Group.fbx"
            };
            
            for (int i = 0; i < 30; i++)
            {
                string path = flowerPaths[Random.Range(0, flowerPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    Vector3 pos = GetRandomPositionWithoutCenter(10f, 48f);
                    var flower = Object.Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), envGroup.transform);
                    float s = Random.Range(0.8f, 1.4f);
                    flower.transform.localScale = Vector3.one * s;
                }
            }
            
            Debug.Log("[ArenaEnvironmentBuilder] Mushroom Grove environment built.");
#endif
        }

        private static void BuildWaterArenaEnvironment()
        {
#if UNITY_EDITOR
            var envGroup = new GameObject("Environment_WaterArena");
            
            RenderSettings.ambientLight = new Color(0.2f, 0.3f, 0.4f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.15f, 0.25f, 0.35f, 1f);
            RenderSettings.fogDensity = 0.015f;
            RenderSettings.fogMode = FogMode.Exponential;
            
            Material sandMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            sandMat.color = new Color(0.65f, 0.6f, 0.45f);
            
            // Use TerrainDemoScene terrain prefabs for water tiles instead of procedural planes
            string[] waterPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_02.prefab"
            };
            
            for (int ix = -2; ix <= 2; ix++)
            {
                for (int iz = -2; iz <= 2; iz++)
                {
                    if (Mathf.Abs(ix) <= 1 && Mathf.Abs(iz) <= 1) continue;
                    
                    var waterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(waterPaths[Random.Range(0, waterPaths.Length)]);
                    if (waterPrefab != null)
                    {
                        var waterTile = Object.Instantiate(waterPrefab, new Vector3(ix * 25f, -0.5f, iz * 25f), Quaternion.identity, envGroup.transform);
                        waterTile.name = $"Water_{ix}_{iz}";
                        waterTile.transform.localScale = new Vector3(25f, 1f, 25f);
                        
                        Material waterMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        waterMat.color = new Color(0.1f, 0.3f, 0.5f, 0.7f);
                        waterMat.SetFloat("_Smoothness", 0.9f);
                        var renderers = waterTile.GetComponentsInChildren<Renderer>();
                        foreach (var rend in renderers) rend.material = waterMat;
                    }
                }
            }

            string[] islandPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_02.prefab"
            };
            
            for (int i = 0; i < 9; i++)
            {
                string path = islandPaths[Random.Range(0, islandPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    float angle = (i / 9f) * Mathf.PI * 2f;
                    float radius = Random.Range(25f, 40f);
                    Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                    
                    var island = Object.Instantiate(prefab, pos, Quaternion.identity, envGroup.transform);
                    float s = Random.Range(0.8f, 1.5f);
                    island.transform.localScale = new Vector3(s, 1f, s);
                }
            }

            // Use Synty floor assets for main platform instead of procedural cylinder
            string[] platformPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_Round_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_01.prefab"
            };
            
            var platformPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(platformPaths[Random.Range(0, platformPaths.Length)]);
            if (platformPrefab != null)
            {
                var mainPlatform = Object.Instantiate(platformPrefab, Vector3.zero, Quaternion.identity, envGroup.transform);
                mainPlatform.name = "MainWaterPlatform";
                mainPlatform.transform.localScale = new Vector3(35f, 0.5f, 35f);
                
                var renderers = mainPlatform.GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers) rend.material = sandMat;
            }
            
            string[] rockPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_Overgrown_A.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_Overgrown_B.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_Overgrown_C.prefab"
            };
            
            for (int i = 0; i < 25; i++)
            {
                string path = rockPaths[Random.Range(0, rockPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    Vector3 pos = GetRandomPositionWithoutCenter(10f, 45f);
                    var rock = Object.Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), envGroup.transform);
                    float s = Random.Range(0.6f, 1.5f);
                    rock.transform.localScale = Vector3.one * s;
                    rock.AddComponent<DestructibleEnvironment>();
                }
            }
            
            Debug.Log("[ArenaEnvironmentBuilder] Water Arena environment built.");
#endif
        }

        private static void BuildKoreanTempleEnvironment()
        {
#if UNITY_EDITOR
            var envGroup = new GameObject("Environment_KoreanTemple");
            
            RenderSettings.ambientLight = new Color(0.3f, 0.28f, 0.25f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.25f, 0.22f, 0.18f, 1f);
            RenderSettings.fogDensity = 0.025f;
            RenderSettings.fogMode = FogMode.Exponential;
            
            // Use TerrainDemoScene terrain prefabs for temple ground instead of procedural plane
            string[] groundPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_02.prefab"
            };
            
            var groundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(groundPaths[0]);
            if (groundPrefab != null)
            {
                var ground = Object.Instantiate(groundPrefab, Vector3.zero, Quaternion.identity, envGroup.transform);
                ground.name = "TempleGround";
                ground.transform.localScale = new Vector3(50f, 1f, 50f);
                
                var renderers = ground.GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers) rend.material = stoneMat;
            }

            // Use Synty building assets for temple buildings instead of procedural cubes
            string[] buildingPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_Round_01.prefab"
            };
            string[] roofPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_02.prefab"
            };
            
            for (int i = 0; i < 4; i++)
            {
                float angle = (i / 4f) * Mathf.PI * 2f;
                float radius = 30f;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                
                var buildingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(buildingPaths[Random.Range(0, buildingPaths.Length)]);
                if (buildingPrefab != null)
                {
                    var building = Object.Instantiate(buildingPrefab, pos + Vector3.up * 0.1f, Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0), envGroup.transform);
                    building.name = $"TempleBuilding_{i}";
                    building.transform.localScale = new Vector3(Random.Range(8f, 15f), Random.Range(8f, 20f), Random.Range(8f, 15f));
                    
                    var renderers = building.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var mat = new Material(rend.material);
                        mat.color = new Color(0.5f, 0.25f, 0.2f);
                        rend.material = mat;
                    }
                    building.AddComponent<DestructibleEnvironment>();
                    
                    // Roof
                    var roofPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(roofPaths[Random.Range(0, roofPaths.Length)]);
                    if (roofPrefab != null)
                    {
                        float roofHeight = building.transform.localScale.y * 0.3f;
                        var roof = Object.Instantiate(roofPrefab, Vector3.zero, Quaternion.identity, building.transform);
                        roof.name = $"Roof_{i}";
                        roof.transform.localPosition = new Vector3(0, 0.5f + (roofHeight / building.transform.localScale.y), 0);
                        roof.transform.localScale = new Vector3(1.4f, roofHeight / building.transform.localScale.y, 1.4f);
                        
                        var roofRenderers = roof.GetComponentsInChildren<Renderer>();
                        foreach (var rend in roofRenderers)
                        {
                            var mat = new Material(rend.material);
                            mat.color = new Color(0.2f, 0.2f, 0.25f);
                            rend.material = mat;
                        }
                    }
                }
            }

            // Use Synty rock assets for temple lanterns instead of procedural cylinders
            string[] lanternPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_02.prefab"
            };
            
            for (int i = 0; i < 8; i++)
            {
                float angle = (i / 8f) * Mathf.PI * 2f;
                float radius = 20f;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                
                var lanternPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(lanternPaths[Random.Range(0, lanternPaths.Length)]);
                if (lanternPrefab != null)
                {
                    var lantern = Object.Instantiate(lanternPrefab, pos + Vector3.up * 0.1f, Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0), envGroup.transform);
                    lantern.name = $"Lantern_{i}";
                    lantern.transform.localScale = new Vector3(0.8f, 3f, 0.8f);
                    
                    var renderers = lantern.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var mat = new Material(rend.material);
                        mat.color = new Color(0.9f, 0.7f, 0.3f);
                        mat.SetFloat("_Emission", 0.5f);
                        rend.material = mat;
                    }
                    lantern.AddComponent<DestructibleEnvironment>();
                }
            }

            string[] treePaths = {
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pine_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pine_2.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pine_3.fbx"
            };
            
            for (int i = 0; i < 30; i++)
            {
                string path = treePaths[Random.Range(0, treePaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    Vector3 pos = GetRandomPositionWithoutCenter(15f, 48f);
                    var tree = Object.Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), envGroup.transform);
                    float s = Random.Range(0.7f, 1.3f);
                    tree.transform.localScale = Vector3.one * s;
                    tree.AddComponent<DestructibleEnvironment>();
                }
            }
            
            Debug.Log("[ArenaEnvironmentBuilder] Korean Temple environment built.");
#endif
        }

        private static void BuildVolcanicCoastEnvironment()
        {
#if UNITY_EDITOR
            var envGroup = new GameObject("Environment_Volcanic");
            
            RenderSettings.ambientLight = new Color(0.03f, 0.02f, 0.02f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.03f, 0.025f, 0.03f, 1f);
            RenderSettings.fogDensity = 0.04f;
            RenderSettings.fogMode = FogMode.Exponential;
            
            Material volcanicGroundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            volcanicGroundMat.color = new Color(0.02f, 0.015f, 0.01f);
            volcanicGroundMat.SetFloat("_Smoothness", 0.05f);
            volcanicGroundMat.SetFloat("_Metallic", 0f);
            
            // Use TerrainDemoScene terrain prefabs for volcanic ground tiles instead of procedural planes
            string[] groundPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_02.prefab"
            };
            
            for (int ix = -1; ix <= 1; ix++)
            {
                for (int iz = -1; iz <= 1; iz++)
                {
                    var tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(groundPaths[Random.Range(0, groundPaths.Length)]);
                    if (tilePrefab != null)
                    {
                        var tile = Object.Instantiate(tilePrefab, new Vector3(ix * 60f, -0.05f, iz * 60f), Quaternion.identity, envGroup.transform);
                        tile.name = $"VolcanicGround_Tile_{ix}_{iz}";
                        tile.transform.localScale = new Vector3(60f, 1f, 60f);
                        
                        var renderers = tile.GetComponentsInChildren<Renderer>();
                        foreach (var rend in renderers)
                        {
                            Material tileMat = new Material(volcanicGroundMat);
                            float variation = Random.Range(0.8f, 1.2f);
                            float baseDarkness = Random.Range(0.04f, 0.08f);
                            tileMat.color = new Color(baseDarkness * variation, baseDarkness * 0.8f * variation, baseDarkness * 0.6f * variation);
                            rend.material = tileMat;
                        }
                    }
                }
            }
            
            string[] allRockPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_A_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_A_02.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_B_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_B_02.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_C_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_C_02.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_D.prefab"
            };
            
            for (int i = 0; i < 80; i++)
            {
                Vector3 pos = GetRandomPositionWithoutCenter(25f, 32f);
                
                GameObject rockPrefab = null;
                int rockIndex = Random.Range(0, allRockPaths.Length);
                for (int r = 0; r < allRockPaths.Length; r++)
                {
                    int tryIndex = (rockIndex + r) % allRockPaths.Length;
                    rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(allRockPaths[tryIndex]);
                    if (rockPrefab != null) 
                    {
                        rockIndex = tryIndex;
                        break;
                    }
                }
                
                if (rockPrefab != null)
                {
                    var rock = Object.Instantiate(rockPrefab, pos, Quaternion.Euler(Random.Range(0, 30), Random.Range(0, 360), Random.Range(0, 30)), envGroup.transform);
                    float s = Random.Range(0.6f, 2.8f);
                    rock.transform.localScale = new Vector3(s, s * Random.Range(0.7f, 1.5f), s);
                    
                    var cols = rock.GetComponentsInChildren<Collider>();
                    foreach (var c in cols) Object.DestroyImmediate(c);
                    
                    rock.AddComponent<BoxCollider>();
                    rock.AddComponent<DestructibleEnvironment>();
                }
            }

            // Use Synty floor assets for lava pit instead of procedural cylinder
            string[] lavaPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_Round_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_01.prefab"
            };
            
            var lavaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(lavaPaths[Random.Range(0, lavaPaths.Length)]);
            if (lavaPrefab != null)
            {
                var lavaPit = Object.Instantiate(lavaPrefab, Vector3.zero, Quaternion.identity, envGroup.transform);
                lavaPit.name = "LavaPit";
                lavaPit.transform.localScale = new Vector3(25f, 0.2f, 25f);
                
                Material lavaMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                lavaMat.color = new Color(1f, 0.3f, 0.1f);
                lavaMat.SetFloat("_Emission", 1f);
                lavaMat.SetColor("_EmissionColor", new Color(1f, 0.2f, 0f));
                
                var renderers = lavaPit.GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers) rend.material = lavaMat;
                
                lavaPit.AddComponent<FireZone>();
            }
            
            Debug.Log("[ArenaEnvironmentBuilder] Volcanic Coast environment built.");
#endif
        }

        private static Vector3 GetRandomPositionWithoutCenter(float minRadius, float maxRadius)
        {
            int maxAttempts = 50;
            for (int i = 0; i < maxAttempts; i++)
            {
                float x = Random.Range(-maxRadius, maxRadius);
                float z = Random.Range(-maxRadius, maxRadius);
                Vector3 pos = new Vector3(x, 0, z);
                
                if (Mathf.Abs(pos.x) < 16f && Mathf.Abs(pos.z) < 16f) continue;
                
                if (pos.magnitude > minRadius) return pos;
            }
            return new Vector3(maxRadius, 0, maxRadius);
        }

        // =====================================================
        // PREMIUM ENVIRONMENT BUILDERS - Enhanced ZCB Zones
        // Based on design documents: design/levels/zcb-*.md
        // =====================================================

#if UNITY_EDITOR
        /// <summary>
        /// ZCB-FOREST Premium: Bioluminescent forest with greenhouse ruins
        /// Uses real project assets: CommonTree, Pine, Bush, Fern, Flower assets
        /// </summary>
        public static void BuildForestEnvironmentPremium()
        {
            var envGroup = new GameObject("Environment_ZCB_FOREST_Premium");
            
            // Green dimensional fog premium
            RenderSettings.ambientLight = new Color(0.12f, 0.28f, 0.12f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.08f, 0.22f, 0.1f, 1f);
            RenderSettings.fogDensity = 0.022f;
            
            // PREMIUM: Forest ground with stylized grass material + texture variation
            var groundMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Stylized_Grass.mat");
            if (groundMat == null)
                groundMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Material_Grass.mat");
            
            // Use TerrainDemoScene terrain prefabs for premium forest ground
            string[] groundPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_02.prefab"
            };
            
            var groundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(groundPaths[0]);
            if (groundPrefab != null)
            {
                var ground = Object.Instantiate(groundPrefab, new Vector3(0, -0.1f, 0), Quaternion.identity, envGroup.transform);
                ground.name = "ForestGround_Premium";
                ground.transform.localScale = new Vector3(120f, 1f, 120f);
                
                // Apply material with texture tiling for premium look
                if (groundMat != null)
                {
                    var forestGroundMat = new Material(groundMat);
                    forestGroundMat.mainTextureScale = new Vector2(20f, 20f);
                    forestGroundMat.SetFloat("_Smoothness", 0.25f);
                    forestGroundMat.SetFloat("_Metallic", 0f);
                    
                    var renderers = ground.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers) rend.material = forestGroundMat;
                }
                
                var cols = ground.GetComponentsInChildren<Collider>();
                foreach (var col in cols) if (col != null) col.enabled = true;
                
                ground.tag = "Ground";
            }
            
            // Greenhouse ruins with better materials
            BuildStructureRuins(envGroup, "Greenhouse", new Vector3(18f, 7f, 18f), new Color(0.35f, 0.48f, 0.42f));
            
            // PREMIUM: 100 Real trees from Stylized Nature MegaKit with material variation
            string[] treePaths = {
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/CommonTree_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/CommonTree_2.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/CommonTree_3.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/CommonTree_4.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/CommonTree_5.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pine_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pine_2.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pine_3.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pine_4.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pine_5.fbx"
            };
            
            // Load bark material for trees
            var barkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Stylized_Bark.mat");
            
            for (int i = 0; i < 100; i++)
            {
                string path = treePaths[Random.Range(0, treePaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    Vector3 pos = GetRandomPositionWithoutCenter(10f, 55f);
                    var tree = Object.Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), envGroup.transform);
                    float s = Random.Range(0.85f, 1.9f);
                    tree.transform.localScale = new Vector3(s, s * Random.Range(0.9f, 1.35f), s);
                    
                    // Premium bioluminescence using pebble assets
                    string[] glowPaths = {
                        "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Round_1.fbx",
                        "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Round_2.fbx"
                    };
                    
                    var glowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(glowPaths[Random.Range(0, glowPaths.Length)]);
                    if (glowPrefab != null)
                    {
                        var bio = Object.Instantiate(glowPrefab, tree.transform);
                        bio.name = "ForestBioLight_Premium";
                        bio.transform.localPosition = new Vector3(0, Random.Range(2.5f, 5f), 0);
                        bio.transform.localScale = Vector3.one * Random.Range(0.15f, 0.35f);
                        
                        Color[] bioColors = {
                            new Color(0.2f, 0.8f, 0.3f), // Green
                            new Color(0.8f, 0.9f, 0.2f), // Yellow-green
                            new Color(0.3f, 0.9f, 0.6f)  // Cyan
                        };
                        
                        var renderers = bio.GetComponentsInChildren<Renderer>();
                        foreach (var rend in renderers)
                        {
                            var bioMat = new Material(rend.material);
                            bioMat.color = bioColors[Random.Range(0, bioColors.Length)];
                            bioMat.SetFloat("_Emission", 0.6f);
                            rend.material = bioMat;
                        }
                    }
                    
                    tree.AddComponent<DestructibleEnvironment>();
                }
            }
            
            // Prisoner trees with premium twisted models
            string[] twistedPaths = {
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/TwistedTree_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/TwistedTree_2.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/TwistedTree_3.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/TwistedTree_4.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/TwistedTree_5.fbx"
            };
            
            for (int i = 0; i < 12; i++)
            {
                float angle = (i / 12f) * Mathf.PI * 2f;
                float radius = 28f;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                
                string path = twistedPaths[Random.Range(0, twistedPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    var tree = Object.Instantiate(prefab, pos, Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0), envGroup.transform);
                    float s = Random.Range(0.95f, 1.5f);
                    tree.transform.localScale = new Vector3(s, s, s);
                    
                    // Enhanced purple emission for prisoner trees using pebble assets
                    string[] glowPaths = {
                        "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Round_1.fbx",
                        "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Round_2.fbx"
                    };
                    
                    var glowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(glowPaths[Random.Range(0, glowPaths.Length)]);
                    if (glowPrefab != null)
                    {
                        var bio = Object.Instantiate(glowPrefab, tree.transform);
                        bio.name = "PrisonerGlow_Premium";
                        bio.transform.localPosition = new Vector3(0, 2.8f, 0);
                        bio.transform.localScale = Vector3.one * 0.5f;
                        
                        var renderers = bio.GetComponentsInChildren<Renderer>();
                        foreach (var rend in renderers)
                        {
                            var bioMat = new Material(rend.material);
                            bioMat.color = new Color(0.65f, 0.15f, 0.85f);
                            bioMat.SetFloat("_Emission", 0.75f);
                            rend.material = bioMat;
                        }
                    }
                    
                    tree.AddComponent<DestructibleEnvironment>();
                }
            }
            
            // Premium bushes with flower variants
            string[] bushPaths = {
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Bush_Common.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Bush_Common_Flowers.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Fern_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Grass_Common_Short.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Grass_Common_Tall.fbx"
            };
            
            for (int i = 0; i < 60; i++)
            {
                string path = bushPaths[Random.Range(0, bushPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    Vector3 pos = GetRandomPositionWithoutCenter(8f, 52f);
                    var bush = Object.Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), envGroup.transform);
                    float s = Random.Range(0.65f, 1.4f);
                    bush.transform.localScale = Vector3.one * s;
                    bush.AddComponent<DestructibleEnvironment>();
                }
            }
            
            // PREMIUM: 45 Synty mushrooms - smaller clusters with bioluminescence
            string[] syntyMushroomPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Mushroom_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Mushroom_02.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Mushroom_03.prefab"
            };
            
            for (int cluster = 0; cluster < 45; cluster++)
            {
                Vector3 center = GetRandomPositionWithoutCenter(12f, 45f);
                int mushroomsInCluster = Random.Range(3, 8);
                
                for (int i = 0; i < mushroomsInCluster; i++)
                {
                    Vector3 offset = Random.insideUnitSphere * 4f;
                    offset.y = 0;
                    Vector3 pos = center + offset;
                    
                    string path = syntyMushroomPaths[Random.Range(0, syntyMushroomPaths.Length)];
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        var mushroom = Object.Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), envGroup.transform);
                        float s = Random.Range(0.9f, 1.8f);
                        mushroom.transform.localScale = Vector3.one * s;
                        mushroom.AddComponent<DestructibleEnvironment>();
                    }
                }
            }
            
            // Enhanced spore sacks
            BuildSporeSacks(envGroup, 35);
            
            // The Heart - central giant mushroom
            BuildHeartMushroom(envGroup);
            
            // Underground lab tunnels
            BuildFungalTunnels(envGroup);
            
            // PREMIUM: Synty ferns for ground cover
            string[] fernPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Fern_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Fern_02.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Fern_03.prefab"
            };
            
            for (int i = 0; i < 35; i++)
            {
                string path = fernPaths[Random.Range(0, fernPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    Vector3 pos = GetRandomPositionWithoutCenter(10f, 48f);
                    var fern = Object.Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), envGroup.transform);
                    float s = Random.Range(0.9f, 1.6f);
                    fern.transform.localScale = Vector3.one * s;
                    fern.AddComponent<DestructibleEnvironment>();
                }
            }
            
            // Add real nature kit ferns and grass
            string[] natureFernPaths = {
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Fern_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Grass_Wispy_Short.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Grass_Wispy_Tall.fbx"
            };
            
            for (int i = 0; i < 50; i++)
            {
                string path = natureFernPaths[Random.Range(0, natureFernPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    Vector3 pos = GetRandomPositionWithoutCenter(8f, 50f);
                    var plant = Object.Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), envGroup.transform);
                    float s = Random.Range(0.7f, 1.4f);
                    plant.transform.localScale = Vector3.one * s;
                }
            }
            
            Debug.Log("[ArenaEnvironmentBuilder] ZCB-FUNGAL Premium built with 80+ mushroom assets and vibrant bioluminescence.");
            
            GenerateThumbnailForMap("mushroomgrove");
        }

        /// <summary>
        /// ZCB-HYDRO Premium: Marine X-SPACE - Platforms over alien ocean
        /// Complete visual overhaul with deep sea atmosphere, realistic water, and marine structures
        /// </summary>
        public static void BuildHydroEnvironmentPremium()
        {
            var envGroup = new GameObject("Environment_ZCB_HYDRO_Premium");
            
            // Deep ocean atmosphere - dark blue mysterious lighting
            RenderSettings.ambientLight = new Color(0.08f, 0.15f, 0.25f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.05f, 0.12f, 0.2f, 1f);
            RenderSettings.fogDensity = 0.025f;
            RenderSettings.fogMode = FogMode.Exponential;
            
            // Create deep ocean floor visible through transparent water
            BuildDeepOceanFloor(envGroup);
            
            // Create realistic water surface with transparency
            BuildRealisticWaterSurface(envGroup);
            
            // Main central platform - large stone/marine structure
            BuildCentralMarinePlatform(envGroup);
            
            // Ring of smaller satellite islands around center
            BuildSatelliteIslands(envGroup, 10);
            
            // Marine structures - observation towers, docks, bridges
            BuildMarineTowers(envGroup, 4);
            BuildConnectingBridges(envGroup);
            
            // Underwater vegetation and coral visible through water
            BuildUnderwaterVegetation(envGroup);
            BuildCoralReefs(envGroup, 15);
            
            // Surface details - lily pads, floating debris, buoys
            BuildSurfaceDetails(envGroup);
            
            // Atmospheric effects - floating particles, light rays
            BuildMarineAtmosphere(envGroup);
            
            Debug.Log("[ArenaEnvironmentBuilder] ZCB-HYDRO Premium - Marine Arena built with deep ocean atmosphere.");
            
            // Generate thumbnail for this map
            GenerateThumbnailForMap("waterarena");
        }

        /// <summary>
        /// ZCB-SANCTUM Premium: Corrupted Sanctuary - Ancient temple invaded by X-SPACE
        /// Uses real project assets: Synty rocks, TerrainDemoScene rocks, pine trees
        /// </summary>
        public static void BuildSanctumEnvironmentPremium()
        {
            var envGroup = new GameObject("Environment_ZCB_SANCTUM_Premium");
            
            // Mystical temple lighting
            RenderSettings.ambientLight = new Color(0.25f, 0.22f, 0.18f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.2f, 0.17f, 0.15f, 1f);
            RenderSettings.fogDensity = 0.025f;
            
            // Stone temple ground using TerrainDemoScene terrain
            string[] groundPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_02.prefab"
            };
            
            var groundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(groundPaths[0]);
            if (groundPrefab != null)
            {
                var ground = Object.Instantiate(groundPrefab, new Vector3(0, -0.1f, 0), Quaternion.identity, envGroup.transform);
                ground.name = "TempleGround";
                ground.transform.localScale = new Vector3(100f, 1f, 100f);
                
                if (stoneMat != null)
                {
                    var renderers = ground.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers) rend.material = stoneMat;
                }
                
                var cols = ground.GetComponentsInChildren<Collider>();
                foreach (var col in cols) if (col != null) col.enabled = true;
                
                ground.tag = "Ground";
            }
            
            // 6 Pagodas using Synty rocks as temple structures
            string[] pagodaPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_06.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_07.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_08.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_09.prefab"
            };
            
            for (int i = 0; i < 6; i++)
            {
                float angle = (i / 6f) * Mathf.PI * 2f;
                float radius = 30f;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                
                // Some pagodas float higher
                float height = (i % 2 == 0) ? 0f : Random.Range(3f, 8f);
                
                string path = pagodaPaths[Random.Range(0, pagodaPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    var pagoda = Object.Instantiate(prefab, pos + Vector3.up * height, Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0), envGroup.transform);
                    float s = Random.Range(2f, 4f);
                    pagoda.transform.localScale = new Vector3(s, s * Random.Range(1.5f, 3f), s);
                    pagoda.AddComponent<DestructibleEnvironment>();
                }
            }
            
            // 16 Stone lanterns using Synty rock pillars
            string[] lanternPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Cliff_Pillar_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Stalactite_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Stalactite_02.prefab"
            };
            
            for (int i = 0; i < 16; i++)
            {
                float angle = (i / 16f) * Mathf.PI * 2f;
                float radius = Random.Range(15f, 40f);
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                
                string path = lanternPaths[Random.Range(0, lanternPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    var lantern = Object.Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), envGroup.transform);
                    float s = Random.Range(0.8f, 1.5f);
                    lantern.transform.localScale = new Vector3(s, s * Random.Range(2f, 4f), s);
                    
                    // Golden glow using pebble assets
                    string[] glowPaths = {
                        "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Round_1.fbx",
                        "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Round_2.fbx"
                    };
                    
                    var glowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(glowPaths[Random.Range(0, glowPaths.Length)]);
                    if (glowPrefab != null)
                    {
                        var bio = Object.Instantiate(glowPrefab, lantern.transform);
                        bio.name = "LanternGlow";
                        bio.transform.localPosition = new Vector3(0, 3f, 0);
                        bio.transform.localScale = Vector3.one * 0.3f;
                        
                        var renderers = bio.GetComponentsInChildren<Renderer>();
                        foreach (var rend in renderers)
                        {
                            var bioMat = new Material(rend.material);
                            bioMat.color = new Color(0.3f, 0.8f, 0.4f);
                            bioMat.SetFloat("_Emission", 0.8f);
                            rend.material = bioMat;
                        }
                    }
                    
                    lantern.AddComponent<DestructibleEnvironment>();
                }
            }
            
            // 25 Pine trees from Stylized Nature MegaKit
            string[] pinePaths = {
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pine_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pine_2.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pine_3.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pine_4.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pine_5.fbx"
            };
            
            for (int i = 0; i < 25; i++)
            {
                string path = pinePaths[Random.Range(0, pinePaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    Vector3 pos = GetRandomPositionWithoutCenter(15f, 48f);
                    var tree = Object.Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), envGroup.transform);
                    float s = Random.Range(0.8f, 1.6f);
                    tree.transform.localScale = Vector3.one * s;
                    tree.AddComponent<DestructibleEnvironment>();
                }
            }
            
            // Corrupted zen garden - floating rocks
            BuildFloatingZenGarden(envGroup);
            
            // The Anchor Gate
            BuildAnchorGate(envGroup);
            
            // Dimensional rifts
            BuildDimensionalRifts(envGroup, 10);
            
            // Changed Monks
            BuildChangedMonks(envGroup, 8);
            
            Debug.Log("[ArenaEnvironmentBuilder] ZCB-SANCTUM Premium built with real temple assets.");
            
            // Generate thumbnail for this map
            GenerateThumbnailForMap("koreantemple");
        }

        /// <summary>
        /// ZCB-VOLCANIC Premium: The Failed Purification - Fire vs X-SPACE
        /// Uses real project assets: TerrainDemoScene dark rocks, Synty cliffs
        /// </summary>
        public static void BuildVolcanicEnvironmentPremium()
        {
            var envGroup = new GameObject("Environment_ZCB_VOLCANIC_Premium");
            
            // Dark volcanic lighting
            RenderSettings.ambientLight = new Color(0.03f, 0.02f, 0.02f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.04f, 0.03f, 0.03f, 1f);
            RenderSettings.fogDensity = 0.04f;
            
            // Volcanic ground tiles with variation - use flat cubes for stable colliders
            var volcanicMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Stylized_Rock.mat");
            
            // Volcanic ground using TerrainDemoScene terrain
            string[] groundPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_02.prefab"
            };
            
            var groundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(groundPaths[0]);
            if (groundPrefab != null)
            {
                var ground = Object.Instantiate(groundPrefab, new Vector3(0, -0.1f, 0), Quaternion.identity, envGroup.transform);
                ground.name = "VolcanicGround";
                ground.transform.localScale = new Vector3(130f, 1f, 130f);
                
                if (volcanicMat != null)
                {
                    var groundMat = new Material(volcanicMat);
                    groundMat.color = new Color(0.06f, 0.05f, 0.04f);
                    
                    var renderers = ground.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers) rend.material = groundMat;
                }
                
                var cols = ground.GetComponentsInChildren<Collider>();
                foreach (var col in cols) if (col != null) col.enabled = true;
                
                ground.tag = "Ground";
            }
            
            // 80 Dark volcanic rocks from TerrainDemoScene
            string[] rockPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_A_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_A_02.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_B_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_B_02.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_C_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_C_02.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_D.prefab"
            };
            
            for (int i = 0; i < 80; i++)
            {
                Vector3 pos = GetRandomPositionWithoutCenter(25f, 32f);
                
                string path = rockPaths[Random.Range(0, rockPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    var rock = Object.Instantiate(prefab, pos, Quaternion.Euler(Random.Range(0, 30), Random.Range(0, 360), Random.Range(0, 30)), envGroup.transform);
                    float s = Random.Range(0.6f, 2.8f);
                    rock.transform.localScale = new Vector3(s, s * Random.Range(0.7f, 1.5f), s);
                    
                    var cols = rock.GetComponentsInChildren<Collider>();
                    foreach (var c in cols) Object.DestroyImmediate(c);
                    
                    rock.AddComponent<BoxCollider>();
                    rock.AddComponent<DestructibleEnvironment>();
                }
            }
            
            // Synty cliffs for dramatic canyon walls
            string[] cliffPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Cliff_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Cliff_02.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Cliff_03.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Cliff_04.prefab"
            };
            
            for (int i = 0; i < 12; i++)
            {
                float angle = (i / 12f) * Mathf.PI * 2f;
                float radius = Random.Range(50f, 60f);
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                
                string path = cliffPaths[Random.Range(0, cliffPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    var cliff = Object.Instantiate(prefab, pos, Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0), envGroup.transform);
                    float s = Random.Range(2f, 4f);
                    cliff.transform.localScale = new Vector3(s, s * Random.Range(1.5f, 3f), s);
                    cliff.AddComponent<DestructibleEnvironment>();
                }
            }
            
            // Lava rivers
            BuildLavaRivers(envGroup, 4);
            
            // Cooling towers (safe zones)
            BuildCoolingTowers(envGroup, 4);
            
            // Industrial pipes
            BuildIndustrialPipes(envGroup, 20);
            
            // Active crater center
            BuildActiveCrater(envGroup);
            
            // Ash falling effect locations
            BuildAshSources(envGroup, 12);
            
            // Failed geothermal equipment
            BuildFailedEquipment(envGroup);
            
            Debug.Log("[ArenaEnvironmentBuilder] ZCB-VOLCANIC Premium built with real volcanic assets.");
            
            // Generate thumbnail for this map
            GenerateThumbnailForMap("volcanic");
        }

        // =====================================================
        // THEME-SPECIFIC HELPER METHODS
        // =====================================================

        // DEADWOODS helpers
        private static void BuildPetrifiedTrees(GameObject parent, int count)
        {
            // Use Synty dead tree assets instead of procedural cylinders
            string[] treePaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_Dead_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_Dead_02.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_Dead_03.prefab"
            };
            
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = GetRandomPositionWithoutCenter(10f, 48f);
                
                string path = treePaths[Random.Range(0, treePaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    var trunk = Object.Instantiate(prefab, pos + Vector3.up * 3f, Quaternion.Euler(Random.Range(-10f, 10f), Random.Range(0f, 360f), Random.Range(-10f, 10f)), parent.transform);
                    trunk.name = "PetrifiedTree_" + i;
                    trunk.transform.localScale = new Vector3(Random.Range(0.8f, 1.5f), Random.Range(5f, 8f), Random.Range(0.8f, 1.5f));
                    
                    var renderers = trunk.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var trunkMat = new Material(rend.material);
                        trunkMat.color = new Color(0.08f, 0.06f, 0.05f);
                        rend.material = trunkMat;
                    }
                    
                    trunk.AddComponent<DestructibleEnvironment>();
                }
            }
        }

        private static void BuildEchoes(GameObject parent, int count)
        {
            // Use Synty rock assets for shadowy echoes instead of procedural cylinders
            string[] echoPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_02.prefab"
            };
            
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f;
                float radius = Random.Range(20f, 40f);
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 1f, Mathf.Sin(angle) * radius);
                
                string path = echoPaths[Random.Range(0, echoPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    var echo = Object.Instantiate(prefab, pos + Vector3.up * 0.1f, Quaternion.identity, parent.transform);
                    echo.name = "Echo_" + i;
                    echo.transform.localScale = new Vector3(0.8f, 3f, 0.8f);
                    
                    var renderers = echo.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var echoMat = new Material(rend.material);
                        echoMat.color = new Color(0.02f, 0.01f, 0.03f);
                        echoMat.SetFloat("_Emission", 0.1f);
                        echoMat.SetColor("_EmissionColor", new Color(0.1f, 0.05f, 0.15f));
                        rend.material = echoMat;
                    }
                }
            }
        }

        private static void BuildToxinLabEntrance(GameObject parent)
        {
            // Use Synty floor assets for collapsed lab entrance instead of procedural cubes
            string[] entrancePaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_Round_01.prefab"
            };
            
            var entrancePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(entrancePaths[Random.Range(0, entrancePaths.Length)]);
            if (entrancePrefab != null)
            {
                var entrance = Object.Instantiate(entrancePrefab, new Vector3(0, 0.5f, 40f), Quaternion.identity, parent.transform);
                entrance.name = "ToxinLab_Entrance";
                entrance.transform.localScale = new Vector3(8f, 1f, 0.5f);
                
                var renderers = entrance.GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers)
                {
                    var mat = new Material(rend.material);
                    mat.color = new Color(0.2f, 0.18f, 0.15f);
                    rend.material = mat;
                }
            }
            
            // Warning sign using pebble asset
            string[] signPaths = {
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Square_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Square_2.fbx"
            };
            
            var signPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(signPaths[Random.Range(0, signPaths.Length)]);
            if (signPrefab != null)
            {
                var sign = Object.Instantiate(signPrefab, new Vector3(0, 0.1f, 39.5f), Quaternion.identity, parent.transform);
                sign.name = "Biohazard_Sign";
                sign.transform.localScale = new Vector3(0.8f, 0.2f, 0.05f);
                
                var renderers = sign.GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers)
                {
                    var signMat = new Material(rend.material);
                    signMat.color = new Color(0.7f, 0.1f, 0.8f);
                    signMat.SetFloat("_Emission", 0.5f);
                    rend.material = signMat;
                }
            }
        }

        private static void BuildToxinRiver(GameObject parent)
        {
            // Use TerrainDemoScene terrain for glowing violet river instead of procedural planes
            string[] riverPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_02.prefab"
            };
            
            for (int i = -2; i <= 2; i++)
            {
                var riverPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(riverPaths[Random.Range(0, riverPaths.Length)]);
                if (riverPrefab != null)
                {
                    var river = Object.Instantiate(riverPrefab, new Vector3(i * 15f, -0.3f, 0f), Quaternion.identity, parent.transform);
                    river.name = "ToxinRiver_" + i;
                    river.transform.localScale = new Vector3(12f, 1f, 55f);
                    
                    var renderers = river.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var riverMat = new Material(rend.material);
                        riverMat.color = new Color(0.5f, 0.1f, 0.6f);
                        riverMat.SetFloat("_Emission", 0.8f);
                        rend.material = riverMat;
                    }
                }
            }
        }

        // FUNGAL helpers
        private static void BuildGiantMushrooms(GameObject parent, int count)
        {
            // Use real Mushroom assets from Stylized Nature MegaKit instead of procedural cylinders
            string[] capPaths = {
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Mushroom_Common.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Mushroom_Laetiporus.fbx"
            };
            string[] stemPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Stalactite_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Stalactite_02.prefab"
            };
            
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = GetRandomPositionWithoutCenter(15f, 45f);
                
                // Mushroom cap using real mushroom assets
                var capPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(capPaths[Random.Range(0, capPaths.Length)]);
                if (capPrefab != null)
                {
                    var cap = Object.Instantiate(capPrefab, pos + Vector3.up * 6f, Quaternion.identity, parent.transform);
                    cap.name = "GiantMushroom_" + i;
                    cap.transform.localScale = new Vector3(Random.Range(4f, 8f), 2f, Random.Range(4f, 8f));
                    
                    var renderers = cap.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var capMat = new Material(rend.material);
                        Color[] colors = { new Color(0.4f, 0.2f, 0.6f), new Color(0.2f, 0.5f, 0.7f), new Color(0.7f, 0.3f, 0.5f) };
                        capMat.color = colors[Random.Range(0, colors.Length)];
                        capMat.SetFloat("_Emission", 0.6f);
                        rend.material = capMat;
                    }
                }
                
                // Stem using Synty stalactite assets
                var stemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(stemPaths[Random.Range(0, stemPaths.Length)]);
                if (stemPrefab != null)
                {
                    var stem = Object.Instantiate(stemPrefab, pos + Vector3.up * 3f, Quaternion.identity, parent.transform);
                    stem.name = "MushroomStem_" + i;
                    stem.transform.localScale = new Vector3(1.5f, 6f, 1.5f);
                    
                    var renderers = stem.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var stemMat = new Material(rend.material);
                        stemMat.color = new Color(0.9f, 0.85f, 0.9f);
                        rend.material = stemMat;
                    }
                }
            }
        }

        private static void BuildMushroomClusters(GameObject parent, int count)
        {
            // Use Synty mushroom assets instead of procedural cylinders
            string[] clusterPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Mushroom_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Mushroom_02.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Mushroom_03.prefab"
            };
            
            for (int cluster = 0; cluster < count; cluster++)
            {
                Vector3 center = GetRandomPositionWithoutCenter(12f, 42f);
                int mushroomsInCluster = Random.Range(3, 7);
                
                for (int i = 0; i < mushroomsInCluster; i++)
                {
                    Vector3 offset = Random.insideUnitSphere * 3f;
                    offset.y = 0;
                    Vector3 pos = center + offset;
                    
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(clusterPaths[Random.Range(0, clusterPaths.Length)]);
                    if (prefab != null)
                    {
                        var mushroom = Object.Instantiate(prefab, pos + Vector3.up * 0.65f, Quaternion.identity, parent.transform);
                        mushroom.name = "ClusterMushroom_" + cluster + "_" + i;
                        mushroom.transform.localScale = new Vector3(Random.Range(0.8f, 1.5f), Random.Range(1f, 2f), Random.Range(0.8f, 1.5f));
                        
                        var renderers = mushroom.GetComponentsInChildren<Renderer>();
                        foreach (var rend in renderers)
                        {
                            var mat = new Material(rend.material);
                            mat.color = new Color(Random.Range(0.3f, 0.7f), Random.Range(0.2f, 0.5f), Random.Range(0.4f, 0.8f));
                            mat.SetFloat("_Emission", 0.5f);
                            rend.material = mat;
                        }
                    }
                }
            }
        }

        private static void BuildSporeSacks(GameObject parent, int count)
        {
            // Use real Bush assets from Stylized Nature MegaKit instead of procedural spheres
            string[] bushPaths = {
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Bush_Common.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Bush_Common_Flowers.fbx"
            };
            
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = GetRandomPositionWithoutCenter(10f, 45f);
                
                string path = bushPaths[Random.Range(0, bushPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    var sack = Object.Instantiate(prefab, pos + Vector3.up * Random.Range(2f, 5f), Quaternion.Euler(Random.Range(-20f, 20f), Random.Range(0f, 360f), Random.Range(-20f, 20f)), parent.transform);
                    sack.name = "SporeSack_" + i;
                    float s = Random.Range(0.3f, 0.6f);
                    sack.transform.localScale = Vector3.one * s;
                    
                    // Apply fungal color
                    var renderers = sack.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var mat = new Material(rend.material);
                        mat.color = new Color(0.8f, 0.4f, 0.2f);
                        mat.SetFloat("_Emission", 0.4f);
                        rend.material = mat;
                    }
                }
            }
        }

        private static void BuildHeartMushroom(GameObject parent)
        {
            // Use real Mushroom assets from Stylized Nature MegaKit instead of procedural cylinders
            string[] mushroomPaths = {
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Mushroom_Common.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Mushroom_Laetiporus.fbx"
            };
            
            var path = mushroomPaths[Random.Range(0, mushroomPaths.Length)];
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                var heart = Object.Instantiate(prefab, new Vector3(0, 8f, 0), Quaternion.identity, parent.transform);
                heart.name = "HeartMushroom";
                heart.transform.localScale = Vector3.one * 8f;
                
                // Apply glow material
                var renderers = heart.GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers)
                {
                    var mat = new Material(rend.material);
                    mat.color = new Color(0.6f, 0.2f, 0.8f);
                    mat.SetFloat("_Emission", 1f);
                    rend.material = mat;
                }
                
                // Pulsing glow using pebble asset
                string[] pulsePaths = {
                    "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Round_1.fbx",
                    "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Round_2.fbx"
                };
                
                var pulsePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(pulsePaths[Random.Range(0, pulsePaths.Length)]);
                if (pulsePrefab != null)
                {
                    var pulse = Object.Instantiate(pulsePrefab, heart.transform);
                    pulse.name = "HeartPulse";
                    pulse.transform.localPosition = Vector3.zero;
                    pulse.transform.localScale = Vector3.one * 0.3f;
                    
                    var pulseRenderers = pulse.GetComponentsInChildren<Renderer>();
                    foreach (var rend in pulseRenderers)
                    {
                        var pulseMat = new Material(rend.material);
                        pulseMat.color = new Color(1f, 0.5f, 0.9f);
                        pulseMat.SetFloat("_Emission", 1.5f);
                        rend.material = pulseMat;
                    }
                }
            }
        }

        private static void BuildFungalTunnels(GameObject parent)
        {
            // Use Synty rock assets for tunnel entrances instead of procedural cylinders
            string[] tunnelPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_02.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_03.prefab"
            };
            
            for (int i = 0; i < 4; i++)
            {
                float angle = (i / 4f) * Mathf.PI * 2f;
                float radius = 35f;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0.2f, Mathf.Sin(angle) * radius);
                
                string path = tunnelPaths[Random.Range(0, tunnelPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    var tunnel = Object.Instantiate(prefab, pos + Vector3.up * 0.2f, Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0), parent.transform);
                    tunnel.name = "FungalTunnel_" + i;
                    tunnel.transform.localScale = new Vector3(3f, 0.4f, 3f);
                    
                    var renderers = tunnel.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var mat = new Material(rend.material);
                        mat.color = new Color(0.3f, 0.15f, 0.4f);
                        mat.SetFloat("_Emission", 0.3f);
                        rend.material = mat;
                    }
                }
            }
        }

        // HYDRO helpers
        private static void BuildXSpaceOcean(GameObject parent)
        {
            // Ocean tiles using TerrainDemoScene terrain instead of procedural planes
            string[] waterPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_02.prefab"
            };
            
            for (int x = -3; x <= 3; x++)
            {
                for (int z = -3; z <= 3; z++)
                {
                    if (Mathf.Abs(x) <= 1 && Mathf.Abs(z) <= 1) continue; // Skip center for main platform
                    
                    var waterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(waterPaths[Random.Range(0, waterPaths.Length)]);
                    if (waterPrefab != null)
                    {
                        var water = Object.Instantiate(waterPrefab, new Vector3(x * 20f, -1f, z * 20f), Quaternion.identity, parent.transform);
                        water.name = "XSpaceOcean_" + x + "_" + z;
                        water.transform.localScale = new Vector3(20f, 1f, 20f);
                        
                        var renderers = water.GetComponentsInChildren<Renderer>();
                        foreach (var rend in renderers)
                        {
                            var waterMat = new Material(rend.material);
                            waterMat.color = new Color(0.1f, 0.3f, 0.5f);
                            waterMat.SetFloat("_Smoothness", 0.9f);
                            rend.material = waterMat;
                        }
                    }
                }
            }
        }

        private static void BuildMainMarinePlatform(GameObject parent)
        {
            // Use Synty building assets for main marine platform instead of procedural cylinder
            string[] platformPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_Round_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_01.prefab"
            };
            
            var path = platformPaths[Random.Range(0, platformPaths.Length)];
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                var platform = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity, parent.transform);
                platform.name = "MainMarinePlatform";
                platform.transform.localScale = new Vector3(30f, 0.5f, 30f);
                
                var renderers = platform.GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers)
                {
                    var mat = new Material(rend.material);
                    mat.color = new Color(0.65f, 0.6f, 0.45f);
                    rend.material = mat;
                }
            }
        }

        private static void BuildSatellitePlatforms(GameObject parent, int count)
        {
            // Use Synty building assets for satellite platforms instead of procedural cylinders
            string[] platformPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_Round_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_01.prefab"
            };
            
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f;
                float radius = Random.Range(35f, 50f);
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                
                string path = platformPaths[Random.Range(0, platformPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    var platform = Object.Instantiate(prefab, pos + Vector3.up * 0.25f, Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0), parent.transform);
                    platform.name = "SatellitePlatform_" + i;
                    platform.transform.localScale = new Vector3(Random.Range(8f, 15f), 0.5f, Random.Range(8f, 15f));
                    
                    var renderers = platform.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var mat = new Material(rend.material);
                        mat.color = new Color(0.6f, 0.55f, 0.4f);
                        rend.material = mat;
                    }
                }
            }
        }

        private static void BuildMarineWalkways(GameObject parent)
        {
            // Walkways between platforms using Synty floor assets instead of procedural cubes
            string[] walkwayPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_Round_01.prefab"
            };
            
            for (int i = 0; i < 6; i++)
            {
                float angle = (i / 6f) * Mathf.PI * 2f;
                float startRadius = 15f;
                float endRadius = 40f;
                Vector3 start = new Vector3(Mathf.Cos(angle) * startRadius, 0.25f, Mathf.Sin(angle) * startRadius);
                Vector3 end = new Vector3(Mathf.Cos(angle) * endRadius, 0.25f, Mathf.Sin(angle) * endRadius);
                Vector3 center = (start + end) / 2f;
                float length = Vector3.Distance(start, end);
                
                var walkwayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(walkwayPaths[Random.Range(0, walkwayPaths.Length)]);
                if (walkwayPrefab != null)
                {
                    var walkway = Object.Instantiate(walkwayPrefab, center, Quaternion.Euler(0, angle * Mathf.Rad2Deg + 90f, 0), parent.transform);
                    walkway.name = "MarineWalkway_" + i;
                    walkway.transform.localScale = new Vector3(2f, 0.2f, length);
                    
                    var renderers = walkway.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var mat = new Material(rend.material);
                        mat.color = new Color(0.55f, 0.5f, 0.35f);
                        rend.material = mat;
                    }
                }
            }
        }

        private static void BuildObservationTower(GameObject parent)
        {
            // Use Synty building assets for observation tower instead of procedural cylinder
            string[] towerPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_Round_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_01.prefab"
            };
            
            var path = towerPaths[Random.Range(0, towerPaths.Length)];
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                var tower = Object.Instantiate(prefab, new Vector3(20f, 5f, 0f), Quaternion.identity, parent.transform);
                tower.name = "ObservationTower";
                tower.transform.localScale = new Vector3(4f, 10f, 4f);
                
                var renderers = tower.GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers)
                {
                    var mat = new Material(rend.material);
                    mat.color = new Color(0.5f, 0.48f, 0.45f);
                    rend.material = mat;
                }
            }
        }

        private static void BuildLifeboats(GameObject parent, int count)
        {
            // Use pebble assets from Stylized Nature MegaKit for lifeboats instead of procedural capsules
            string[] boatPaths = {
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Round_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Round_2.fbx"
            };
            
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = GetRandomPositionWithoutCenter(25f, 45f);
                
                string path = boatPaths[Random.Range(0, boatPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    var boat = Object.Instantiate(prefab, pos + Vector3.up * 0.5f, Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0f, 360f), Random.Range(-15f, 15f)), parent.transform);
                    boat.name = "AbandonedLifeboat_" + i;
                    boat.transform.localScale = new Vector3(1f, 0.4f, 2.5f);
                    
                    var renderers = boat.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var mat = new Material(rend.material);
                        mat.color = new Color(0.8f, 0.75f, 0.6f);
                        rend.material = mat;
                    }
                }
            }
        }

        private static void BuildPumpingStation(GameObject parent)
        {
            // Use Synty building assets for pumping station instead of procedural cubes
            string[] stationPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_Round_01.prefab"
            };
            string[] pipePaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_02.prefab"
            };
            
            var path = stationPaths[Random.Range(0, stationPaths.Length)];
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                var station = Object.Instantiate(prefab, new Vector3(-20f, 1f, 0f), Quaternion.identity, parent.transform);
                station.name = "PumpingStation";
                station.transform.localScale = new Vector3(8f, 2f, 6f);
                
                var renderers = station.GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers)
                {
                    var mat = new Material(rend.material);
                    mat.color = new Color(0.45f, 0.42f, 0.4f);
                    rend.material = mat;
                }
                
                // Pumping pipes using rock assets
                for (int i = 0; i < 3; i++)
                {
                    var pipePath = pipePaths[Random.Range(0, pipePaths.Length)];
                    var pipePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(pipePath);
                    if (pipePrefab != null)
                    {
                        var pipe = Object.Instantiate(pipePrefab, new Vector3(-20f + (i - 1) * 2f, 0.5f, 4f), Quaternion.identity, parent.transform);
                        pipe.name = "PumpPipe_" + i;
                        pipe.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                        
                        var pipeRenderers = pipe.GetComponentsInChildren<Renderer>();
                        foreach (var rend in pipeRenderers)
                        {
                            var mat = new Material(rend.material);
                            mat.color = new Color(0.5f, 0.47f, 0.45f);
                            rend.material = mat;
                        }
                    }
                }
            }
        }

        // SANCTUM helpers
        private static void BuildPagodas(GameObject parent, int count)
        {
            // Use Synty building assets for pagodas instead of procedural cylinders
            string[] pagodaPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_Round_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_01.prefab"
            };
            string[] roofPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_02.prefab"
            };
            
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f;
                float radius = 30f;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                
                // Pagoda base
                var path = pagodaPaths[Random.Range(0, pagodaPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    var pagoda = Object.Instantiate(prefab, pos + Vector3.up * 2.5f, Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0), parent.transform);
                    pagoda.name = "Pagoda_" + i;
                    pagoda.transform.localScale = new Vector3(Random.Range(4f, 6f), Random.Range(5f, 8f), Random.Range(4f, 6f));
                    
                    var renderers = pagoda.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var mat = new Material(rend.material);
                        mat.color = new Color(0.5f, 0.25f, 0.2f);
                        rend.material = mat;
                    }
                    
                    // Roof
                    var roofPath = roofPaths[Random.Range(0, roofPaths.Length)];
                    var roofPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(roofPath);
                    if (roofPrefab != null)
                    {
                        var roof = Object.Instantiate(roofPrefab, Vector3.zero, Quaternion.identity, pagoda.transform);
                        roof.name = "PagodaRoof_" + i;
                        roof.transform.localPosition = new Vector3(0, 0.8f, 0);
                        roof.transform.localScale = new Vector3(1.4f, 0.3f, 1.4f);
                        
                        var roofRenderers = roof.GetComponentsInChildren<Renderer>();
                        foreach (var rend in roofRenderers)
                        {
                            var mat = new Material(rend.material);
                            mat.color = new Color(0.2f, 0.2f, 0.25f);
                            rend.material = mat;
                        }
                    }
                }
            }
        }

        private static void BuildStoneLanterns(GameObject parent, int count)
        {
            // Use Synty rock assets for stone lanterns instead of procedural cylinders
            string[] lanternPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_02.prefab"
            };
            
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f;
                float radius = Random.Range(15f, 40f);
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                
                string path = lanternPaths[Random.Range(0, lanternPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    var lantern = Object.Instantiate(prefab, pos + Vector3.up * 0.9f, Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0), parent.transform);
                    lantern.name = "StoneLantern_" + i;
                    lantern.transform.localScale = new Vector3(0.6f, 1.5f, 0.6f);
                    
                    var renderers = lantern.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var mat = new Material(rend.material);
                        mat.color = new Color(0.9f, 0.7f, 0.3f);
                        mat.SetFloat("_Emission", 0.4f);
                        rend.material = mat;
                    }
                }
            }
        }

        private static void BuildFloatingZenGarden(GameObject parent)
        {
            // Use TerrainDemoScene rock prefabs for floating rocks instead of procedural cubes
            string[] rockPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_Overgrown_A.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_Overgrown_B.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_Overgrown_C.prefab"
            };
            
            for (int i = 0; i < 15; i++)
            {
                Vector3 pos = GetRandomPositionWithoutCenter(15f, 35f);
                pos.y = Random.Range(2f, 6f);
                
                string path = rockPaths[Random.Range(0, rockPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    var rock = Object.Instantiate(prefab, pos, Quaternion.Euler(Random.Range(-20f, 20f), Random.Range(0f, 360f), Random.Range(-20f, 20f)), parent.transform);
                    rock.name = "FloatingRock_" + i;
                    rock.transform.localScale = new Vector3(Random.Range(0.5f, 1.5f), Random.Range(0.3f, 1f), Random.Range(0.5f, 1.5f));
                    
                    var renderers = rock.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var mat = new Material(rend.material);
                        mat.color = new Color(0.35f, 0.33f, 0.3f);
                        rend.material = mat;
                    }
                }
            }
        }

        private static void BuildAnchorGate(GameObject parent)
        {
            // Use Synty building assets for dimensional anchor instead of procedural cylinders
            string[] gatePaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_Round_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_01.prefab"
            };
            string[] ringPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_01.prefab"
            };
            
            var path = gatePaths[Random.Range(0, gatePaths.Length)];
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                var gate = Object.Instantiate(prefab, new Vector3(0, 5f, 0), Quaternion.identity, parent.transform);
                gate.name = "AnchorGate";
                gate.transform.localScale = new Vector3(8f, 10f, 8f);
                
                var renderers = gate.GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers)
                {
                    var mat = new Material(rend.material);
                    mat.color = new Color(0.3f, 0.25f, 0.4f);
                    mat.SetFloat("_Emission", 0.8f);
                    rend.material = mat;
                }
                
                // Energy ring
                var ringPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ringPaths[0]);
                if (ringPrefab != null)
                {
                    var ring = Object.Instantiate(ringPrefab, Vector3.zero, Quaternion.identity, gate.transform);
                    ring.name = "AnchorRing";
                    ring.transform.localPosition = Vector3.zero;
                    ring.transform.localScale = new Vector3(1.2f, 0.1f, 1.2f);
                    
                    var ringRenderers = ring.GetComponentsInChildren<Renderer>();
                    foreach (var rend in ringRenderers)
                    {
                        var ringMat = new Material(rend.material);
                        ringMat.color = new Color(0.5f, 0.3f, 0.8f);
                        ringMat.SetFloat("_Emission", 1.2f);
                        rend.material = ringMat;
                    }
                }
            }
        }

        private static void BuildDimensionalRifts(GameObject parent, int count)
        {
            // Use TerrainDemoScene terrain for dimensional rifts instead of procedural planes
            string[] riftPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_02.prefab"
            };
            
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = GetRandomPositionWithoutCenter(20f, 48f);
                
                var riftPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(riftPaths[Random.Range(0, riftPaths.Length)]);
                if (riftPrefab != null)
                {
                    var rift = Object.Instantiate(riftPrefab, pos + Vector3.up * 3f, Quaternion.Euler(90f, Random.Range(0f, 360f), 0f), parent.transform);
                    rift.name = "DimensionalRift_" + i;
                    rift.transform.localScale = new Vector3(Random.Range(2f, 5f), 1f, Random.Range(3f, 6f));
                    
                    var renderers = rift.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var mat = new Material(rend.material);
                        mat.color = new Color(0.4f, 0.2f, 0.6f);
                        mat.SetFloat("_Emission", 1f);
                        rend.material = mat;
                    }
                }
            }
        }

        private static void BuildChangedMonks(GameObject parent, int count)
        {
            // Use Synty rock assets for changed monks instead of procedural cylinders
            string[] monkPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_02.prefab"
            };
            
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f;
                float radius = 25f;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                
                string path = monkPaths[Random.Range(0, monkPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    var monk = Object.Instantiate(prefab, pos + Vector3.up * 0.9f, Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0), parent.transform);
                    monk.name = "ChangedMonk_" + i;
                    monk.transform.localScale = new Vector3(0.6f, 1.5f, 0.6f);
                    
                    var renderers = monk.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var mat = new Material(rend.material);
                        mat.color = new Color(0.3f, 0.5f, 0.4f);
                        mat.SetFloat("_Emission", 0.3f);
                        rend.material = mat;
                    }
                }
            }
        }

        // VOLCANIC helpers
        private static void BuildVolcanicGround(GameObject parent)
        {
            // Volcanic tiles using TerrainDemoScene terrain
            string[] groundPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_02.prefab"
            };
            
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    var tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(groundPaths[Random.Range(0, groundPaths.Length)]);
                    if (tilePrefab != null)
                    {
                        var tile = Object.Instantiate(tilePrefab, new Vector3(x * 60f, -0.1f, z * 60f), Quaternion.identity, parent.transform);
                        tile.name = "VolcanicTile_" + x + "_" + z;
                        tile.transform.localScale = new Vector3(60f, 1f, 60f);
                        
                        var renderers = tile.GetComponentsInChildren<Renderer>();
                        foreach (var rend in renderers)
                        {
                            var tileMat = new Material(rend.material);
                            float variation = Random.Range(0.8f, 1.2f);
                            float baseDarkness = Random.Range(0.04f, 0.08f);
                            tileMat.color = new Color(baseDarkness * variation, baseDarkness * 0.8f * variation, baseDarkness * 0.6f * variation);
                            rend.material = tileMat;
                        }
                    }
                }
            }
        }

        private static void BuildLavaRivers(GameObject parent, int count)
        {
            // Use Synty floor assets for lava rivers instead of procedural cylinders
            string[] lavaPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_Round_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_01.prefab"
            };
            
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f;
                float radius = Random.Range(25f, 45f);
                
                var lavaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(lavaPaths[Random.Range(0, lavaPaths.Length)]);
                if (lavaPrefab != null)
                {
                    var lava = Object.Instantiate(lavaPrefab, new Vector3(Mathf.Cos(angle) * radius, -0.2f, Mathf.Sin(angle) * radius), Quaternion.Euler(0, angle * Mathf.Rad2Deg + 90f, 0), parent.transform);
                    lava.name = "LavaRiver_" + i;
                    lava.transform.localScale = new Vector3(Random.Range(4f, 8f), 0.2f, Random.Range(15f, 30f));
                    
                    var renderers = lava.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var lavaMat = new Material(rend.material);
                        lavaMat.color = new Color(1f, 0.25f, 0.05f);
                        lavaMat.SetFloat("_Emission", 2f);
                        rend.material = lavaMat;
                    }
                }
            }
        }

        private static void BuildCoolingTowers(GameObject parent, int count)
        {
            // Use Synty building assets for cooling towers instead of procedural cylinders
            string[] towerPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_Round_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_01.prefab"
            };
            
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f;
                float radius = 40f;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                
                string path = towerPaths[Random.Range(0, towerPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    var tower = Object.Instantiate(prefab, pos + Vector3.up * 4f, Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0), parent.transform);
                    tower.name = "CoolingTower_" + i;
                    tower.transform.localScale = new Vector3(8f, 8f, 8f);
                    
                    var renderers = tower.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var mat = new Material(rend.material);
                        mat.color = new Color(0.5f, 0.47f, 0.45f);
                        rend.material = mat;
                    }
                }
            }
        }

        private static void BuildIndustrialPipes(GameObject parent, int count)
        {
            // Use Synty rock assets for industrial pipes instead of procedural cylinders
            string[] pipePaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_02.prefab"
            };
            
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = GetRandomPositionWithoutCenter(15f, 50f);
                
                string path = pipePaths[Random.Range(0, pipePaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    var pipe = Object.Instantiate(prefab, pos + Vector3.up * 0.5f, Quaternion.Euler(Random.Range(-10f, 10f), Random.Range(0f, 360f), Random.Range(-10f, 10f)), parent.transform);
                    pipe.name = "IndustrialPipe_" + i;
                    pipe.transform.localScale = new Vector3(0.4f, 0.5f, 0.4f);
                    
                    var renderers = pipe.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var mat = new Material(rend.material);
                        mat.color = new Color(0.4f, 0.38f, 0.35f);
                        rend.material = mat;
                    }
                }
            }
        }

        private static void BuildActiveCrater(GameObject parent)
        {
            // Use Synty floor assets for active crater instead of procedural cylinders
            string[] craterPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_Round_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_01.prefab"
            };
            
            // Central active crater
            var craterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(craterPaths[Random.Range(0, craterPaths.Length)]);
            if (craterPrefab != null)
            {
                var crater = Object.Instantiate(craterPrefab, new Vector3(0, -0.4f, 0), Quaternion.identity, parent.transform);
                crater.name = "ActiveCrater";
                crater.transform.localScale = new Vector3(18f, 0.8f, 18f);
                
                var renderers = crater.GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers)
                {
                    var craterMat = new Material(rend.material);
                    craterMat.color = new Color(0.08f, 0.05f, 0.04f);
                    rend.material = craterMat;
                }
            }
            
            // Lava center
            var lavaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(craterPaths[Random.Range(0, craterPaths.Length)]);
            if (lavaPrefab != null)
            {
                var lava = Object.Instantiate(lavaPrefab, new Vector3(0, -0.3f, 0), Quaternion.identity, parent.transform);
                lava.name = "CraterLava";
                lava.transform.localScale = new Vector3(12f, 0.4f, 12f);
                
                var renderers = lava.GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers)
                {
                    var lavaMat = new Material(rend.material);
                    lavaMat.color = new Color(1f, 0.2f, 0f);
                    lavaMat.SetFloat("_Emission", 3f);
                    rend.material = lavaMat;
                }
            }
        }

        private static void BuildAshSources(GameObject parent, int count)
        {
            // Use pebble assets from Stylized Nature MegaKit for ash sources instead of procedural cubes
            string[] ashPaths = {
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Square_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Square_2.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Round_1.fbx"
            };
            
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = GetRandomPositionWithoutCenter(20f, 55f);
                
                string path = ashPaths[Random.Range(0, ashPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    var source = Object.Instantiate(prefab, pos + Vector3.up * 0.65f, Quaternion.Euler(0, Random.Range(0, 360), 0), parent.transform);
                    source.name = "AshSource_" + i;
                    source.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
                    
                    var renderers = source.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var mat = new Material(rend.material);
                        mat.color = new Color(0.2f, 0.18f, 0.15f);
                        rend.material = mat;
                    }
                }
            }
        }

        private static void BuildFailedEquipment(GameObject parent)
        {
            // Use Synty building assets for failed geothermal equipment instead of procedural cubes
            string[] equipmentPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_Round_01.prefab"
            };
            string[] lightPaths = {
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Round_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Round_2.fbx"
            };
            
            var path = equipmentPaths[Random.Range(0, equipmentPaths.Length)];
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                var equipment = Object.Instantiate(prefab, new Vector3(-25f, 1f, 0f), Quaternion.identity, parent.transform);
                equipment.name = "FailedEquipment";
                equipment.transform.localScale = new Vector3(8f, 2f, 6f);
                
                var renderers = equipment.GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers)
                {
                    var mat = new Material(rend.material);
                    mat.color = new Color(0.3f, 0.28f, 0.25f);
                    rend.material = mat;
                }
                
                // Warning lights using pebble assets
                for (int i = 0; i < 3; i++)
                {
                    var lightPath = lightPaths[Random.Range(0, lightPaths.Length)];
                    var lightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(lightPath);
                    if (lightPrefab != null)
                    {
                        var light = Object.Instantiate(lightPrefab, Vector3.zero, Quaternion.identity, equipment.transform);
                        light.name = "WarningLight_" + i;
                        light.transform.localPosition = new Vector3(0.5f, 0.6f, (i - 1) * 0.3f);
                        light.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                        
                        var lightRenderers = light.GetComponentsInChildren<Renderer>();
                        foreach (var rend in lightRenderers)
                        {
                            var mat = new Material(rend.material);
                            mat.color = new Color(0.9f, 0.1f, 0.1f);
                            mat.SetFloat("_Emission", 1f);
                            rend.material = mat;
                        }
                    }
                }
            }
        }

        // =====================================================
        // HYDRO ARENA - NEW VISUAL SYSTEM
        // =====================================================

        private static void BuildDeepOceanFloor(GameObject parent)
        {
            // Deep ocean floor using TerrainDemoScene terrain
            string[] floorPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_02.prefab"
            };
            
            var floorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(floorPaths[Random.Range(0, floorPaths.Length)]);
            if (floorPrefab != null)
            {
                var oceanFloor = Object.Instantiate(floorPrefab, new Vector3(0, -15f, 0), Quaternion.identity, parent.transform);
                oceanFloor.name = "DeepOceanFloor";
                oceanFloor.transform.localScale = new Vector3(15f, 1f, 15f);
                
                var renderers = oceanFloor.GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers)
                {
                    var floorMat = new Material(rend.material);
                    floorMat.color = new Color(0.02f, 0.05f, 0.12f);
                    floorMat.SetFloat("_Smoothness", 0.3f);
                    rend.material = floorMat;
                }
            }
            
            // Add sand dunes using Synty rock assets
            string[] dunePaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_02.prefab"
            };
            
            for (int i = 0; i < 20; i++)
            {
                Vector3 pos = new Vector3(
                    Random.Range(-70f, 70f), 
                    -14f, 
                    Random.Range(-70f, 70f)
                );
                
                var dunePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(dunePaths[Random.Range(0, dunePaths.Length)]);
                if (dunePrefab != null)
                {
                    var dune = Object.Instantiate(dunePrefab, pos, Quaternion.Euler(0, Random.Range(0f, 360f), 0), parent.transform);
                    dune.name = "OceanDune_" + i;
                    dune.transform.localScale = new Vector3(
                        Random.Range(8f, 20f), 
                        Random.Range(1f, 4f), 
                        Random.Range(8f, 20f)
                    );
                    
                    var renderers = dune.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var duneMat = new Material(rend.material);
                        duneMat.color = new Color(0.03f, 0.06f, 0.1f);
                        rend.material = duneMat;
                    }
                }
            }
        }

        private static void BuildRealisticWaterSurface(GameObject parent)
        {
            // Large transparent water surface using TerrainDemoScene terrain
            string[] waterPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_02.prefab"
            };
            
            var waterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(waterPaths[Random.Range(0, waterPaths.Length)]);
            if (waterPrefab != null)
            {
                var waterSurface = Object.Instantiate(waterPrefab, new Vector3(0, -0.5f, 0), Quaternion.identity, parent.transform);
                waterSurface.name = "WaterSurface";
                waterSurface.transform.localScale = new Vector3(15f, 1f, 15f);
                
                var renderers = waterSurface.GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers)
                {
                    var waterMat = new Material(rend.material);
                    waterMat.color = new Color(0.08f, 0.25f, 0.45f, 0.7f);
                    waterMat.SetFloat("_Smoothness", 0.95f);
                    waterMat.SetFloat("_Metallic", 0.1f);
                    rend.material = waterMat;
                }
            }
            
            // Second layer for depth effect
            var deepPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(waterPaths[Random.Range(0, waterPaths.Length)]);
            if (deepPrefab != null)
            {
                var waterDeep = Object.Instantiate(deepPrefab, new Vector3(0, -3f, 0), Quaternion.identity, parent.transform);
                waterDeep.name = "WaterDeepLayer";
                waterDeep.transform.localScale = new Vector3(15f, 1f, 15f);
                
                var renderers = waterDeep.GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers)
                {
                    var deepMat = new Material(rend.material);
                    deepMat.color = new Color(0.04f, 0.15f, 0.3f);
                    deepMat.SetFloat("_Smoothness", 0.8f);
                    rend.material = deepMat;
                }
            }
        }

        private static void BuildCentralMarinePlatform(GameObject parent)
        {
            // Large central island using Synty floor assets
            string[] platformPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_Round_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_01.prefab"
            };
            string[] rockPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_Overgrown_A.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_Overgrown_B.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_Overgrown_C.prefab"
            };
            
            // Create main platform base
            var mainBasePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(platformPaths[Random.Range(0, platformPaths.Length)]);
            if (mainBasePrefab != null)
            {
                var mainBase = Object.Instantiate(mainBasePrefab, new Vector3(0, -0.5f, 0), Quaternion.identity, parent.transform);
                mainBase.name = "CentralPlatformBase";
                mainBase.transform.localScale = new Vector3(45f, 3f, 45f);
                
                var renderers = mainBase.GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers)
                {
                    var baseMat = new Material(rend.material);
                    baseMat.color = new Color(0.25f, 0.3f, 0.35f);
                    rend.material = baseMat;
                }
                
                mainBase.tag = "Ground";
            }
            
            // Top surface - walkable area
            var topPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(platformPaths[Random.Range(0, platformPaths.Length)]);
            if (topPrefab != null)
            {
                var topSurface = Object.Instantiate(topPrefab, new Vector3(0, 1f, 0), Quaternion.identity, parent.transform);
                topSurface.name = "CentralPlatformTop";
                topSurface.transform.localScale = new Vector3(40f, 0.5f, 40f);
                
                var renderers = topSurface.GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers)
                {
                    var topMat = new Material(rend.material);
                    topMat.color = new Color(0.5f, 0.55f, 0.5f);
                    rend.material = topMat;
                }
                
                topSurface.tag = "Ground";
            }
            
            // Rock formations around the edge
            for (int i = 0; i < 16; i++)
            {
                float angle = (i / 16f) * Mathf.PI * 2f;
                float radius = 18f;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 1f, Mathf.Sin(angle) * radius);
                
                string path = rockPaths[Random.Range(0, rockPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    var rock = Object.Instantiate(prefab, pos, Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0), parent.transform);
                    float s = Random.Range(1.5f, 3f);
                    rock.transform.localScale = new Vector3(s, s * 1.5f, s);
                    rock.AddComponent<DestructibleEnvironment>();
                }
            }
            
            // Central elevated structure
            var towerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(platformPaths[Random.Range(0, platformPaths.Length)]);
            if (towerPrefab != null)
            {
                var centerTower = Object.Instantiate(towerPrefab, new Vector3(0, 3f, 0), Quaternion.identity, parent.transform);
                centerTower.name = "CenterTower";
                centerTower.transform.localScale = new Vector3(8f, 4f, 8f);
                
                var renderers = centerTower.GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers)
                {
                    var towerMat = new Material(rend.material);
                    towerMat.color = new Color(0.35f, 0.4f, 0.45f);
                    rend.material = towerMat;
                }
                
                centerTower.tag = "Ground";
            }
            
            // Tower top platform
            var topTowerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(platformPaths[Random.Range(0, platformPaths.Length)]);
            if (topTowerPrefab != null)
            {
                var towerTop = Object.Instantiate(topTowerPrefab, new Vector3(0, 5.5f, 0), Quaternion.identity, parent.transform);
                towerTop.name = "TowerTop";
                towerTop.transform.localScale = new Vector3(10f, 0.3f, 10f);
                
                Material topMat = null;
                var topSurfaceObj = parent.transform.Find("CentralPlatformTop");
                if (topSurfaceObj != null)
                {
                    var topRend = topSurfaceObj.GetComponentInChildren<Renderer>();
                    if (topRend != null) topMat = new Material(topRend.material);
                }
                
                var renderers = towerTop.GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers)
                {
                    if (topMat != null) rend.material = topMat;
                }
                
                towerTop.tag = "Ground";
            }
        }

        private static void BuildSatelliteIslands(GameObject parent, int count)
        {
            string[] platformPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_Round_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_01.prefab"
            };
            string[] rockPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_Overgrown_A.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_Overgrown_B.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_Overgrown_C.prefab"
            };
            
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f;
                float radius = Random.Range(35f, 55f);
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                
                // Island base using Synty floor assets
                var islandPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(platformPaths[Random.Range(0, platformPaths.Length)]);
                if (islandPrefab != null)
                {
                    var islandBase = Object.Instantiate(islandPrefab, pos + Vector3.down * 0.5f, Quaternion.identity, parent.transform);
                    islandBase.name = "Island_" + i;
                    float islandSize = Random.Range(8f, 15f);
                    islandBase.transform.localScale = new Vector3(islandSize, 2f, islandSize);
                    
                    var renderers = islandBase.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var islandMat = new Material(rend.material);
                        islandMat.color = new Color(0.4f, 0.45f, 0.42f);
                        rend.material = islandMat;
                    }
                    
                    islandBase.tag = "Ground";
                }
                
                // Rock formations on island
                int rocksOnIsland = Random.Range(2, 5);
                for (int r = 0; r < rocksOnIsland; r++)
                {
                    string path = rockPaths[Random.Range(0, rockPaths.Length)];
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        Vector3 rockPos = pos + new Vector3(
                            Random.Range(-3f, 3f), 
                            1f, 
                            Random.Range(-3f, 3f)
                        );
                        var rock = Object.Instantiate(prefab, rockPos, Quaternion.Euler(0, Random.Range(0f, 360f), 0), parent.transform);
                        float s = Random.Range(0.8f, 1.5f);
                        rock.transform.localScale = Vector3.one * s;
                        rock.AddComponent<DestructibleEnvironment>();
                    }
                }
            }
        }

        private static void BuildMarineTowers(GameObject parent, int count)
        {
            string[] towerPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Cliff_Pillar_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Cliff_Pillar_02.prefab"
            };
            string[] lightPaths = {
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Round_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Round_2.fbx"
            };
            
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f + 0.4f;
                float radius = 28f;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                
                string path = towerPaths[Random.Range(0, towerPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    var tower = Object.Instantiate(prefab, pos, Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0), parent.transform);
                    float s = Random.Range(1.2f, 2f);
                    tower.transform.localScale = new Vector3(s, s * 3f, s);
                    
                    // Light at top of tower using pebble asset
                    var lightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(lightPaths[Random.Range(0, lightPaths.Length)]);
                    if (lightPrefab != null)
                    {
                        var light = Object.Instantiate(lightPrefab, tower.transform);
                        light.name = "TowerLight_" + i;
                        light.transform.localPosition = new Vector3(0, 3f, 0);
                        light.transform.localScale = Vector3.one * 0.5f;
                        
                        var renderers = light.GetComponentsInChildren<Renderer>();
                        foreach (var rend in renderers)
                        {
                            var lightMat = new Material(rend.material);
                            lightMat.color = new Color(0.9f, 0.8f, 0.3f);
                            lightMat.SetFloat("_Emission", 1f);
                            rend.material = lightMat;
                        }
                    }
                }
            }
        }

        private static void BuildConnectingBridges(GameObject parent)
        {
            // Wooden/stone bridges connecting center to satellites using Synty floor assets
            string[] bridgePaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_Round_01.prefab"
            };
            string[] supportPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Cliff_Pillar_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Cliff_Pillar_02.prefab"
            };
            
            for (int i = 0; i < 4; i++)
            {
                float angle = (i / 4f) * Mathf.PI * 2f;
                
                // Bridge from center to mid-point
                Vector3 start = new Vector3(Mathf.Cos(angle) * 20f, 1f, Mathf.Sin(angle) * 20f);
                Vector3 end = new Vector3(Mathf.Cos(angle) * 32f, 0.5f, Mathf.Sin(angle) * 32f);
                Vector3 mid = (start + end) / 2f;
                float length = Vector3.Distance(start, end);
                
                var bridgePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bridgePaths[Random.Range(0, bridgePaths.Length)]);
                if (bridgePrefab != null)
                {
                    var bridge = Object.Instantiate(bridgePrefab, mid, Quaternion.Euler(0, -angle * Mathf.Rad2Deg + 90f, 0), parent.transform);
                    bridge.name = "Bridge_" + i;
                    bridge.transform.localScale = new Vector3(3f, 0.3f, length);
                    
                    var renderers = bridge.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var bridgeMat = new Material(rend.material);
                        bridgeMat.color = new Color(0.4f, 0.35f, 0.3f);
                        rend.material = bridgeMat;
                    }
                    
                    bridge.tag = "Ground";
                }
                
                // Bridge supports
                for (int s = 0; s < 3; s++)
                {
                    float t = s / 2f;
                    Vector3 supportPos = Vector3.Lerp(start, end, t);
                    supportPos.y = -2f;
                    
                    var supportPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(supportPaths[Random.Range(0, supportPaths.Length)]);
                    if (supportPrefab != null)
                    {
                        var support = Object.Instantiate(supportPrefab, supportPos, Quaternion.identity, parent.transform);
                        support.name = "BridgeSupport_" + i + "_" + s;
                        support.transform.localScale = new Vector3(0.8f, 4f, 0.8f);
                        
                        var renderers = support.GetComponentsInChildren<Renderer>();
                        foreach (var rend in renderers)
                        {
                            var supportMat = new Material(rend.material);
                            supportMat.color = new Color(0.3f, 0.3f, 0.35f);
                            rend.material = supportMat;
                        }
                    }
                }
            }
        }

        private static void BuildUnderwaterVegetation(GameObject parent)
        {
            string[] seaweedPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Fern_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Fern_02.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Fern_03.prefab"
            };
            
            // Underwater kelp/seaweed forests
            for (int i = 0; i < 40; i++)
            {
                Vector3 pos = new Vector3(
                    Random.Range(-60f, 60f),
                    -3f,
                    Random.Range(-60f, 60f)
                );
                
                // Don't place too close to center platform
                if (Vector3.Distance(pos, Vector3.zero) < 25f) continue;
                
                string path = seaweedPaths[Random.Range(0, seaweedPaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    var seaweed = Object.Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0f, 360f), 0), parent.transform);
                    float s = Random.Range(1.5f, 3f);
                    seaweed.transform.localScale = new Vector3(s, s * 2f, s);
                    
                    // Dark green underwater color
                    Material seaMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    seaMat.color = new Color(0.1f, 0.35f, 0.25f);
                    seaMat.SetFloat("_Emission", 0.1f);
                    seaweed.GetComponent<Renderer>().material = seaMat;
                }
            }
        }

        private static void BuildCoralReefs(GameObject parent, int count)
        {
            // Use Synty stalactite assets for coral instead of procedural cylinders
            string[] coralPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Stalactite_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Stalactite_02.prefab"
            };
            
            Color[] coralColors = {
                new Color(0.9f, 0.4f, 0.5f),
                new Color(0.4f, 0.8f, 0.9f),
                new Color(0.8f, 0.5f, 0.9f),
                new Color(0.9f, 0.7f, 0.3f),
                new Color(0.5f, 0.9f, 0.6f)
            };
            
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = new Vector3(
                    Random.Range(-50f, 50f),
                    -2f,
                    Random.Range(-50f, 50f)
                );
                
                if (Vector3.Distance(pos, Vector3.zero) < 30f) continue;
                
                // Coral cluster
                int coralPieces = Random.Range(3, 8);
                for (int c = 0; c < coralPieces; c++)
                {
                    Vector3 offset = new Vector3(
                        Random.Range(-2f, 2f), 
                        0, 
                        Random.Range(-2f, 2f)
                    );
                    
                    var coralPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(coralPaths[Random.Range(0, coralPaths.Length)]);
                    if (coralPrefab != null)
                    {
                        var coral = Object.Instantiate(coralPrefab, pos + offset, Quaternion.Euler(
                            Random.Range(-10f, 10f), 
                            Random.Range(0f, 360f), 
                            Random.Range(-10f, 10f)
                        ), parent.transform);
                        coral.name = "Coral_" + i + "_" + c;
                        coral.transform.localScale = new Vector3(
                            Random.Range(0.3f, 0.8f), 
                            Random.Range(1f, 3f), 
                            Random.Range(0.3f, 0.8f)
                        );
                        
                        var renderers = coral.GetComponentsInChildren<Renderer>();
                        foreach (var rend in renderers)
                        {
                            var coralMat = new Material(rend.material);
                            coralMat.color = coralColors[Random.Range(0, coralColors.Length)];
                            coralMat.SetFloat("_Emission", 0.5f);
                            rend.material = coralMat;
                        }
                    }
                }
            }
        }

        private static void BuildSurfaceDetails(GameObject parent)
        {
            // Floating lily pads using pebble assets
            string[] lilyPaths = {
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Round_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Round_2.fbx"
            };
            
            for (int i = 0; i < 25; i++)
            {
                Vector3 pos = new Vector3(
                    Random.Range(-70f, 70f),
                    -0.4f,
                    Random.Range(-70f, 70f)
                );
                
                if (Vector3.Distance(pos, Vector3.zero) < 25f) continue;
                
                var lilyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(lilyPaths[Random.Range(0, lilyPaths.Length)]);
                if (lilyPrefab != null)
                {
                    var lilypad = Object.Instantiate(lilyPrefab, pos, Quaternion.identity, parent.transform);
                    lilypad.name = "LilyPad_" + i;
                    lilypad.transform.localScale = new Vector3(
                        Random.Range(0.8f, 1.5f), 
                        0.05f, 
                        Random.Range(0.8f, 1.5f)
                    );
                    
                    var renderers = lilypad.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var padMat = new Material(rend.material);
                        padMat.color = new Color(0.2f, 0.5f, 0.25f);
                        rend.material = padMat;
                    }
                }
            }
            
            // Floating buoys/markers using pebble assets
            string[] buoyPaths = {
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Round_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Round_2.fbx"
            };
            
            for (int i = 0; i < 8; i++)
            {
                float angle = (i / 8f) * Mathf.PI * 2f;
                float radius = 45f;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, -0.3f, Mathf.Sin(angle) * radius);
                
                var buoyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(buoyPaths[Random.Range(0, buoyPaths.Length)]);
                if (buoyPrefab != null)
                {
                    var buoy = Object.Instantiate(buoyPrefab, pos, Quaternion.identity, parent.transform);
                    buoy.name = "Buoy_" + i;
                    buoy.transform.localScale = Vector3.one * 0.6f;
                    
                    var renderers = buoy.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var buoyMat = new Material(rend.material);
                        buoyMat.color = new Color(0.9f, 0.2f, 0.2f);
                        buoyMat.SetFloat("_Emission", 0.5f);
                        rend.material = buoyMat;
                    }
                }
            }
        }

        private static void BuildMarineAtmosphere(GameObject parent)
        {
            // Floating particle effect using pebble assets
            string[] particlePaths = {
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Round_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Round_2.fbx"
            };
            
            for (int i = 0; i < 30; i++)
            {
                Vector3 pos = new Vector3(
                    Random.Range(-60f, 60f),
                    Random.Range(2f, 15f),
                    Random.Range(-60f, 60f)
                );
                
                var particlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(particlePaths[Random.Range(0, particlePaths.Length)]);
                if (particlePrefab != null)
                {
                    var particle = Object.Instantiate(particlePrefab, pos, Quaternion.identity, parent.transform);
                    particle.name = "AtmosphereParticle_" + i;
                    particle.transform.localScale = Vector3.one * Random.Range(0.1f, 0.3f);
                    
                    var renderers = particle.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var partMat = new Material(rend.material);
                        partMat.color = new Color(0.7f, 0.9f, 1f, 0.3f);
                        partMat.SetFloat("_Emission", 0.3f);
                        rend.material = partMat;
                    }
                }
            }
            
            // Light ray cones from above using Synty stalactite assets
            string[] rayPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Stalactite_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Stalactite_02.prefab"
            };
            
            for (int i = 0; i < 6; i++)
            {
                Vector3 pos = new Vector3(
                    Random.Range(-30f, 30f),
                    20f,
                    Random.Range(-30f, 30f)
                );
                
                var rayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(rayPaths[Random.Range(0, rayPaths.Length)]);
                if (rayPrefab != null)
                {
                    var lightRay = Object.Instantiate(rayPrefab, pos, Quaternion.identity, parent.transform);
                    lightRay.name = "LightRay_" + i;
                    lightRay.transform.localScale = new Vector3(2f, 20f, 2f);
                    
                    var renderers = lightRay.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        var rayMat = new Material(rend.material);
                        rayMat.color = new Color(0.6f, 0.8f, 0.9f, 0.2f);
                        rayMat.SetFloat("_Emission", 0.2f);
                        rend.material = rayMat;
                    }
                }
            }
        }

        /// <summary>
        /// Premium helper: Build grass patches using real grass assets
        /// </summary>
        private static void BuildGrassPatches(GameObject parent, int count)
        {
            string[] grassPaths = {
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Grass_Common_Short.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Grass_Common_Tall.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Grass_Wispy_Short.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Grass_Wispy_Tall.fbx"
            };
            
            for (int i = 0; i < count; i++)
            {
                Vector3 clusterCenter = GetRandomPositionWithoutCenter(8f, 50f);
                int grassInCluster = Random.Range(5, 12);
                
                for (int j = 0; j < grassInCluster; j++)
                {
                    Vector3 offset = Random.insideUnitSphere * 2f;
                    offset.y = 0;
                    Vector3 pos = clusterCenter + offset;
                    
                    string path = grassPaths[Random.Range(0, grassPaths.Length)];
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        var grass = Object.Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), parent.transform);
                        float s = Random.Range(0.8f, 1.5f);
                        grass.transform.localScale = Vector3.one * s;
                    }
                }
            }
        }

        /// <summary>
        /// Premium helper: Build pebble debris scattered around
        /// </summary>
        private static void BuildPebbleDebris(GameObject parent, int count)
        {
            string[] pebblePaths = {
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Round_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Round_2.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Round_3.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Square_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Square_2.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/Pebble_Square_3.fbx"
            };
            
            for (int i = 0; i < count; i++)
            {
                string path = pebblePaths[Random.Range(0, pebblePaths.Length)];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    Vector3 pos = GetRandomPositionWithoutCenter(5f, 52f);
                    var pebble = Object.Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), parent.transform);
                    float s = Random.Range(0.4f, 1.2f);
                    pebble.transform.localScale = Vector3.one * s;
                }
            }
        }

        /// <summary>
        /// Generates a thumbnail for the specified map and saves it to Resources/MapThumbnails/
        /// </summary>
        private static void GenerateThumbnailForMap(string mapId)
        {
#if UNITY_EDITOR
            // Create temporary camera
            GameObject camObj = new GameObject("ThumbnailCamera_Temp");
            Camera cam = camObj.AddComponent<Camera>();
            
            // Setup camera for aerial view
            cam.transform.position = new Vector3(0, 60, 0);
            cam.transform.rotation = Quaternion.Euler(90, 0, 0);
            cam.orthographic = true;
            cam.orthographicSize = 40f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 1f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;
            
            // Create render texture
            int resolution = 512;
            RenderTexture rt = new RenderTexture(resolution, resolution, 24);
            cam.targetTexture = rt;
            
            // Render
            cam.Render();
            
            // Read pixels
            RenderTexture.active = rt;
            Texture2D screenshot = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
            screenshot.Apply();
            
            // Encode to PNG
            byte[] bytes = screenshot.EncodeToPNG();
            
            // Save to file
            string directoryPath = System.IO.Path.Combine(Application.dataPath, "Resources/MapThumbnails");
            if (!System.IO.Directory.Exists(directoryPath))
            {
                System.IO.Directory.CreateDirectory(directoryPath);
            }
            
            string filePath = System.IO.Path.Combine(directoryPath, $"thumbnail_{mapId}.png");
            System.IO.File.WriteAllBytes(filePath, bytes);
            
            // Cleanup
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(camObj);
            Object.DestroyImmediate(screenshot);
            
            Debug.Log($"[ArenaEnvironmentBuilder] Thumbnail generated: {filePath}");
            
            // Import and configure as sprite
            string relativePath = $"Assets/Resources/MapThumbnails/thumbnail_{mapId}.png";
            AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
            
            TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100;
                importer.filterMode = FilterMode.Trilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
#endif
        }

        // ============================================================
        // ZCB-ALPHA PREMIUM: La Zona Cero - Original Arena
        // ============================================================
        /// <summary>
        /// Builds the ZCB-ALPHA environment - the first accessible map with dimensional rift.
        /// Features: Concentric rings (Safe→Transition→Danger), Nucleo de Contención,
        /// Campamento de Evacuación Fallida, and the iconic Dimensional Rift at perimeter.
        /// </summary>
        public static void BuildAlphaEnvironmentPremium()
        {
            var envGroup = new GameObject("Environment_ZCB_ALPHA_Premium");
            
            // Gradient lighting: Natural center → Dimensional perimeter
            RenderSettings.ambientLight = new Color(0.25f, 0.28f, 0.22f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.15f, 0.12f, 0.18f, 1f);
            RenderSettings.fogDensity = 0.01f;
            RenderSettings.fogMode = FogMode.Exponential;
            
            // Skybox gradient from blue to purple
            RenderSettings.skybox = null; // Use gradient sky
            
            // 1. Build Concentric Ground Rings
            BuildZCBGroundRings(envGroup);
            
            // 2. Nucleo de Contención (Center - Safe Zone)
            BuildContainmentCoreZCB(envGroup);
            
            // 3. Campamento de Evacuación Fallida (Mid-ring - Transition)
            BuildEvacuationCamp(envGroup);
            
            // 4. La Brecha Dimensional (Perimeter - Danger Zone)
            BuildDimensionalRiftZCB(envGroup);
            
            // 5. BIOHORIZON Equipment scattered around
            BuildBIOHORIZONEquipment(envGroup);
            
            // 6. Vegetation with bioluminescence near perimeter
            BuildBioluminescentVegetation(envGroup);
            
            // Generate thumbnail
            GenerateThumbnailForMap("alpha");
        }
        
        private static void BuildZCBGroundRings(GameObject parent)
        {
            // Central Safe Zone (30x30m) - Natural grass
            string[] groundPaths = {
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/TerrainPlane_02.prefab"
            };
            
            var centerGround = AssetDatabase.LoadAssetAtPath<GameObject>(groundPaths[0]);
            if (centerGround != null)
            {
                var ground = Object.Instantiate(centerGround, Vector3.zero, Quaternion.identity, parent.transform);
                ground.name = "ZCB_CenterZone";
                ground.transform.localScale = new Vector3(30f, 1f, 30f);
                
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.35f, 0.55f, 0.35f); // Healthy green
                var renderers = ground.GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers) rend.material = mat;
            }
            
            // Transition Ring (30-50m) - Mixed with patches of bioluminescence
            for (int i = 0; i < 8; i++)
            {
                float angle = (i / 8f) * Mathf.PI * 2f;
                float radius = 40f;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                
                var ringPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(groundPaths[i % 2]);
                if (ringPrefab != null)
                {
                    var ring = Object.Instantiate(ringPrefab, pos, Quaternion.identity, parent.transform);
                    ring.name = $"ZCB_Transition_{i}";
                    ring.transform.localScale = new Vector3(15f, 1f, 15f);
                    
                    // Mixed colors - some green, some showing bioluminescence
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    float t = i / 8f;
                    mat.color = Color.Lerp(
                        new Color(0.35f, 0.55f, 0.35f),
                        new Color(0.2f, 0.4f, 0.3f), // Slightly alien
                        t
                    );
                    var renderers = ring.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers) rend.material = mat;
                }
            }
            
            // Outer Ring (50-70m) - Bioluminescent, dimensional influence
            for (int i = 0; i < 12; i++)
            {
                float angle = (i / 12f) * Mathf.PI * 2f;
                float radius = 60f;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, -0.1f, Mathf.Sin(angle) * radius);
                
                var outerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(groundPaths[i % 2]);
                if (outerPrefab != null)
                {
                    var outer = Object.Instantiate(outerPrefab, pos, Quaternion.identity, parent.transform);
                    outer.name = $"ZCB_Outer_{i}";
                    outer.transform.localScale = new Vector3(12f, 1f, 12f);
                    
                    // Bioluminescent purple-green color
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = new Color(0.15f, 0.35f, 0.25f);
                    mat.SetFloat("_Emission", 0.3f);
                    mat.SetColor("_EmissionColor", new Color(0.2f, 0.4f, 0.3f));
                    var renderers = outer.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers) rend.material = mat;
                }
            }
        }
        
        private static void BuildContainmentCoreZCB(GameObject parent)
        {
            // Central containment structure - partially collapsed
            string[] structurePaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_Round_01.prefab"
            };
            
            var structurePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(structurePaths[0]);
            if (structurePrefab != null)
            {
                // Main structure
                var core = Object.Instantiate(structurePrefab, new Vector3(0, 0.5f, 0), Quaternion.identity, parent.transform);
                core.name = "Nucleo_Contencion";
                core.transform.localScale = new Vector3(8f, 1f, 8f);
                
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.3f, 0.32f, 0.35f); // Steel gray
                var renderers = core.GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers) rend.material = mat;
                
                // Add as destructible
                core.AddComponent<DestructibleEnvironment>();
            }
            
            // Scattered containment crystals (broken)
            string[] crystalPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_01.prefab"
            };
            
            for (int i = 0; i < 6; i++)
            {
                float angle = (i / 6f) * Mathf.PI * 2f;
                float radius = 8f;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0.2f, Mathf.Sin(angle) * radius);
                
                var crystalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(crystalPaths[0]);
                if (crystalPrefab != null)
                {
                    var crystal = Object.Instantiate(crystalPrefab, pos, Quaternion.Euler(Random.Range(-30f, 30f), angle * Mathf.Rad2Deg, Random.Range(-30f, 30f)), parent.transform);
                    crystal.name = $"BrokenCrystal_{i}";
                    crystal.transform.localScale = new Vector3(0.5f, 0.8f, 0.5f);
                    
                    // Glowing crystal material
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = new Color(0.3f, 0.7f, 0.9f, 0.6f);
                    mat.SetFloat("_Emission", 0.8f);
                    mat.SetColor("_EmissionColor", new Color(0.2f, 0.6f, 0.8f));
                    var renderers = crystal.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers) rend.material = mat;
                }
            }
        }
        
        private static void BuildEvacuationCamp(GameObject parent)
        {
            // Military vehicles in defensive circle
            string[] vehiclePaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Vehicles/SM_Veh_Car_Muscle_01.prefab"
            };
            
            for (int i = 0; i < 4; i++)
            {
                float angle = (i / 4f) * Mathf.PI * 2f + Mathf.PI / 4f;
                float radius = 25f;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0.5f, Mathf.Sin(angle) * radius);
                
                var vehiclePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(vehiclePaths[0]);
                if (vehiclePrefab != null)
                {
                    var vehicle = Object.Instantiate(vehiclePrefab, pos, Quaternion.Euler(0, angle * Mathf.Rad2Deg + 90f, 0), parent.transform);
                    vehicle.name = $"AbandonedVehicle_{i}";
                    vehicle.transform.localScale = Vector3.one * 1.2f;
                    
                    // Military olive drab
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = new Color(0.25f, 0.28f, 0.15f);
                    var renderers = vehicle.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers) rend.material = mat;
                    
                    vehicle.AddComponent<DestructibleEnvironment>();
                }
            }
            
            // Supply crates scattered
            string[] cratePaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_01.prefab"
            };
            
            for (int i = 0; i < 8; i++)
            {
                Vector3 pos = GetRandomPositionWithoutCenter(20f, 30f);
                pos.y = 0.3f;
                
                var cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(cratePaths[0]);
                if (cratePrefab != null)
                {
                    var crate = Object.Instantiate(cratePrefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), parent.transform);
                    crate.name = $"SupplyCrate_{i}";
                    crate.transform.localScale = new Vector3(1f, 0.6f, 0.8f);
                    
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = new Color(0.4f, 0.35f, 0.25f); // Cardboard brown
                    var renderers = crate.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers) rend.material = mat;
                }
            }
        }
        
        private static void BuildDimensionalRiftZCB(GameObject parent)
        {
            // Spawn the Dimensional Rift at the perimeter
            float riftRadius = 55f;
            Vector3 riftPos = new Vector3(0, 2f, riftRadius);
            
            // Create the rift GameObject
            var riftGO = new GameObject("La_Brecha_Dimensional");
            riftGO.transform.SetParent(parent.transform);
            riftGO.transform.position = riftPos;
            
            // Add the DimensionalRift component
            var rift = riftGO.AddComponent<DimensionalRift>();
            rift.pulseSpeed = 1.2f;
            rift.pulseIntensity = 2f;
            rift.baseScale = new Vector3(3f, 6f, 3f);
            rift.effectRadius = 20f;
            rift.damagePerSecond = 3f;
            
            // Bioluminescent crystals around the rift
            string[] crystalPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_02.prefab"
            };
            
            for (int i = 0; i < 12; i++)
            {
                float angle = (i / 12f) * Mathf.PI * 2f;
                float dist = Random.Range(8f, 15f);
                Vector3 pos = riftPos + new Vector3(Mathf.Cos(angle) * dist, 0, Mathf.Sin(angle) * dist);
                
                var crystalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(crystalPaths[i % 2]);
                if (crystalPrefab != null)
                {
                    var crystal = Object.Instantiate(crystalPrefab, pos, Quaternion.Euler(Random.Range(-20f, 20f), Random.Range(0, 360), Random.Range(-20f, 20f)), parent.transform);
                    crystal.name = $"XSpaceCrystal_{i}";
                    crystal.transform.localScale = Vector3.one * Random.Range(0.5f, 1.5f);
                    
                    // Glowing X-Space color
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = new Color(0.5f, 0.1f, 0.7f, 0.8f);
                    mat.SetFloat("_Emission", 1f);
                    mat.SetColor("_EmissionColor", new Color(0.4f, 0f, 0.8f));
                    var renderers = crystal.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers) rend.material = mat;
                }
            }
        }
        
        private static void BuildBIOHORIZONEquipment(GameObject parent)
        {
            // Scattered BIOHORIZON equipment and terminals
            string[] equipPaths = {
                "Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Floor_01.prefab"
            };
            
            // Terminals
            for (int i = 0; i < 3; i++)
            {
                Vector3 pos = GetRandomPositionWithoutCenter(15f, 35f);
                pos.y = 0.8f;
                
                var equipPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(equipPaths[0]);
                if (equipPrefab != null)
                {
                    var terminal = Object.Instantiate(equipPrefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), parent.transform);
                    terminal.name = $"BIOHORIZON_Terminal_{i}";
                    terminal.transform.localScale = new Vector3(1.5f, 1.6f, 1f);
                    
                    // BIOHORIZON white/grey
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = new Color(0.85f, 0.87f, 0.9f);
                    mat.SetFloat("_Emission", 0.3f);
                    mat.SetColor("_EmissionColor", new Color(0.2f, 0.4f, 0.6f)); // Blue screen glow
                    var renderers = terminal.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers) rend.material = mat;
                }
            }
        }
        
        private static void BuildBioluminescentVegetation(GameObject parent)
        {
            // Trees with bioluminescence near perimeter
            string[] treePaths = {
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/DeadTree_1.fbx",
                "Assets/Models/Stylized Nature MegaKit[Standard]/FBX/DeadTree_2.fbx"
            };
            
            for (int i = 0; i < 15; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float radius = Random.Range(40f, 55f);
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                
                var treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(treePaths[i % 2]);
                if (treePrefab != null)
                {
                    var tree = Object.Instantiate(treePrefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), parent.transform);
                    tree.name = $"MutatedTree_{i}";
                    tree.transform.localScale = Vector3.one * Random.Range(0.8f, 1.5f);
                    
                    // Mutated purple-green color with bioluminescence
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = new Color(0.15f, 0.35f, 0.2f);
                    mat.SetFloat("_Emission", 0.4f);
                    mat.SetColor("_EmissionColor", new Color(0.2f, 0.5f, 0.3f));
                    var renderers = tree.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers) rend.material = mat;
                    
                    tree.AddComponent<DestructibleEnvironment>();
                }
            }
        }

        // Stub methods for missing helpers
        private static void BuildContainmentCore(GameObject parent) { }
        private static void BuildMilitaryCamp(GameObject parent, float radius, string type) { }
        private static void BuildPerimeterBreach(GameObject parent) { }
        private static void BuildSciFiProps(GameObject parent, int count) { }
        private static void BuildStructureRuins(GameObject parent, string type, Vector3 size, Color color) { }

#else
        // ============================================================
        // RUNTIME FALLBACK - Entorno básico para builds standalone
        // ============================================================
        
        private static void BuildOriginalEnvironment()
        {
            CreateBasicEnvironment("Environment", new Color(0.3f, 0.5f, 0.3f));
        }
        
        private static void BuildForestArenaEnvironment()
        {
            RenderSettings.ambientLight = new Color(0.2f, 0.35f, 0.2f, 1f);
            CreateBasicEnvironment("Environment_Forest", new Color(0.25f, 0.45f, 0.25f));
        }
        
        private static void BuildRockyCanyonEnvironment()
        {
            RenderSettings.ambientLight = new Color(0.25f, 0.22f, 0.18f, 1f);
            CreateBasicEnvironment("Environment_Rocky", new Color(0.5f, 0.45f, 0.4f));
        }
        
        private static void BuildDeadWoodsEnvironment()
        {
            RenderSettings.ambientLight = new Color(0.08f, 0.08f, 0.1f, 1f);
            CreateBasicEnvironment("Environment_DeadWoods", new Color(0.12f, 0.1f, 0.08f));
        }
        
        private static void BuildMushroomGroveEnvironment()
        {
            RenderSettings.ambientLight = new Color(0.25f, 0.2f, 0.35f, 1f);
            CreateBasicEnvironment("Environment_Mushroom", new Color(0.4f, 0.3f, 0.5f));
        }
        
        private static void BuildWaterArenaEnvironment()
        {
            RenderSettings.ambientLight = new Color(0.2f, 0.3f, 0.4f, 1f);
            CreateBasicEnvironment("Environment_Water", new Color(0.65f, 0.6f, 0.45f));
        }
        
        private static void BuildKoreanTempleEnvironment()
        {
            RenderSettings.ambientLight = new Color(0.3f, 0.28f, 0.25f, 1f);
            CreateBasicEnvironment("Environment_Temple", new Color(0.5f, 0.48f, 0.45f));
        }
        
        private static void BuildVolcanicCoastEnvironment()
        {
            RenderSettings.ambientLight = new Color(0.03f, 0.02f, 0.02f, 1f);
            CreateBasicEnvironment("Environment_Volcanic", new Color(0.2f, 0.1f, 0.08f));
        }
        
        private static void CreateBasicEnvironment(string name, Color groundColor)
        {
            var envGroup = new GameObject(name);
            
            // Suelo básico
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(envGroup.transform);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);
            
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = groundColor;
            ground.GetComponent<Renderer>().material = mat;
            
            // Tag para identificar suelo
            ground.tag = "Ground";
            
            Debug.Log($"[ArenaEnvironmentBuilder] Basic runtime environment created: {name}");
        }
#endif

        // =====================================================
        // RUNTIME VERSIONS - Usan Resources.Load (funcionan en builds)
        // =====================================================
        
        /// <summary>
        /// Versión runtime de ZCB-FOREST que funciona en builds usando Resources.Load
        /// </summary>
        public static void BuildForestEnvironmentRuntime()
        {
            var envGroup = new GameObject("Environment_ZCB_FOREST_Runtime");
            
            // Green dimensional fog
            RenderSettings.ambientLight = new Color(0.12f, 0.28f, 0.12f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.08f, 0.22f, 0.1f, 1f);
            RenderSettings.fogDensity = 0.022f;
            
            // Ground plane procedural (runtime safe)
            CreateProceduralGround(envGroup, "ForestGround", new Color(0.25f, 0.45f, 0.25f), 120f);
            
            // Árboles desde Resources/Free_Forest
            string[] treePrefabs = {
                "Prefabs/ForestTrees/Tree/Fir/forestpack_tree_fir_tall",
                "Prefabs/ForestTrees/Tree/Leaf/Normal/forestpack_tree_1_leaf_1",
                "Prefabs/ForestTrees/Tree/Treestump/forestpack_tree_stump_1"
            };
            
            int treesPlaced = 0;
            for (int i = 0; i < 80; i++)
            {
                string path = treePrefabs[Random.Range(0, treePrefabs.Length)];
                GameObject prefab = Resources.Load<GameObject>(path);
                if (prefab != null)
                {
                    Vector3 pos = GetRandomPositionWithoutCenter(12f, 55f);
                    var tree = Object.Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), envGroup.transform);
                    float s = Random.Range(0.8f, 1.6f);
                    tree.transform.localScale = new Vector3(s, s * Random.Range(0.9f, 1.3f), s);
                    tree.AddComponent<DestructibleEnvironment>();
                    treesPlaced++;
                }
            }
            
            // Foliage/Grass
            string[] foliagePrefabs = {
                "Prefabs/ForestTrees/Foliage/Grass_Brown",
                "Prefabs/ForestTrees/Foliage/Grass_Green"
            };
            
            for (int i = 0; i < 50; i++)
            {
                string path = foliagePrefabs[Random.Range(0, foliagePrefabs.Length)];
                GameObject prefab = Resources.Load<GameObject>(path);
                if (prefab != null)
                {
                    Vector3 pos = GetRandomPositionWithoutCenter(8f, 52f);
                    var bush = Object.Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), envGroup.transform);
                    float s = Random.Range(0.6f, 1.4f);
                    bush.transform.localScale = Vector3.one * s;
                    bush.AddComponent<DestructibleEnvironment>();
                }
            }
            
            // Stones/Rocks
            string[] stonePrefabs = {
                "Prefabs/ForestTrees/Stone/Stone_Big_Ambient_Occlusion",
                "Prefabs/ForestTrees/Stone/Stone_Small_Ambient_Occlusion"
            };
            
            for (int i = 0; i < 25; i++)
            {
                string path = stonePrefabs[Random.Range(0, stonePrefabs.Length)];
                GameObject prefab = Resources.Load<GameObject>(path);
                if (prefab != null)
                {
                    Vector3 pos = GetRandomPositionWithoutCenter(10f, 50f);
                    var stone = Object.Instantiate(prefab, pos, Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0, 360), 0), envGroup.transform);
                    float s = Random.Range(0.5f, 1.2f);
                    stone.transform.localScale = Vector3.one * s;
                    stone.AddComponent<DestructibleEnvironment>();
                }
            }
            
            Debug.Log($"[ArenaEnvironmentBuilder] ZCB-FOREST Runtime built with {treesPlaced} trees from Free_Forest assets.");
        }
        
        /// <summary>
        /// Crea suelo procedural que funciona en runtime - versión corregida sin Z-fighting
        /// </summary>
        private static void CreateProceduralGround(GameObject parent, string name, Color color, float size)
        {
            // Destruir suelo existente si hay uno con el mismo nombre para evitar duplicación
            var existing = parent.transform.Find(name);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }
            
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = name;
            ground.transform.SetParent(parent.transform);
            
            // Subir ligeramente para evitar Z-fighting con cualquier superficie base (0.01 unidades)
            ground.transform.position = new Vector3(0, 0.01f, 0);
            
            // Escala: Plane default es 10x10, escalamos para cubrir el área deseada
            ground.transform.localScale = new Vector3(size / 10f, 1f, size / 10f);
            
            // Configurar material con propiedades para evitar flickering
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            mat.color = color;
            mat.SetFloat("_Smoothness", 0.05f);  // Menos reflectivo = más estable
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_SpecularHighlights", 0f); // Reducir highlights que causan flickering
            
            // Para URP: desactivar receive shadows puede ayudar con flickering en planos
            var renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = mat;
                renderer.receiveShadows = false; // Evitar artefactos de sombras en planos grandes
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            
            // Configurar collider para ser sólido pero sin intersecciones
            var collider = ground.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = false;
            }
            
            ground.tag = "Ground";
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0)
            {
                ground.layer = groundLayer;
            }
            else
            {
                Debug.LogWarning("[ArenaEnvironmentBuilder] 'Ground' layer not found in project settings. Using Default layer.");
            }
            
            Debug.Log($"[ArenaEnvironmentBuilder] Ground created: {name} at y={ground.transform.position.y}");
        }
    }
}
