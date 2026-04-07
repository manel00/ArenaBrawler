using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace ArenaEnhanced.Editor
{
    /// <summary>
    /// Generador de librería de materiales para los mapas ZCB
    /// Crea materiales según las especificaciones de diseño de niveles
    /// </summary>
    public class ZCBMaterialLibraryGenerator : EditorWindow
    {
        private string outputPath = "Assets/SourceFiles/Materials/ZCB_Library";
        private bool createZCBAlpha = true;
        private bool createZCBCanyon = true;
        private bool createZCBDeadwoods = true;
        private bool createZCBFungal = true;
        private bool createZCBVolcanic = true;
        private bool createZCBHydro = true;
        private bool createZCBSanctum = true;
        private bool createZCBForest = true;

        [MenuItem("Window/Arena Enhanced/ZCB Material Library Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<ZCBMaterialLibraryGenerator>("ZCB Material Library");
            window.minSize = new Vector2(400, 500);
        }

        private void OnGUI()
        {
            GUILayout.Label("ZCB Material Library Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "Genera materiales URP compatibles para todos los mapas ZCB " +
                "según las especificaciones del GDD.", MessageType.Info);

            EditorGUILayout.Space();

            GUILayout.Label("Output Path:", EditorStyles.boldLabel);
            outputPath = EditorGUILayout.TextField(outputPath);

            EditorGUILayout.Space();
            GUILayout.Label("Maps to Generate Materials For:", EditorStyles.boldLabel);

            createZCBAlpha = GUILayout.Toggle(createZCBAlpha, "ZCB-ALPHA (Original Arena)");
            createZCBCanyon = GUILayout.Toggle(createZCBCanyon, "ZCB-CANYON (Rocky Canyon)");
            createZCBDeadwoods = GUILayout.Toggle(createZCBDeadwoods, "ZCB-DEADWOODS");
            createZCBFungal = GUILayout.Toggle(createZCBFungal, "ZCB-FUNGAL (Mushroom Grove)");
            createZCBHydro = GUILayout.Toggle(createZCBHydro, "ZCB-HYDRO (Water Arena)");
            createZCBSanctum = GUILayout.Toggle(createZCBSanctum, "ZCB-SANCTUM (Korean Temple)");
            createZCBVolcanic = GUILayout.Toggle(createZCBVolcanic, "ZCB-VOLCANIC");
            createZCBForest = GUILayout.Toggle(createZCBForest, "ZCB-FOREST (Forest Valley)");

            EditorGUILayout.Space();

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("GENERATE MATERIAL LIBRARY", GUILayout.Height(50)))
            {
                GenerateLibrary();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space();

            if (GUILayout.Button("Create Missing Folders"))
            {
                CreateFolderStructure();
            }
        }

        private void CreateFolderStructure()
        {
            string[] folders = new string[]
            {
                "Terrain",
                "Structures",
                "Vegetation",
                "Props",
                "VFX",
                "XSpace"
            };

            foreach (var folder in folders)
            {
                string path = Path.Combine(outputPath, folder);
                if (!AssetDatabase.IsValidFolder(path))
                {
                    string parent = Path.GetDirectoryName(path).Replace("\\", "/");
                    string folderName = Path.GetFileName(path);
                    AssetDatabase.CreateFolder(parent, folderName);
                }
            }

            EditorUtility.DisplayDialog("Folders Created", 
                "Folder structure created successfully!", "OK");
        }

        private void GenerateLibrary()
        {
            if (!AssetDatabase.IsValidFolder(outputPath))
            {
                string parent = "Assets";
                string[] parts = outputPath.Replace("Assets/", "").Split('/');
                foreach (var part in parts)
                {
                    if (!AssetDatabase.IsValidFolder($"{parent}/{part}"))
                    {
                        AssetDatabase.CreateFolder(parent, part);
                    }
                    parent = $"{parent}/{part}";
                }
            }

            int createdCount = 0;

            // Generate materials for each map
            if (createZCBAlpha) createdCount += GenerateZCBAlphaMaterials();
            if (createZCBCanyon) createdCount += GenerateZCBCanyonMaterials();
            if (createZCBDeadwoods) createdCount += GenerateZCBDeadwoodsMaterials();
            if (createZCBFungal) createdCount += GenerateZCBFungalMaterials();
            if (createZCBHydro) createdCount += GenerateZCBHydroMaterials();
            if (createZCBSanctum) createdCount += GenerateZCBSanctumMaterials();
            if (createZCBVolcanic) createdCount += GenerateZCBVolcanicMaterials();
            if (createZCBForest) createdCount += GenerateZCBForestMaterials();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Material Library Generated", 
                $"Created {createdCount} materials in {outputPath}", "OK");
        }

        private int GenerateZCBAlphaMaterials()
        {
            int count = 0;
            string basePath = $"{outputPath}/Terrain";

            // Ground materials - Concentric ring design
            // Center - Safe zone (terrestrial)
            var grassCenter = CreateMaterial($"{basePath}/ZCBAlpha_Grass_Center.mat", 
                "Universal Render Pipeline/Lit",
                new Color(0.35f, 0.6f, 0.35f), 0.1f, 0.5f);
            grassCenter.SetFloat("_Smoothness", 0.1f);
            count++;

            // Mid ring - Transition (mutated grass)
            var grassMid = CreateMaterial($"{basePath}/ZCBAlpha_Grass_Transition.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.4f, 0.55f, 0.4f), 0.15f, 0.4f);
            count++;

            // Perimeter - Danger zone (bioluminescent)
            var grassPerimeter = CreateMaterial($"{basePath}/ZCBAlpha_Grass_Mutated.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.3f, 0.45f, 0.35f), 0.1f, 0.3f);
            // Add subtle emission for bioluminescence
            grassPerimeter.EnableKeyword("_EMISSION");
            grassPerimeter.SetColor("_EmissionColor", new Color(0.2f, 0.4f, 0.3f, 1f) * 0.3f);
            count++;

            // Structure materials
            basePath = $"{outputPath}/Structures";
            var steelRusted = CreateMaterial($"{basePath}/ZCBAlpha_Steel_Rusted.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.35f, 0.32f, 0.28f), 0.9f, 0.6f);
            steelRusted.SetFloat("_Metallic", 0.8f);
            steelRusted.SetFloat("_Smoothness", 0.3f);
            count++;

            var concreteDamaged = CreateMaterial($"{basePath}/ZCBAlpha_Concrete_Damaged.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.45f, 0.45f, 0.48f), 0.1f, 0.2f);
            count++;

            // X-Space materials
            basePath = $"{outputPath}/XSpace";
            var xspaceCrystal = CreateMaterial($"{basePath}/ZCBAlpha_XSpace_Crystal.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.6f, 0.2f, 0.8f), 0.0f, 0.9f);
            xspaceCrystal.EnableKeyword("_EMISSION");
            xspaceCrystal.SetColor("_EmissionColor", new Color(0.8f, 0.3f, 1f, 1f) * 2f);
            xspaceCrystal.SetFloat("_Smoothness", 0.95f);
            count++;

            var brechaEmissive = CreateMaterial($"{basePath}/ZCBAlpha_Brecha_Emissive.mat",
                "Universal Render Pipeline/Particles/Unlit",
                new Color(0.7f, 0.3f, 0.9f, 0.6f), 0f, 1f);
            brechaEmissive.EnableKeyword("_EMISSION");
            brechaEmissive.SetColor("_EmissionColor", new Color(1f, 0.5f, 0.9f, 1f) * 3f);
            brechaEmissive.SetFloat("_Surface", 1); // Transparent
            brechaEmissive.SetFloat("_Blend", 1); // Additive
            brechaEmissive.renderQueue = 3000;
            count++;

            // Props
            basePath = $"{outputPath}/Props";
            var militaryGreen = CreateMaterial($"{basePath}/ZCBAlpha_Military_Green.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.25f, 0.35f, 0.15f), 0.0f, 0.2f);
            count++;

            var militaryCase = CreateMaterial($"{basePath}/ZCBAlpha_Military_Case.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.2f, 0.2f, 0.18f), 0.1f, 0.3f);
            count++;

            return count;
        }

        private int GenerateZCBCanyonMaterials()
        {
            int count = 0;
            string basePath = $"{outputPath}/Terrain";

            // Rocky canyon - warm tones
            var rockRed = CreateMaterial($"{basePath}/ZCBCanyon_Rock_Red.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.65f, 0.35f, 0.25f), 0.0f, 0.1f);
            count++;

            var rockOrange = CreateMaterial($"{basePath}/ZCBCanyon_Rock_Orange.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.75f, 0.45f, 0.25f), 0.0f, 0.1f);
            count++;

            var sandCanyon = CreateMaterial($"{basePath}/ZCBCanyon_Sand.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.7f, 0.5f, 0.35f), 0.0f, 0.05f);
            count++;

            // Platforms
            basePath = $"{outputPath}/Structures";
            var stonePlatform = CreateMaterial($"{basePath}/ZCBCanyon_Stone_Platform.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.55f, 0.5f, 0.45f), 0.0f, 0.15f);
            count++;

            return count;
        }

        private int GenerateZCBDeadwoodsMaterials()
        {
            int count = 0;
            string basePath = $"{outputPath}/Terrain";

            // Dark, muted ground
            var groundDead = CreateMaterial($"{basePath}/ZCBDeadwoods_Ground_Dead.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.12f, 0.1f, 0.08f), 0.0f, 0.1f);
            count++;

            basePath = $"{outputPath}/Vegetation";
            var barkDead = CreateMaterial($"{basePath}/ZCBDeadwoods_Bark_Dead.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.15f, 0.13f, 0.12f), 0.0f, 0.05f);
            count++;

            var barkTwisted = CreateMaterial($"{basePath}/ZCBDeadwoods_Bark_Twisted.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.12f, 0.1f, 0.1f), 0.0f, 0.1f);
            count++;

            return count;
        }

        private int GenerateZCBFungalMaterials()
        {
            int count = 0;
            string basePath = $"{outputPath}/Terrain";

            var groundFungal = CreateMaterial($"{basePath}/ZCBFungal_Ground.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.25f, 0.2f, 0.35f), 0.0f, 0.15f);
            groundFungal.EnableKeyword("_EMISSION");
            groundFungal.SetColor("_EmissionColor", new Color(0.3f, 0.2f, 0.4f, 1f) * 0.2f);
            count++;

            basePath = $"{outputPath}/Vegetation";
            var mushroomCommon = CreateMaterial($"{basePath}/ZCBFungal_Mushroom_Common.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.4f, 0.3f, 0.5f), 0.0f, 0.3f);
            count++;

            var mushroomGlow = CreateMaterial($"{basePath}/ZCBFungal_Mushroom_Glow.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.5f, 0.8f, 0.4f), 0.0f, 0.5f);
            mushroomGlow.EnableKeyword("_EMISSION");
            mushroomGlow.SetColor("_EmissionColor", new Color(0.4f, 0.9f, 0.3f, 1f) * 0.5f);
            count++;

            return count;
        }

        private int GenerateZCBHydroMaterials()
        {
            int count = 0;
            string basePath = $"{outputPath}/Terrain";

            var waterDeep = CreateMaterial($"{basePath}/ZCBHydro_Water_Deep.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.1f, 0.3f, 0.5f, 0.7f), 0.0f, 0.9f);
            waterDeep.SetFloat("_Surface", 1);
            waterDeep.SetFloat("_Smoothness", 0.95f);
            count++;

            var waterShallow = CreateMaterial($"{basePath}/ZCBHydro_Water_Shallow.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.2f, 0.5f, 0.7f, 0.6f), 0.0f, 0.8f);
            waterShallow.SetFloat("_Surface", 1);
            waterShallow.SetFloat("_Smoothness", 0.9f);
            count++;

            var sandWet = CreateMaterial($"{basePath}/ZCBHydro_Sand_Wet.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.45f, 0.45f, 0.4f), 0.0f, 0.2f);
            count++;

            return count;
        }

        private int GenerateZCBSanctumMaterials()
        {
            int count = 0;
            string basePath = $"{outputPath}/Structures";

            var stoneTemple = CreateMaterial($"{basePath}/ZCBSanctum_Stone_Temple.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.5f, 0.48f, 0.42f), 0.0f, 0.2f);
            count++;

            var stoneRoof = CreateMaterial($"{basePath}/ZCBSanctum_Stone_Roof.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.35f, 0.35f, 0.4f), 0.0f, 0.15f);
            count++;

            var woodDark = CreateMaterial($"{basePath}/ZCBSanctum_Wood_Dark.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.25f, 0.2f, 0.15f), 0.0f, 0.15f);
            count++;

            var goldTrim = CreateMaterial($"{basePath}/ZCBSanctum_Gold_Trim.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.9f, 0.75f, 0.35f), 0.8f, 0.7f);
            goldTrim.EnableKeyword("_EMISSION");
            goldTrim.SetColor("_EmissionColor", new Color(0.8f, 0.6f, 0.2f, 1f) * 0.3f);
            count++;

            return count;
        }

        private int GenerateZCBVolcanicMaterials()
        {
            int count = 0;
            string basePath = $"{outputPath}/Terrain";

            var lavaRock = CreateMaterial($"{basePath}/ZCBVolcanic_LavaRock.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.15f, 0.1f, 0.08f), 0.0f, 0.1f);
            lavaRock.EnableKeyword("_EMISSION");
            lavaRock.SetColor("_EmissionColor", new Color(0.8f, 0.2f, 0.05f, 1f) * 0.8f);
            count++;

            var ashGround = CreateMaterial($"{basePath}/ZCBVolcanic_Ash_Ground.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.08f, 0.08f, 0.08f), 0.0f, 0.05f);
            count++;

            basePath = $"{outputPath}/VFX";
            var lavaFlow = CreateMaterial($"{basePath}/ZCBVolcanic_Lava_Flow.mat",
                "Universal Render Pipeline/Particles/Unlit",
                new Color(1f, 0.3f, 0.05f, 0.9f), 0f, 1f);
            lavaFlow.EnableKeyword("_EMISSION");
            lavaFlow.SetColor("_EmissionColor", new Color(1f, 0.4f, 0.1f, 1f) * 4f);
            lavaFlow.SetFloat("_Surface", 1);
            lavaFlow.renderQueue = 3000;
            count++;

            return count;
        }

        private int GenerateZCBForestMaterials()
        {
            int count = 0;
            string basePath = $"{outputPath}/Terrain";

            var grassForest = CreateMaterial($"{basePath}/ZCBForest_Grass.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.3f, 0.55f, 0.25f), 0.0f, 0.1f);
            count++;

            var dirtForest = CreateMaterial($"{basePath}/ZCBForest_Dirt.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.35f, 0.25f, 0.15f), 0.0f, 0.1f);
            count++;

            basePath = $"{outputPath}/Vegetation";
            var leavesGreen = CreateMaterial($"{basePath}/ZCBForest_Leaves_Green.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.25f, 0.5f, 0.15f), 0.0f, 0.15f);
            count++;

            var barkForest = CreateMaterial($"{basePath}/ZCBForest_Bark.mat",
                "Universal Render Pipeline/Lit",
                new Color(0.35f, 0.25f, 0.15f), 0.0f, 0.1f);
            count++;

            return count;
        }

        private Material CreateMaterial(string path, string shaderName, Color color, float metallic, float smoothness)
        {
            // Check if material already exists
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                // Update existing
                Undo.RecordObject(existing, "Update Material");
                var existingShader = Shader.Find(shaderName);
                if (existingShader != null)
                    existing.shader = existingShader;
                existing.SetColor("_BaseColor", color);
                existing.SetFloat("_Metallic", metallic);
                existing.SetFloat("_Smoothness", smoothness);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            // Create new
            var newShader = Shader.Find(shaderName);
            if (newShader == null)
            {
                Debug.LogError($"Shader not found: {shaderName}");
                newShader = Shader.Find("Universal Render Pipeline/Lit");
            }

            var mat = new Material(newShader);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);

            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}
