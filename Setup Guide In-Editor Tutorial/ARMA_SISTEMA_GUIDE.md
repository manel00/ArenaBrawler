# Guía Completa del Sistema de Armas en Unity

## 1. Cómo Probar los Cambios en Unity (Paso a Paso)

### Método 1: Refrescar y Recompilar (Recomendado)

1. **Guarda todos los scripts** en Visual Studio Code (Ctrl+S)
2. **Cambia a Unity** (haz clic en la ventana de Unity)
3. **Espera a que Unity recompile** - verás:
   - La barra de progreso abajo de Unity
   - "Compiling..." en la esquina inferior derecha
4. **Cuando termine**, haz clic en el botón **Play** (▶) o presiona **Ctrl+P**
5. **Prueba tus cambios** en el juego

### Método 2: Forzar Recompilación Completa

1. Ve a **Edit > Preferences > General** (Windows) o **Unity > Preferences** (Mac)
2. Verifica que **Auto Refresh** esté activado
3. Si los cambios no aparecen:
   - Ve a **Assets > Refresh** o presiona **Ctrl+R**
   - O usa **File > Refresh**

### Método 3: Play Mode y Script Changes

1. Ve a **Edit > Preferences > General**
2. Busca **"Script Changes While Playing"**
3. Configúralo a:
   - **Recompile After Finished Playing**: Recompila cuando pares el juego
   - **Recompile And Continue Playing**: Recompila en tiempo real (puede causar errores)

### Solución de Problemas Comunes

| Problema | Solución |
|----------|----------|
| Los cambios no aparecen | Guarda en VS Code, luego Refresh en Unity (Ctrl+R) |
| Errores de compilación | Revisa la Consola (Ctrl+Shift+C) en Unity |
| El juego no inicia | Hay errores en los scripts - revisa Console |
| Cambios no persistentes | Asegúrate de guardar escenas (Ctrl+S) |

---

## 2. Cómo Agrandar la Pantalla del Juego en Unity

### Método 1: Game View en Unity Editor

1. **Abre la ventana Game View** (menú: Window > General > Game)
2. **Free Aspect** - haz clic en el desplegable
3. **Selecciona una resolución mayor** o:
   - **Click en "+"** para añadir resolución personalizada
   - Ejemplo: 1920x1080, 1600x900, etc.
4. **Maximiza el Game View** - botón "Maximize on Play" en Game View

### Método 2: Pantalla Completa

1. En el **Game View**, busca el botón **"Maximize on Play"** (icono de pantalla completa)
2. Haz **Play** y el juego se verá en pantalla completa
3. Para salir: presiona **Escape** o haz clic en **Play** de nuevo

### Método 3: Configurar Resolución por Defecto

1. Ve a **File > Build Settings**
2. Selecciona tu plataforma (PC, Mac, etc.)
3. Haz clic en **Player Settings**
4. En **Resolution and Presentation**:
   - **Default Is Native Resolution**: ON
   - **Fullscreen Mode**: Elegir "Fullscreen Window" o "Exclusive Fullscreen"
   - **Default Screen Width/Height**: Configura tu resolución deseada

### Método 4: Cambiar Tamaño de la Ventana en Builds

```csharp
// En un script para cambiar la resolución en tiempo de ejecución
Screen.SetResolution(1920, 1080, false); // Ancho, Alto, Fullscreen
```

---

## 3. Sistema de Armas: Recoger y Usar

### Archivos Creados

1. **WeaponData.cs** - ScriptableObject con datos del arma
2. **WeaponPickup.cs** - Componente para armas en el suelo
3. **PlayerWeaponSystem.cs** - Sistema del jugador para equipar/usar

### Cómo Configurar en Unity

#### Paso 1: Crear WeaponData

1. En Project: **Right-click > Create > WoW > Weapon Data**
2. Configura:
   - **Weapon Name**: "Espada de Fuego"
   - **Damage**: 25
   - **Attack Cooldown**: 0.5 segundos
   - **Max Durability**: 5
   - **Weapon Color**: Color rojo/naranja
   - **Weapon Scale**: (1, 1, 1)

#### Paso 2: Añadir PlayerWeaponSystem al Jugador

1. Selecciona tu **GameObject del jugador**
2. **Add Component > PlayerWeaponSystem**
3. Configura:
   - **Weapon Hold Point**: Crea un Empty GameObject hijo como "Hand" y arrástralo aquí
   - **Attack Key**: Mouse0 (clic izquierdo)
   - **Pickup Key**: E
   - **Drop Key**: Q

#### Paso 3: Crear Arma en el Suelo (Spawn)

```csharp
// Para spawnear un arma en una posición específica
using WoW.Armas;

// En cualquier script o en Start()
WeaponData miEspada = /* tu WeaponData */;
Vector3 posicionSpawn = new Vector3(5, 0.5f, 3);
WeaponPickup pickup = WeaponPickup.CreatePickup(miEspada, posicionSpawn);
```

