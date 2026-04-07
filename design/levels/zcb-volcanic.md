# ZCB-MAGMA: Volcanic Coast
## La Forja de Purificación

**Canon Level:** Established  
**Visible To Player:** Yes (mapa térmico, atmósfera de destrucción controlada)  
**Cross-Reference:** `design/lore/biodeath-universe.md`

---

## 1. Historia de la ZCB-MAGMA

### 1.1 Origen

La ZCB-MAGMA fue el experimento más desesperado del PROYECTO THRESHOLD. Construida en una zona de actividad volcánica, BIOHORIZON intentó usar calor extremo para "quemar" la infección X-SPACE.

**Propósito Original:** Terraformación geotérmica como método de esterilización biológica

### 1.2 El Experimento de Purificación (17-20 Nov 2087)

Cuando las otras ZCB cayeron, la ZCB-MAGMA intentó contener la propagación activando sistemas geotérmicos masivos. La teoría: X-SPACE no podría sobrevivir temperaturas extremas.

**La Teoría Fallida:**
- Sistemas geotérmicos fueron activados
- Calor extremo fue aplicado a muestras X-SPACE
- Resultado: X-SPACE no murió... se adaptó
- Nacieron las Termo-Mutaciones

### 1.3 Estado Actual

ZCB-MAGMA es una costa volcánica donde el fuego y X-SPACE coexisten. Es el territorio de las mutaciones térmicas - criaturas que prosperan en calor extremo.

**Características Únicas:**
- Ríos de lava fluyen al lado de bioluminiscencia
- Temperatura variable (zonas calientes/frías)
- Estructuras de enfriamiento aún funcionando (parcialmente)
- Cenizas que caen constantemente

---

## 2. Environmental Storytelling

### 2.1 Elementos Visuales Clave

**La Forja Fallida:**
- Estructuras industriales para enfriamiento geotérmico
- Torres de enfriamiento que aún emiten vapor
- Tuberías rotas que liberan vapor/lava
- Mensaje: "La ciencia intentó contener lo inconmensurable"

**Ríos de Fuego y Luz:**
- Lava que fluye junto a bioluminiscencia
- Dos fuentes de luz: naranja (calor) y azul/púrpura (X-SPACE)
- Contrastes visuales dramáticos
- Áreas donde ambas se mezclan (morada naranja)

**Las Cenizas Eternas:**
- Ceniza volcánica cae constantemente
- Cubre superficies con polvo gris
- Ocasionalmente, cenizas "brillan" con X-SPACE
- Sensación de mundo que se reconstruye y destruye

### 2.2 Navegación Emocional

**La Lucha de Fuego vs. X-SPACE:**
- Calor extremo como aliado temporal
- Zonas de lava son peligrosas para todos
- Fuego quema tanto aliados como enemigos
- ¿Está ganando el fuego? ¿O X-SPACE lo absorbe?

**La Desesperación Científica:**
- Evidencia de intento desesperado de contención
- Estructuras que sugieren últimos momentos de control
- Narrativa de "funcionó temporalmente, luego falló"

**El Calor como Refugio:**
- Cerca de lava, enemigos térmicos son más débiles
- Pero también el jugador sufre daño por calor
- Elección táctica: riesgo vs. recompensa

### 2.3 Descubrimientos del Jugador

**Documentos Recuperables:**
- "Protocolo PURGATORIO: Activar todos los sistemas geotérmicos. Quemar todo."
- "Informe térmico: Las muestras X-7 no mueren a 1000°C. Se vuelven más activas."
- "Último mensaje del Director: El fuego no es suficiente. Necesitamos algo más frío."

**Grabaciones de Audio:**
- Alarma de sobrecalentamiento
- Sonido de sistemas de enfriamiento colapsando
- Rugido de criaturas que emergen de lava

**Elementos Interactivos:**
- Válvulas de geotermia (liberan vapor/lava, daño a enemigos)
- Torres de enfriamiento (zonas seguras temporales)
- Muestras de X-SPACE térmica (lore sobre adaptación)
- Sistemas de monitoreo (temperatura de cada zona)

---

## 3. Diseño de Nivel

### 3.1 Layout Espacial

**Concepto:** Zonas térmicas contrastantes, uso de lava como arma ambiental

**Estructura:**
```
[ZONAS FRÍAS - Torres de enfriamiento, relativamente seguras]
    ↓ (rutas entre torres)
[ZONAS TEMPLADAS - Zona de combate principal]
    ↓ (cruces de lava)
[ZONAS CALIENTES - Lava fluye, peligro extremo]
    ↑ (refugio temporal si enemigos son térmicos)
```

**Puntos de Interés:**
1. **Torre de Enfriamiento Central:** Zona más segura, campamento base
2. **Puente de Lava Solidificada:** Ruta riesgosa, atajo
3. **Cráter Activo:** Centro del mapa, clímax visual
4. **Estación Geotérmica:** Control de lava (interactuable)

### 3.2 Rutas de Flujo

**Ruta Principal (Oleadas):**
- Enemigos spawnan de zonas calientes (resisten calor)
- Avanzan hacia zonas frías (donde el jugador está)
- Dificultad: Enemigos térmicos son resistentes, pero zona fría los debilita

**Rutas Secundarias:**
- Atajo por zona de lava (rápido pero daño constante)
- Túneles de enfriamiento (seguro, criaturas frías)
- Puentes sobre lava (riesgo de caída, enemigos caen y mueren)

### 3.3 Pacing de Combate

**Wave 1-20:**
- Enemigos estándar, sin resistencia térmica especial
- Jugador usa lava para dañar enemigos
- Estrategia: Empujar hacia lava

