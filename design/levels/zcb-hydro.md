# ZCB-HYDRO: Water Arena
## La Última Frontera Acuática

**Canon Level:** Established  
**Visible To Player:** Yes (mapa con mecánicas de plataformas acuáticas)  
**Cross-Reference:** `design/lore/biodeath-universe.md`

---

## 1. Historia de la ZCB-HYDRO

### 1.1 Origen

La ZCB-HYDRO fue una plataforma marina de investigación construida sobre el Océano Pacífico. Diseñada para estudiar y contener mutaciones de vida acuática expuesta a X-SPACE.

**Propósito Original:** Prevenir que mutaciones marinas lleguen a ecosistemas oceánicos globales

### 1.2 El Incidente de Marea (16-17 Nov 2087)

Las esporas dimensionales llegaron por aire, pero las mutaciones acuáticas resultaron ser las más impredecibles. La vida marina X-SPACE no sigue las reglas de biología terrestre.

**La Marea que No Debía Subir:**
- El nivel del "agua" comenzó a cambiar de formas imposibles
- Criaturas que no deberían existir emergieron
- La plataforma se volvió una isla en un mar que ya no era solo agua
- Personal atrapado entre dos peligros: el mar y la plataforma

### 1.3 Estado Actual

ZCB-HYDRO es una serie de plataformas sobre un océano alienígena. El agua ya no es H₂O pura; es una solución biológica que sustenta vida X-SPACE. Las plataformas son las únicas zonas "seguras".

**Características Únicas:**
- Agua que brilla con bioluminiscencia azul
- Plataformas flotantes (algunas inestables)
- Vida marina mutante visible debajo de superficie
- Cielo reflejado de formas imposibles en el agua

---

## 2. Environmental Storytelling

### 2.1 Elementos Visuales Clave

**Las Plataformas:**
- Estructuras de metal oxidado, barnacles X-SPACE (bioluminiscentes)
- Conectadas por pasarelas (algunas rotas)
- Campamentos de supervivencia abandonados
- Sistemas de bombeo aún intentando "purificar" el agua

**El Océano X-SPACE:**
- Agua de color azul profundo no natural
- Bioluminiscencia pulsante desde debajo
- Formas que nadan en profundidad (¿criaturas? ¿algo más?)
- Superficie que "respira" con ritmo orgánico

**El Horizonte Incorrecto:**
- Cielo y agua se funden sin línea de horizonte clara
- Reflejos imposibles (estructuras que no existen)
- Luces en profundidad que sugieren... ciudad?

### 2.2 Navegación Emocional

**La Isla en el Infinito:**
- Plataformas son refugio; el agua es peligro
- Sensación de estar atrapado, rodeado
- Navegación cuidadosa entre plataformas

**La Tentación del Agua:**
- Visualmente hermosa, casi hipnótica
- Pero mortal (caer al agua = daño/muerte)
- Criaturas que observan desde profundidad

**La Soledad Marina:**
- Sonido de olas pero sin el confort normal
- Viento marino con "olor" a bioluminiscencia
- Sensación de aislamiento extremo

### 2.3 Descubrimientos del Jugador

**Documentos Recuperables:**
- "Informe de Contaminación: El agua... ya no es agua. Es... otra cosa."
- "Log del Capitán: Las criaturas no vienen de abajo. Vienen de... ¿al lado?"
- "Mensaje final: No intenten nadar. El agua os quiere."

**Grabaciones de Audio:**
- Sonido de olas distorsionado
- Voces ahogadas (¿personal? ¿criaturas?)
- Sonido de profundidad: "latido" de océano vivo

**Elementos Interactivos:**
- Sistema de bombeo (intenta purificar, inútil)
- Botes salvavidas (narrativa de escape fallido)
- Cámaras submarinas (muestran lo que hay debajo)
- Plataformas inestables (se mueven al pisar)

---

## 3. Diseño de Nivel

### 3.1 Layout Espacial

**Concepto:** Archipiélago de plataformas con navegación riesgosa

**Estructura:**
```
[PLATAFORMAS EXTERIORES - Pequeñas, peligrosas]
    ↓ (saltos, pasarelas)
[PLATAFORMAS INTERMEDIAS - Principales, combate]
    ↓ (rutas múltiples)
[PLATAFORMA CENTRAL - Grande, segura relativa]
    ↑ (núcleo de supervivencia)
```

**Puntos de Interés:**
1. **Torre de Observación:** Vista de horizonte incorrecto
2. **Estación de Bombeo:** Narrativa de intento fallido de purificación
3. **Campamento de Botes:** Escape fracasado, botes vacíos
4. **Profundidad Visible:** Área de cristal donde se ve el agua debajo

### 3.2 Rutas de Flujo

**Ruta Principal (Oleadas):**
- Enemigos spawnan de agua (emergen)
- Avanzan hacia plataformas centrales
- Dificultad: Espacios reducidos, caídas mortales

**Rutas Secundarias:**
- Saltos entre plataformas (riesgo, velocidad)
- Pasarelas (seguro pero lento)
- Botes (movibles, riesgo de naufragio)

### 3.3 Pacing de Combate

**Wave 1-20:**
- Enemigos en plataformas principales
- Combate de espacio reducido
- Uso de borde como peligro táctico

**Wave 21-40:**
- Enemigos desde agua (emergen)
- Combate dinámico con plataformas pequeñas
- Estrategia: Empujar enemigos al agua

