using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Datos de configuración para cada mapa - almacenados como JSON en Resources/Configs/Maps
    /// </summary>
    [System.Serializable]
    public class MapConfigData
    {
        public string mapId;
        public string displayName;
        public string description;
        public float arenaRadius;
        public Vector3 playerSpawnPosition;
        public float botSpawnRadius;
        public Color ambientLightColor;
        public Color fogColor;
        public float fogDensity;
        public EnvironmentType environmentType;
    }

    public enum EnvironmentType
    {
        Original,
        ForestArena,
        RockyCanyon,
        DeadWoods,
        MushroomGrove,
        WaterArena,
        KoreanTemple,
        VolcanicCoast
    }
}