**Wave 21-40:**
- Enemigos térmicos (resistentes a fuego)
- Zonas frías se vuelven importantes tácticamente
- Lava ya no mata enemigos (se adaptaron)

**Wave 41-60:**
- Cráter comienza a erupcionar
- T-Rex Térmicos emergen de lava
- Mapa cambia (lava fluye en nuevas rutas)
- Combate final con ambiente en transformación

---

## 4. Paleta de Colores y Mood

### 4.1 Esquema Visual

**Fuego y Lava:**
- Naranjas, rojos, amarillos intensos
- Brillo de lava en oscuridad
- Sombras que bailan con luz de fuego

**X-SPACE en Calor:**
- Bioluminiscencia púrpura que contrasta con naranja
- Morado-naranja donde ambos se mezclan
- Cristales térmicos X-SPACE (brillan en calor)

**Estructuras Industriales:**
- Metal gris, oxidado por calor
- Torres de enfriamiento con vapor
- Sistemas de tuberías (algunas rotas, liberan lava)

### 4.2 Post-Procesado

**Global:**
- Color grading: cálido, saturación alta en naranjas/rojos
- Heat distortion (ondulación de aire caliente)
- Bloom intenso en fuentes de calor
- Particle system de cenizas cayendo

**Efectos Especiales:**
- Shader de lava (animado, brillante)
- Vapor/volcanic smoke
- Heat shimmer (distorsión térmica)
- Cenizas que se acumulan en superficies

---

## 5. Enemigos y Encuentros

### 5.1 Composición de Oleadas

**Early Waves (1-20):**
- 60% Monos estándar (vulnerables a fuego)
- 30% Sabrewulfs
- 10% Abejas (evitan zonas calientes)

**Mid Waves (21-40):**
- 40% Monos estándar
- 30% Monos Térmicos (resistentes a fuego)
- 20% Sabrewulfs
- 10% Abejas

**Late Waves (41-60):**
- 20% Monos estándar
- 40% Monos Térmicos
- 20% Sabrewulfs
- 10% Abejas Térmicas (fuego + enjambre)
- **3 T-Rex Térmicos** - Emergen de lava, cubiertos de magma

### 5.2 Enemigos Únicos de ZCB-MAGMA

**Mono Térmico:**
- Piel que brilla con calor residual
- Resistente a fuego y explosiones
- Deja rastro de calor (daño al acercarse)
- Debilidad: Zonas frías (ralentizado)

**Sabrewulf de Ceniza:**
- Variante que emerge de cenizas
- Camuflaje perfecto en ambiente
- Ojos brillan como brasas

**Abeja Térmica:**
- Enjambre que genera calor
- Contacto causa quemaduras
- Explota al morir (daño de área)

---

## 6. Audio y Atmósfera

### 6.1 Capas de Sonido

**Ambiente Base:**
- Rugido constante de volcanes distantes
- Chisporroteo de lava
- Viento que lleva cenizas

**Sistemas Industriales:**
- Alarma de sobrecalentamiento
- Sonido de torres de enfriamiento
- Válvulas que liberan presión

**Combate:**
- Enemigos que emergen de lava (splash de fuego)
- Criaturas térmicas (sonidos de brasas)
- T-Rex cubierto de magma (hissing de vapor)

### 6.2 Música

**Exploración:**
- Ambiente de tensión geotérmica
- Notas de inestabilidad e inevitabilidad
- Percusión de sonidos industriales

**Combate:**
- Música intensa, ritmo de marcha industrial
- Crescendo cuando cráter entra en erupción
- Para T-Rex: sonido de maquinaria + rugido biológico

---

## 7. Requisitos de Assets

### 7.1 Nuevos Assets Necesarios

**Elementos Volcánicos:**
- Ríos de lava (shaders animados)
- Rocas volcánicas (negras, rojas)
- Cráteres (modelos de terreno)
- Ceniza cayendo (particle system)

**Estructuras Industriales:**
- Torres de enfriamiento (3 variantes)
- Sistemas de tuberías (modular)
- Válvulas industriales (interactuables)
- Generadores geotérmicos (dañados)

**Efectos VFX:**
- Lava shaders (animados)
- Vapor/volcanic smoke
- Heat distortion
- Cenizas acumulándose

### 7.2 Assets Existentes a Reutilizar

- Rocas del TerrainDemoScene_URP (recolorear)
- Efectos de fuego y explosión
- Estructuras de korean temple (industriales)

---

## 8. Implementación Técnica

```csharp
private static void BuildVolcanicEnvironmentPremium()
{
    // Iluminación cálida
    RenderSettings.ambientLight = new Color(0.1f, 0.05f, 0.02f, 1f);
    RenderSettings.fog = true;
    RenderSettings.fogColor = new Color(0.15f, 0.08f, 0.05f, 1f);
    RenderSettings.fogDensity = 0.03f;
    
    // Suelo volcánico
    BuildVolcanicGround();
    
    // Ríos de lava
    BuildLavaRivers();
    
    // Torres de enfriamiento
    BuildCoolingTowers(4);
    
    // Cráter activo (centro)
    BuildActiveCrater();
    
    // Sistemas de tuberías
    BuildIndustrialPipes();
    
    // Efectos de ceniza
    BuildAshEffects();
}
```

---

## 9. QA Checklist

- [ ] Lava es claramente peligrosa visualmente
- [ ] Zonas térmicas son tácticamente interesantes
- [ ] Narrativa de purificación fallida es clara
- [ ] Contraste fuego/X-SPACE es visualmente espectacular
- [ ] T-Rex Térmico emergiendo de lava es impactante
- [ ] Performance con múltiples efectos de fuego es estable

---

*Documento creado por: World-Builder + Level-Designer*
