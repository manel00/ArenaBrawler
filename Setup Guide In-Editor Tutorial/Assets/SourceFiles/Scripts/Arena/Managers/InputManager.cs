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
        public static event Action OnKatanaAttackPressed; // 5 key down
        public static event Action OnKatanaAttackReleased; // 5 key up

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

            // Sistema nuevo (New Input System)
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) abilityIndex = 1;
                else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) abilityIndex = 2;
                else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) abilityIndex = 3;
                else if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) abilityIndex = 4;
                else if (kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame) abilityIndex = 6;
                else if (kb.digit7Key.wasPressedThisFrame || kb.numpad7Key.wasPressedThisFrame) abilityIndex = 7;
                else if (kb.digit8Key.wasPressedThisFrame || kb.numpad8Key.wasPressedThisFrame) abilityIndex = 8;
                else if (kb.digit9Key.wasPressedThisFrame || kb.numpad9Key.wasPressedThisFrame) abilityIndex = 9;

                // Arma mantenida (hold)
                if (kb.digit4Key.isPressed || kb.numpad4Key.isPressed)
                    OnWeaponAttackPressed?.Invoke(4);
                if (kb.digit4Key.wasReleasedThisFrame || kb.numpad4Key.wasReleasedThisFrame)
                    OnWeaponAttackReleased?.Invoke(4);
            }
#endif

            // Sistema legacy (siempre activo como fallback)
            // Numpad 1
            if (Input.GetKeyDown(KeyCode.Keypad1)) 
            {
                abilityIndex = 1;
                Debug.Log("[InputManager] Legacy Numpad1 pressed directly");
            }
            // Numpad 2
            else if (Input.GetKeyDown(KeyCode.Keypad2)) 
            {
                abilityIndex = 2;
                Debug.Log("[InputManager] Legacy Numpad2 pressed directly");
            }
            // Resto de teclas
            else if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) abilityIndex = 1;
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) abilityIndex = 2;
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) abilityIndex = 3;
            else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) abilityIndex = 4;
            else if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6)) abilityIndex = 6;
            else if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7)) abilityIndex = 7;
            else if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8)) abilityIndex = 8;
            else if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9)) abilityIndex = 9;

            // Arma mantenida (hold) - fallback
            if (Input.GetKey(KeyCode.Alpha4) || Input.GetKey(KeyCode.Keypad4))
                OnWeaponAttackPressed?.Invoke(4);
            if (Input.GetKeyUp(KeyCode.Alpha4) || Input.GetKeyUp(KeyCode.Keypad4))
                OnWeaponAttackReleased?.Invoke(4);
            
            // DEBUG: Mostrar estado de teclas numpad
            if (Input.GetKeyDown(KeyCode.Keypad1)) Debug.Log("[InputManager] Direct Keypad1 check PASSED");
            if (Input.GetKeyDown(KeyCode.Keypad2)) Debug.Log("[InputManager] Direct Keypad2 check PASSED");

            if (abilityIndex >= 0)
            {
                Debug.Log($"[InputManager] Ability {abilityIndex} triggered (numpad compatible)");
                OnAbilityPressed?.Invoke(abilityIndex);
            }
        }

        private void HandleKatanaInput()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame)
            {
                KatanaAttackHoldTime = Time.time;
                OnKatanaAttackPressed?.Invoke();
#if DEBUG
                Debug.Log("[InputManager] KatanaAttackPressed event fired");
#endif
            }

            if (kb.digit5Key.wasReleasedThisFrame || kb.numpad5Key.wasReleasedThisFrame)
            {
                OnKatanaAttackReleased?.Invoke();
                KatanaAttackHoldTime = -1f;
#if DEBUG
                Debug.Log("[InputManager] KatanaAttackReleased event fired");
#endif
            }
#else
            if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
            {
                KatanaAttackHoldTime = Time.time;
                OnKatanaAttackPressed?.Invoke();
#if DEBUG
                Debug.Log("[InputManager] KatanaAttackPressed event fired (legacy)");
#endif
            }

            if (Input.GetKeyUp(KeyCode.Alpha5) || Input.GetKeyUp(KeyCode.Keypad5))
            {
                OnKatanaAttackReleased?.Invoke();
                KatanaAttackHoldTime = -1f;
#if DEBUG
                Debug.Log("[InputManager] KatanaAttackReleased event fired (legacy)");
#endif
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
            return kb.digit5Key.isPressed || kb.numpad5Key.isPressed;
#else
            return Input.GetKey(KeyCode.Alpha5) || Input.GetKey(KeyCode.Keypad5);
#endif
        }
    }
}
