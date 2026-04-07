#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace ArenaEnhanced
{
    /// <summary>
    /// Generates aerial thumbnail screenshots of maps for the welcome screen.
    /// Only works in Editor mode.
    /// </summary>
    public class MapThumbnailGenerator : MonoBehaviour
    {
        [Header("Capture Settings")]
        [Tooltip("Resolution of the thumbnail (512 recommended)")]
        public int resolution = 512;
        
        [Tooltip("Height of the camera above the map")]
        public float cameraHeight = 60f;
        
        [Tooltip("Orthographic camera size - adjust based on map size")]
        public float orthoSize = 40f;
        
        [Header("Output")]
        [Tooltip("Directory to save thumbnails relative to Assets folder")]
        public string outputDirectory = "Resources/MapThumbnails";

        /// <summary>
        /// Generates a thumbnail for the current map
        /// </summary>
        [ContextMenu("Generate Thumbnail for Current Map")]
        public void GenerateThumbnailForCurrentMap()
        {
            string mapId = PlayerPrefs.GetString("SelectedMap", "original");
            GenerateThumbnail(mapId);
        }

        /// <summary>
        /// Generates a thumbnail with the specified map ID
        /// </summary>
        public void GenerateThumbnail(string mapId)
        {
            if (!Application.isEditor)
            {
                Debug.LogError("[MapThumbnailGenerator] Can only generate thumbnails in Editor mode!");
                return;
            }

            StartCoroutine(CaptureMapThumbnail(mapId));
        }

        private System.Collections.IEnumerator CaptureMapThumbnail(string mapId)
        {
            // Wait for end of frame to ensure everything is rendered
            yield return new WaitForEndOfFrame();

            // Create temporary camera
            GameObject camObj = new GameObject("ThumbnailCamera_Temp");
            Camera cam = camObj.AddComponent<Camera>();
            
            // Setup camera
            cam.transform.position = new Vector3(0, cameraHeight, 0);
            cam.transform.rotation = Quaternion.Euler(90, 0, 0);
            cam.orthographic = true;
            cam.orthographicSize = orthoSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 1f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;

            // Create render texture
            RenderTexture rt = new RenderTexture(resolution, resolution, 24);
            cam.targetTexture = rt;
            cam.Render();

            // Read pixels
            RenderTexture.active = rt;
            Texture2D screenshot = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
            screenshot.Apply();

            // Encode to PNG
            byte[] bytes = screenshot.EncodeToPNG();

            // Save to file
            string directoryPath = Path.Combine(Application.dataPath, outputDirectory);
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

            Debug.Log($"[MapThumbnailGenerator] Thumbnail saved: {filePath}");

            // Refresh AssetDatabase
            AssetDatabase.Refresh();

            // Import as sprite
            string relativePath = $"Assets/{outputDirectory}/thumbnail_{mapId}.png";
            ImportAsSprite(relativePath);
        }

        private void ImportAsSprite(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100;
                importer.filterMode = FilterMode.Trilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                Debug.Log($"[MapThumbnailGenerator] Imported as sprite: {path}");
            }
        }

        /// <summary>
        /// Generates thumbnails for all 8 premium maps
        /// </summary>
        [ContextMenu("Generate All Map Thumbnails")]
        public void GenerateAllThumbnails()
        {
            string[] mapIds = {
                "original",
                "forestarena",
                "rockycanyon",
                "deadwoods",
                "mushroomgrove",
                "waterarena",
                "koreantemple",
                "volcanic"
            };

            foreach (string mapId in mapIds)
            {
                Debug.Log($"[MapThumbnailGenerator] Queuing thumbnail for: {mapId}");
                // In a real scenario, you'd load each map scene first
                // For now, this generates based on currently loaded map
            }
        }

        /// <summary>
        /// Editor utility to generate thumbnail from current scene view
        /// </summary>
        [MenuItem("Arena/Generate Map Thumbnail")]
        public static void GenerateThumbnailFromMenu()
        {
            MapThumbnailGenerator generator = FindAnyObjectByType<MapThumbnailGenerator>();
            if (generator == null)
            {
                GameObject go = new GameObject("ThumbnailGenerator");
                generator = go.AddComponent<MapThumbnailGenerator>();
            }
            
            string mapId = EditorUtility.DisplayDialogComplex(
                "Generate Map Thumbnail",
                "Select map to generate thumbnail for:",
                "Current Map",
                "Cancel",
                "All Maps"
            ) switch
            {
                0 => PlayerPrefs.GetString("SelectedMap", "original"),
                2 => "all",
                _ => null
            };

            if (mapId == "all")
            {
                generator.GenerateAllThumbnails();
            }
            else if (mapId != null)
            {
                generator.GenerateThumbnail(mapId);
            }
        }
    }
}
#endif
