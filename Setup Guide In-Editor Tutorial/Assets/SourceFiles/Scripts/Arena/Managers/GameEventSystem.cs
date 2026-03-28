using UnityEngine;
using System;
using System.Collections.Generic;

namespace ArenaEnhanced.Managers
{
    /// <summary>
    /// Sistema de eventos global para comunicación entre scripts
    /// </summary>
    public class GameEventSystem : MonoBehaviour
    {
        private static GameEventSystem _instance;
        public static GameEventSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogWarning("[GameEventSystem] Instance is null. Make sure GameManager initializes it.");
                }
                return _instance;
            }
        }
        
        private Dictionary<string, Action<object>> _eventListeners = new Dictionary<string, Action<object>>();
        private Dictionary<string, List<Action<object>>> _multiListeners = new Dictionary<string, List<Action<object>>>();
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }
        
        /// <summary>
        /// Suscribe un listener a un evento
        /// </summary>
        public void Subscribe(string eventName, Action<object> listener)
        {
            if (!_eventListeners.ContainsKey(eventName))
            {
                _eventListeners[eventName] = listener;
            }
            else
            {
                _eventListeners[eventName] += listener;
            }
        }
        
        /// <summary>
        /// Suscribe un listener que soporta múltiples suscriptores
        /// </summary>
        public void SubscribeMulti(string eventName, Action<object> listener)
        {
            if (!_multiListeners.ContainsKey(eventName))
            {
                _multiListeners[eventName] = new List<Action<object>>();
            }
            
            if (!_multiListeners[eventName].Contains(listener))
            {
                _multiListeners[eventName].Add(listener);
            }
        }
        
        /// <summary>
        /// Desuscribe un listener de un evento
        /// </summary>
        public void Unsubscribe(string eventName, Action<object> listener)
        {
            if (_eventListeners.ContainsKey(eventName))
            {
                _eventListeners[eventName] -= listener;
                
                if (_eventListeners[eventName] == null)
                {
                    _eventListeners.Remove(eventName);
                }
            }
            
            if (_multiListeners.ContainsKey(eventName))
            {
                _multiListeners[eventName].Remove(listener);
                
                if (_multiListeners[eventName].Count == 0)
                {
                    _multiListeners.Remove(eventName);
                }
            }
        }
        
        /// <summary>
        /// Publica un evento a todos los suscriptores
        /// </summary>
        public void Publish(string eventName, object data = null)
        {
            if (_eventListeners.ContainsKey(eventName))
            {
                try
                {
                    _eventListeners[eventName]?.Invoke(data);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GameEventSystem] Error in event '{eventName}': {e.Message}");
                }
            }
            
            if (_multiListeners.ContainsKey(eventName))
            {
                foreach (Action<object> listener in _multiListeners[eventName])
                {
                    try
                    {
                        listener?.Invoke(data);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[GameEventSystem] Error in multi-listener for '{eventName}': {e.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Limpia todos los listeners de un evento
        /// </summary>
        public void ClearEvent(string eventName)
        {
            _eventListeners.Remove(eventName);
            _multiListeners.Remove(eventName);
        }
        
        /// <summary>
        /// Limpia todos los eventos
        /// </summary>
        public void ClearAllEvents()
        {
            _eventListeners.Clear();
            _multiListeners.Clear();
        }
        
        /// <summary>
        /// Verifica si hay suscriptores para un evento
        /// </summary>
        public bool HasListeners(string eventName)
        {
            return _eventListeners.ContainsKey(eventName) || _multiListeners.ContainsKey(eventName);
        }
        
        private void OnDestroy()
        {
            ClearAllEvents();
            
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
    
    /// <summary>
    /// Nombres de eventos del juego
    /// </summary>
    public static class GameEvents
    {
        // Combat Events
        public const string ENEMY_DIED = "EnemyDied";
        public const string BOSS_DIED = "BossDied";
        public const string PLAYER_DIED = "PlayerDied";
        public const string ALLY_DIED = "AllyDied";
        public const string DAMAGE_DEALT = "DamageDealt";
        public const string DAMAGE_RECEIVED = "DamageReceived";
        
        // Wave Events
        public const string WAVE_STARTED = "WaveStarted";
        public const string WAVE_COMPLETED = "WaveCompleted";
        public const string BOSS_SPAWNED = "BossSpawned";
        
        // Environment Events
        public const string BARREL_EXPLODED = "BarrelExploded";
        public const string TRAP_ACTIVATED = "TrapActivated";
        public const string FIRE_ZONE_STARTED = "FireZoneStarted";
        public const string FIRE_ZONE_ENDED = "FireZoneEnded";
        
        // UI Events
        public const string POINTS_CHANGED = "PointsChanged";
        public const string LEVEL_UP = "LevelUp";
        public const string ABILITY_USED = "AbilityUsed";
        public const string WEAPON_PICKED_UP = "WeaponPickedUp";
        public const string WEAPON_DROPPED = "WeaponDropped";
        
        // System Events
        public const string GAME_STARTED = "GameStarted";
        public const string GAME_PAUSED = "GamePaused";
        public const string GAME_RESUMED = "GameResumed";
        public const string GAME_OVER = "GameOver";
    }
}