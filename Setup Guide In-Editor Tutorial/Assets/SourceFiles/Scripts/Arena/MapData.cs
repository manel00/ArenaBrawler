using UnityEngine;

namespace ArenaEnhanced
{
    [CreateAssetMenu(fileName = "NewMap", menuName = "Arena/Map Data")]
    public class MapData : ScriptableObject
    {
        [Header("Map Info")]
        public string mapId = "original";
        public string displayName = "Original Arena";
        [TextArea(2, 4)]
        public string description = "Classic flat arena with trees and rocks";
        public Sprite previewImage;

        [Header("Environment Settings")]
        public EnvironmentType environmentType = EnvironmentType.Original;
        public float arenaRadius = 38f;
        public Vector3 playerSpawnPosition = new Vector3(0f, 1.2f, -6f);
        public float botSpawnRadius = 8f;

        [Header("Visual Settings")]
        public Color ambientLightColor = new Color(0.2f, 0.2f, 0.3f, 1f);
        public Color fogColor = new Color(0.1f, 0.12f, 0.15f, 1f);
        public float fogDensity = 0.01f;

        [Header("3D Game Kit Assets (for custom environments)")]
        public string terrainMaterialPath = "Assets/Materials/Material_Grass.mat";
        public bool use3DGameKitAssets = false;
        public string[] treePrefabPaths;
        public string[] rockPrefabPaths;
    }
}
