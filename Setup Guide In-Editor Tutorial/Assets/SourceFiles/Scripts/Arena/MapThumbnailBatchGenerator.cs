#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace ArenaEnhanced
{
    /// <summary>
    /// Editor tool to batch generate map thumbnails for all premium maps
    /// </summary>
    public class MapThumbnailBatchGenerator : EditorWindow
    {
        private Vector2 scrollPosition;
        private bool[] generateFlags = new bool[8] { true, true, true, true, true, true, true, true };
        
        private string[] mapIds = {
            "original",
            "forestarena", 
            "rockycanyon",
            "deadwoods",
            "mushroomgrove",
            "waterarena",
            "koreantemple",
            "volcanic"
        };
        
        private string[] mapNames = {
            "ZCB-ALPHA (Original)",
            "ZCB-FOREST",
            "ZCB-CANYON", 
            "ZCB-DEADWOODS",
            "ZCB-FUNGAL",
            "ZCB-HYDRO",
            "ZCB-SANCTUM",
            "ZCB-VOLCANIC"
        };

        [MenuItem("Arena/Generate All Map Thumbnails")]
        public static void ShowWindow()
        {
            GetWindow<MapThumbnailBatchGenerator>("Map Thumbnail Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Map Thumbnail Batch Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            GUILayout.Label("Select maps to generate:", EditorStyles.label);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            
            for (int i = 0; i < mapIds.Length; i++)
            {
                generateFlags[i] = GUILayout.Toggle(generateFlags[i], mapNames[i]);
            }
            
            GUILayout.EndScrollView();
            GUILayout.Space(10);
            
            if (GUILayout.Button("Generate Selected Thumbnails", GUILayout.Height(30)))
            {
                GenerateSelectedThumbnails();
            }
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Generate All Thumbnails", GUILayout.Height(30)))
            {
                for (int i = 0; i < generateFlags.Length; i++) generateFlags[i] = true;
                GenerateSelectedThumbnails();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Clear Environment", GUILayout.Height(25)))
            {
                ClearEnvironment();
            }
        }

        private void GenerateSelectedThumbnails()
        {
            int generatedCount = 0;
            
            for (int i = 0; i < mapIds.Length; i++)
            {
                if (!generateFlags[i]) continue;
                
                EditorUtility.DisplayProgressBar(
                    "Generating Thumbnails", 
                    $"Generating {mapNames[i]}...", 
                    (float)i / mapIds.Length
                );
                
                try
                {
                    GenerateThumbnailForMap(mapIds[i]);
                    generatedCount++;
                    Debug.Log($"[MapThumbnailBatchGenerator] Generated thumbnail for {mapNames[i]}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[MapThumbnailBatchGenerator] Failed to generate thumbnail for {mapNames[i]}: {e.Message}");
                }
            }
            
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog(
                "Thumbnails Generated", 
                $"Successfully generated {generatedCount} map thumbnails!\n\nLocation: Assets/Resources/MapThumbnails/", 
                "OK"
            );
        }

        private void GenerateThumbnailForMap(string mapId)
        {
            // Clear existing environment
            ClearEnvironment();
            
            // Generate new environment
            ArenaEnvironmentBuilder.BuildEnvironment(mapId);
            
            // Wait for objects to instantiate
            EditorApplication.ExecuteMenuItem("GameObject/Align View to Selected");
            
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
            string directoryPath = Path.Combine(Application.dataPath, "Resources/MapThumbnails");
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            
            string filePath = Path.Combine(directoryPath, $"thumbnail_{mapId}.png");
            File.WriteAllBytes(filePath, bytes);
            
            // Cleanup
            RenderTexture.active = null;
            cam.targetTexture = null;
            DestroyImmediate(rt);
            DestroyImmediate(camObj);
            DestroyImmediate(screenshot);
            
            // Import and configure
            string relativePath = $"Assets/Resources/MapThumbnails/thumbnail_{mapId}.png";
            AssetDatabase.ImportAsset(relativePath);
            
            TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100;
                importer.filterMode = FilterMode.Trilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        private void ClearEnvironment()
        {
            // Find and destroy environment objects
            string[] envPrefixes = {
                "Environment",
                "Environment_ForestArena",
                "Environment_RockyCanyon", 
                "Environment_DeadWoods",
                "Environment_MushroomGrove",
                "Environment_WaterArena",
                "Environment_KoreanTemple",
                "Environment_Volcanic",
                "Environment_ZCB"
            };
            
            foreach (string prefix in envPrefixes)
            {
                GameObject[] envObjects = GameObject.FindGameObjectsWithTag("Untagged");
                foreach (GameObject obj in envObjects)
                {
                    if (obj.name.StartsWith(prefix) || obj.name.Contains("Environment"))
                    {
                        DestroyImmediate(obj);
                    }
                }
            }
            
            // Also look for specific environment groups
            GameObject env = GameObject.Find("Environment");
            if (env != null) DestroyImmediate(env);
        }
    }
}
#endif