#### Paso 4: Spawn Automático en el Arena

1. Abre **ArenaFixed.cs** o tu script de arena
2. Añade en el método `SpawnInitialWeapons()`:

```csharp
private void SpawnInitialWeapons()
{
    // Spawnear armas en diferentes posiciones
    if (weaponSpawnPoints != null && weaponSpawnPoints.Length > 0)
    {
        foreach (var spawnPoint in weaponSpawnPoints)
        {
            // Seleccionar un WeaponData aleatorio
            WeaponData randomWeapon = allWeapons[Random.Range(0, allWeapons.Length)];
            WeaponPickup.CreatePickup(randomWeapon, spawnPoint.position);
        }
    }
}
```

---

## 4. Mecánica: 5 Usos y Rotura del Arma

### Flujo del Sistema

```
1. Jugador recoge arma del suelo
   ↓
2. Tiene 5 "usos" antes de romperse
   ↓
3. Cada ataque = 1 uso
   ↓
4. Después de 5 ataques → El arma se ROMPE y DESAPARECE
   ↓
5. Jugador debe recoger OTRA arma del suelo
```

### Configuración de Durabilidad

| WeaponData.maxDurability | Comportamiento |
|--------------------------|----------------|
| 5 | Se rompe exactamente después de 5 usos |
| 10 | Se rompe después de 10 usos |
| 3 | Se rompe después de 3 usos (arma frágil) |

### Cómo Funciona Internamente

```csharp
// En PlayerWeaponSystem.Attack()
_usesSinceLastPickup++;

if (_usesSinceLastPickup >= MAX_USES_BEFORE_DROP) // 5
{
    BreakWeapon(); // El arma desaparece
}
```

### Mostrar Durabilidad en UI

```csharp
// En tu script de UI
public class WeaponUI : MonoBehaviour
{
    public Text durabilityText;
    public Image durabilityBar;
    
    private void Start()
    {
        var weaponSystem = GetComponent<PlayerWeaponSystem>();
        weaponSystem.OnDurabilityChanged += UpdateDurabilityUI;
    }
    
    private void UpdateDurabilityUI(int current, int max)
    {
        durabilityText.text = $"{current}/{max}";
        durabilityBar.fillAmount = (float)current / max;
    }
}
```

---

## 5. Cómo Usar Texturas de "My Assets"

### Si tienes assets en una carpeta "My Assets"

1. **Localiza los assets** en el Project window
2. **Importa las texturas**:
   - Arrastra las imágenes a Unity, o
   - **Assets > Import New Asset**
3. ** formatos soportados**: PNG, JPG, TGA, PSD

### Aplicar Texturas a Materiales

1. **Create > Material** (en Project)
2. **Arrastra la textura** al slot "Albedo" del material
3. **Aplica el material** al GameObject:
   - Selecciona el objeto
   - Arrastra el material desde Project al objeto

### Usar Texturas con los Armas

```csharp
// En WeaponData.cs, añade:
public class WeaponData : ScriptableObject
{
    // ... campos existentes ...
    
    public Texture2D weaponTexture; // Añade este campo
    
    // En WeaponPickup.cs, aplica la textura:
    var meshRenderer = GetComponent<MeshRenderer>();
    if (meshRenderer != null && weaponData.weaponTexture != null)
    {
        meshRenderer.material.mainTexture = weaponData.weaponTexture;
    }
}
```

### Configurar Texturas en WeaponData

1. Selecciona tu **WeaponData** en el Project
2. En el Inspector, arrastra tu **textura al campo "Weapon Texture"**
3. Asegúrate que el material usa un **shader que soporta texturas** (Standard, URP Lit, etc.)

---

## 6. Resumen de Controles

| Acción | Tecla |
|--------|-------|
| Atacar | **Click izquierdo** (Mouse0) |
| Recoger arma | **E** |
| Soltar arma | **Q** |
| Mover personaje | WASD |
| Mirar | Ratón |

---

## 7. Próximos Pasos Recomendados

1. ✅ Sistema básico de armas funcionando
2. ⬜ Crear modelos 3D de armas reales (o usar primitives con mejoras)
3. ⬜ Añadir animaciones de ataque
4. ⬜ Sistema de daño a enemigos
5. ⬜ Efectos de partículas al romper
6. ⬜ Sonidos de recogida y rotura
7. ⬜ UI mostrando arma actual y durabilidad

---

## 8. Archivos del Proyecto

```
Assets/SourceFiles/Scripts/Weapons/
├── WeaponData.cs          # ScriptableObject con datos del arma
├── WeaponPickup.cs        # Componente para armas en el suelo
└── PlayerWeaponSystem.cs  # Sistema del jugador
```

Para más información, consulta **ARCHITECTURE.md** y **ROADMAP.md**.