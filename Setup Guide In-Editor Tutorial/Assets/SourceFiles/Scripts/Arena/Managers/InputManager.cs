using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace ArenaEnhanced
{
    /// <summary>
    /// Sistema de input centralizado que maneja todas las entradas del jugador.
    /// Reemplaza el código duplicado #if ENABLE_INPUT_SYSTEM en múltiples scripts.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        // Eventos de input
        public static event Action OnJumpPressed;
        public static event Action OnDashPressed;
        public static event Action OnDropWeaponPressed;
        public static event Action OnPickUpWeaponPressed;
        public static event Action<int> OnAbilityPressed; // 1-9 para habilidades
        public static event Action<int> OnWeaponAttackPressed; // 4 para ataque arma
        public static event Action<int> OnWeaponAttackReleased; // 4 soltar
        public static event Action OnKatanaEquipToggle; // K key
        public static event Action OnKatanaAttackPressed; // 3 key down
        public static event Action OnKatanaAttackReleased; // 3 key up

        // Estado para habilidades que usan hold (katana)
        public static float KatanaAttackHoldTime { get; private set; } = -1f;

        // Bloqueo de input (para menús, cinemáticas, etc.)
        public static bool IsInputBlocked { get; set; } = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Debug.Log("[InputManager] Initialized and ready");
        }

        private void Update()
        {
            if (IsInputBlocked) return;

            HandleMovementInput();
            HandleActionInput();
            HandleAbilityInput();
            HandleKatanaInput();
        }

        private void HandleMovementInput()
        {
            // El movimiento se lee directamente en PlayerController para mejor responsividad
            // Esto es solo para el nuevo input system si es necesario
        }

        private void HandleActionInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return;

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
                OnJumpPressed?.Invoke();

            if (Keyboard.current.fKey.wasPressedThisFrame)
                OnDashPressed?.Invoke();

            if (Keyboard.current.qKey.wasPressedThisFrame)
                OnDropWeaponPressed?.Invoke();

            if (Keyboard.current.eKey.wasPressedThisFrame)
                OnPickUpWeaponPressed?.Invoke();

            if (Keyboard.current.kKey.wasPressedThisFrame)
            {
                OnKatanaEquipToggle?.Invoke();
#if DEBUG
                Debug.Log("[InputManager] KatanaEquipToggle event fired");
#endif
            }
#else
            if (Input.GetKeyDown(KeyCode.Space))
                OnJumpPressed?.Invoke();

            if (Input.GetKeyDown(KeyCode.F))
                OnDashPressed?.Invoke();

            if (Input.GetKeyDown(KeyCode.Q))
                OnDropWeaponPressed?.Invoke();

            if (Input.GetKeyDown(KeyCode.E))
                OnPickUpWeaponPressed?.Invoke();

            if (Input.GetKeyDown(KeyCode.K))
            {
                OnKatanaEquipToggle?.Invoke();
#if DEBUG
                Debug.Log("[InputManager] KatanaEquipToggle event fired (legacy)");
#endif
            }
#endif
        }

        private void HandleAbilityInput()
        {
            int abilityIndex = -1;

            // Sistema legacy (SIEMPRE ACTIVO - funciona con numpad y teclas de arriba)
            if (Input.GetKeyDown(KeyCode.Keypad1) || Input.GetKeyDown(KeyCode.Alpha1)) 
            {
                abilityIndex = 1;
            }
            else if (Input.GetKeyDown(KeyCode.Keypad2) || Input.GetKeyDown(KeyCode.Alpha2)) 
            {
                abilityIndex = 2;
            }
            else if (Input.GetKeyDown(KeyCode.Keypad3) || Input.GetKeyDown(KeyCode.Alpha3)) 
            {
                abilityIndex = 3;
            }
            else if (Input.GetKeyDown(KeyCode.Keypad4) || Input.GetKeyDown(KeyCode.Alpha4)) 
            {
                abilityIndex = 4;
            }
            else if (Input.GetKeyDown(KeyCode.Keypad5) || Input.GetKeyDown(KeyCode.Alpha5)) 
            {
                abilityIndex = 5;
            }
            else if (Input.GetKeyDown(KeyCode.Keypad6) || Input.GetKeyDown(KeyCode.Alpha6)) 
            {
                abilityIndex = 6;
            }
            else if (Input.GetKeyDown(KeyCode.Keypad7) || Input.GetKeyDown(KeyCode.Alpha7)) 
            {
                abilityIndex = 7;
            }
            else if (Input.GetKeyDown(KeyCode.Keypad8) || Input.GetKeyDown(KeyCode.Alpha8)) 
            {
                abilityIndex = 8;
            }
            else if (Input.GetKeyDown(KeyCode.Keypad9) || Input.GetKeyDown(KeyCode.Alpha9)) 
            {
                abilityIndex = 9;
            }

            // Arma mantenida (hold) - numpad 4 o tecla 4
            if (Input.GetKey(KeyCode.Keypad4) || Input.GetKey(KeyCode.Alpha4))
            {
                OnWeaponAttackPressed?.Invoke(4);
            }
            if (Input.GetKeyUp(KeyCode.Keypad4) || Input.GetKeyUp(KeyCode.Alpha4))
            {
                OnWeaponAttackReleased?.Invoke(4);
            }
            
            // Disparar evento de habilidad
            if (abilityIndex >= 0)
            {
                OnAbilityPressed?.Invoke(abilityIndex);
            }
        }

        private void HandleKatanaInput()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)
            {
                KatanaAttackHoldTime = Time.time;
                OnKatanaAttackPressed?.Invoke();
#if DEBUG
                Debug.Log("[InputManager] KatanaAttackPressed event fired");
#endif
            }

            if (kb.digit3Key.wasReleasedThisFrame || kb.numpad3Key.wasReleasedThisFrame)
            {
                OnKatanaAttackReleased?.Invoke();
                KatanaAttackHoldTime = -1f;
#if DEBUG
                Debug.Log("[InputManager] KatanaAttackReleased event fired");
#endif
            }
#else
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                KatanaAttackHoldTime = Time.time;
                OnKatanaAttackPressed?.Invoke();
            }

            if (Input.GetKeyUp(KeyCode.Alpha3) || Input.GetKeyUp(KeyCode.Keypad3))
            {
                OnKatanaAttackReleased?.Invoke();
                KatanaAttackHoldTime = -1f;
            }
#endif
        }

        /// <summary>
        /// Obtiene el tiempo que se ha mantenido presionada la tecla de katana
        /// </summary>
        public static float GetKatanaHoldDuration()
        {
            if (KatanaAttackHoldTime < 0f) return 0f;
            return Time.time - KatanaAttackHoldTime;
        }

        /// <summary>
        /// Verifica si la katana está siendo atacada (tecla 5 presionada)
        /// </summary>
        public static bool IsKatanaAttackHeld()
        {
            if (KatanaAttackHoldTime < 0f) return false;
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            return kb.digit3Key.isPressed || kb.numpad3Key.isPressed;
#else
            return Input.GetKey(KeyCode.Alpha3) || Input.GetKey(KeyCode.Keypad3);
#endif
        }
    }
}