**Wave 41-60:**
- Plataformas se vuelven inestables (comienzan a hundirse)
- T-Rex emergen de agua (saltan a plataformas)
- Combate final en espacio que se reduce

---

## 4. Paleta de Colores y Mood

### 4.1 Esquema Visual

**Agua:**
- Azul profundo no natural, bioluminiscencia
- Superficie refleja luz de formas imposibles
- Profundidad oscura con puntos de luz (¿criaturas? ¿ciudad?)

**Plataformas:**
- Metal oxidado, cubierto de barnacles
- Estructuras blancas, manchadas de óxido
- Techos de campamentos (colores desvanecidos)

**Cielo y Horizonte:**
- Gradiente de azul marino a azul cielo (sin distinción clara)
- Reflejos imposibles en agua
- Bioluminiscencia en horizonte

### 4.2 Post-Procesado

**Global:**
- Color grading: azules saturados
- Reflejos exagerados en agua
- Fog en horizonte (esconde línea de agua-cielo)
- Bloom de bioluminiscencia marina

**Efectos Especiales:**
- Superficie de agua animada (shader personalizado)
- Bioluminiscencia bajo superficie
- Distorsión de visión cerca del agua

---

## 5. Enemigos y Encuentros

### 5.1 Composición de Oleadas

**Early Waves (1-20):**
- 40% Pirañas Voladoras (aéreas, dominan espacio)
- 40% Monos (en plataformas)
- 20% Sabrewulfs (saltan entre plataformas)

**Mid Waves (21-40):**
- 30% Pirañas Voladoras
- 30% Monos
- 30% Sabrewulfs
- 10% Devoradores Acuáticos (nuevos, desde agua)

**Late Waves (41-60):**
- 25% Pirañas Voladoras
- 25% Monos
- 25% Sabrewulfs
- 15% Devoradores Acuáticos
- **3 T-Rex Acuáticos** - Saltan del agua a plataformas

### 5.2 Enemigos Únicos de ZCB-HYDRO

**Devorador Acuático:**
- Criatura que emerge del agua
- Ataque de tentáculos/tubarón híbrido
- Regresa al agua después de atacar
- Solo vulnerable cuando está en superficie

**Piraña X-SPACE:**
- Variante de piraña voladora
- Más resistente, bioluminiscente
- Se mueve en cardúmenes coordinados

---

## 6. Audio y Atmósfera

### 6.1 Capas de Sonido

**Ambiente Base:**
- Sonido de olas distorsionado
- Viento marino constante
- "Latido" del océano desde profundidad

**Cerca del Agua:**
- Chapoteo de criaturas debajo
- Sonido de burbujas (comunicación submarina)
- Silencio repentino (algo grande se acerca)

**Combate:**
- Emergencia de criaturas del agua
- Pasos en metal de plataformas
- T-Rex saltando del agua (impacto masivo)

### 6.2 Música

**Exploración:**
- Ambiente marino pero "incorrecto"
- Notas de aislamiento y vastedad
- Melodía que sugiere profundidad y misterio

**Combate:**
- Percusión de sonidos de agua/metal
- Ritmo que imita olas
- Crescendo cuando T-Rex emerge

---

## 7. Requisitos de Assets

### 7.1 Nuevos Assets Necesarios

**Plataformas:**
- Estructuras marinas modulares (3 variantes)
- Pasarelas conectivas (rectas, curvas, rotas)
- Torres de observación (vista del horizonte)
- Estaciones de bombeo (industriales)

**Elementos de Agua:**
- Shader de agua X-SPACE (azul bioluminiscente)
- Botes salvavidas (vacíos, abandonados)
- Cámaras de observación submarina (cristal)
- Boyas (marcadores de zona segura)

**Efectos VFX:**
- Superficie de agua animada
- Bioluminiscencia submarina
- Emergencia de criaturas del agua
- Salpicaduras X-SPACE (brillan)

### 7.2 Assets Existentes a Reutilizar

- Rocas (como base de plataformas)
- Efectos de partículas de agua

---

## 8. Implementación Técnica

```csharp
private static void BuildHydroEnvironmentPremium()
{
    // Iluminación marina
    RenderSettings.ambientLight = new Color(0.2f, 0.3f, 0.4f, 1f);
    RenderSettings.fog = true;
    RenderSettings.fogColor = new Color(0.15f, 0.25f, 0.35f, 1f);
    RenderSettings.fogDensity = 0.02f;
    
    // Océano X-SPACE
    BuildXSpaceOcean();
    
    // Plataformas principales
    BuildMarinePlatforms(12);
    
    // Plataformas pequeñas (periféricas)
    BuildSmallPlatforms(8);
    
    // Pasarelas conectoras
    BuildWalkways();
    
    // Torre de observación
    BuildObservationTower();
    
    // Estación de bombeo
    BuildPumpingStation();
}
```

---

## 9. QA Checklist

- [ ] Plataformas son navegables sin frustración
- [ ] Agua visualmente espectacular pero claramente peligrosa
- [ ] Caídas al agua son penales claros pero no injustos
- [ ] Narrativa marina es clara sin explicación
- [ ] Combate en espacios reducidos es divertido
- [ ] T-Rex saltando del agua es momento cinematográfico

---

*Documento creado por: World-Builder + Level-Designer*
