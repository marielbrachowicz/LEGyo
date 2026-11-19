# ARMyo Mirror – Miembro Inferior

Visualizador muscular interactivo con realidad aumentada (Magic Mirror) para el miembro inferior: cadera, rodilla y pie.
Se construye tomando como base el proyecto ARMyo de **miembro superior**.

Propuesta original: [00_Documentacion/Propuesta_ARMyo_MiembroInferior.webp](00_Documentacion/Propuesta_ARMyo_MiembroInferior.webp)

---

## 1. Estructura de la carpeta

```
ARMyo_Mirror/
├── LEEME.md                         ← este archivo
├── 00_Documentacion/                ← propuesta, informes, notas
├── 01_Referencia_MiembroSuperior/   ← proyecto Unity del brazo (SOLO CONSULTA, no se modifica)
├── 02_Modelos3D/
│   ├── PiernaAnimada/               ← MODELO PRINCIPAL: PIERNA.fbx + texturas
│   └── CuerpoCompleto_Rigged/       ← Color_Muscular_System_Rigged.fbx (cuerpo completo)
├── 03_Unity_MiembroInferior/        ← NUESTRO proyecto Unity (aquí se trabaja)
└── _Descargas_originales/           ← los .zip tal como se descargaron (respaldo)
```

## 2. Cómo se relacionan los archivos

| Archivo | Qué es | Para qué sirve |
|---|---|---|
| **ARMyOMagicMirror_MiembroSuperior** (→ `01_`) | Proyecto Unity terminado del brazo (Grupo 4) | Es la **plantilla**: su código se adapta de brazo a pierna |
| **PiernaAnimada** (→ `02_/PiernaAnimada`) | Modelo anatómico (huesos + músculos) con **9 animaciones** ya hechas | Es el **modelo 3D de la mitad virtual** |
| **Color_Muscular_System_Rigged.fbx** (→ `02_/CuerpoCompleto_Rigged`) | Cuerpo completo con rig. Es **idéntico** al que usa el proyecto del brazo | Base de la que sale la pierna. Sirve de respaldo |
| **Proyecto_1_ARMyo** (→ `03_`) | Proyecto Unity vacío con el paquete Azure Kinect Examples | Aquí se construye la versión de pierna |

Las texturas `Ecorche_*.png` son las mismas en los tres: todos vienen del mismo modelo anatómico.

### Animaciones que trae `PIERNA.fbx`
| # | Animación | Músculos principales (agonistas) |
|---|---|---|
| 0 | IDLE_DESCANSO | – |
| 1 | FLEXION_CADERA | Psoas mayor, ilíaco, recto femoral, sartorio, tensor de la fascia lata |
| 2 | EXTENSION_CADERA | Glúteo mayor, bíceps femoral (cabeza larga), semitendinoso, semimembranoso |
| 3 | ABDUCCION_CADERA | Glúteo medio, glúteo menor, tensor de la fascia lata |
| 4 | ADUCCION_CADERA | Aductor largo, aductor corto, aductor mayor, grácil, pectíneo |
| 5 | FLEXION_RODILLA | Bíceps femoral, semitendinoso, semimembranoso, gastrocnemio, grácil, sartorio |
| 6 | EXTENSION_RODILLA | Cuádriceps: recto femoral, vasto lateral, vasto medial, vasto intermedio |
| 7 | ROTACION_INTERNA_CADERA | Glúteo medio y menor (fibras anteriores), tensor de la fascia lata |
| 8 | ROTACION_EXTERNA_CADERA | Glúteo mayor, sartorio (y rotadores profundos, que no están en el modelo) |

El modelo tiene **todos estos músculos como piezas separadas** (por ejemplo `Rectus_Femoris_R` o `Vastus_Lateralis_R`), así que cada uno se puede colorear por separado.
Además trae la rótula (`Patella_R`), el ligamento rotuliano, el fémur, la tibia, el peroné y los huesos del pie.

## 3. Cómo funciona el proyecto del brazo (lo que vamos a copiar)

Código propio en `01_Referencia_MiembroSuperior/Assets/Scripts_Mixto/`:

```
Kinect ─► KinectBridge ─► JointAngleMeasurer ─► MovementClassifier ─► texto "Flexión / Extensión"
            (lee joints)     (calcula ángulo)          │
                                                      ├─► MuscleIntensityByAngle ─► color del músculo
                                                      └─► MuscleAnatomyData / AnatomyUIManager ─► nombres en pantalla
          ArmInspectCamera (gesto: mano derecha toca el brazo izq. 1.5 s) ─► ZoneSessionController ─► zoom a hombro/codo/muñeca
```

### Adaptación brazo → pierna

| Script del brazo | Qué hace | Cambio para la pierna |
|---|---|---|
| `KinectBridge` + `ArmBodyFrame` | Lee hombro, codo y muñeca | Leer **cadera, rodilla, tobillo y pie** (`HipLeft/Right`, `KneeLeft/Right`, `AnkleLeft/Right`, `FootLeft/Right`) → `LegBodyFrame` |
| `JointAngleMeasurer` | Ángulo hombro–codo–muñeca | Ángulo **cadera–rodilla–tobillo**. Plano **YZ (sagital)**, porque la rodilla se flexiona hacia atrás |
| `MovementClassifier` | Flexión/extensión según la velocidad | Casi igual. Agregar presets de rodilla y cadera |
| `MuscleIntensityByAngle` | Músculo más rojo según el ángulo | Igual. Solo se reconfiguran los rangos |
| `MuscleAnatomyData` | Nombre y acción de cada músculo | Igual, con los datos de la tabla de arriba |
| `ArmZoneType` | Hombro / Codo / Muñeca | `LegZoneType`: **Cadera / Rodilla / Tobillo** |
| `ArmInspectCamera` | Gesto: la mano toca el brazo | La **mano contraria toca el muslo o la rodilla** (al tobillo no se llega de pie) |
| `ForearmController`, `ShoulderAbductionMeasurer` | Específicos del brazo | Reemplazar por un medidor de **abducción de cadera** |
| `FreezeHips`, `CircularBuffer`, `JointFollower`, `MuscleLabel*` | Utilidades | Se pueden reutilizar casi sin cambios |

Escenas del brazo: `EscenaPrincipal` (menú), `EscenaMixta` (la mitad real y la mitad virtual que pide la propuesta), `EscenaAR` y `EscenaVR`.

## 4. Estado del equipo (revisado el 19/11/2026)

| Elemento | Estado |
|---|---|
| Kinect for Windows SDK 2.0 | ✅ Instalado |
| Unity **2022.3.62f3** (la versión del proyecto) | ✅ Instalado. No abrir el proyecto con Unity 6 |
| Kinect v2 + adaptador para PC | ✅ Funciona. Lo detecta Windows y lo usa Unity (`Kinect2Interface` en *Connected Sensor*) |
| Editor de código (VS Code y Visual Studio) | ✅ Instalados |
| Git | ✅ Instalado. El repositorio está en GitHub: `marielbrachowicz/LEGyo` |
| Blender | ❌ No instalado. Solo hace falta si hay que crear las animaciones de tobillo o retocar las existentes |
| Tarjeta gráfica | Intel integrada. Sirve para **Kinect v2**, pero **no** para el body tracking de Azure Kinect, que necesita NVIDIA |

Cada integrante debe tener lo mismo en su PC: Unity 2022.3.62f3, Kinect SDK 2.0, Git y la carpeta `01_Referencia_MiembroSuperior` (ver la sección 5).

## 5. Para quien continúa el trabajo (desde GitHub)

0. ⚠️ **Requisito obligatorio:** copia la carpeta **`01_Referencia_MiembroSuperior`** (el proyecto del brazo, unos 3,3 GB) **en la raíz del repositorio** con ese nombre exacto. No está en GitHub por su tamaño. `.gitignore` la excluye, así que no se sube por error. Sin ella no se pueden consultar los scripts del brazo que hay que adaptar en la Fase 3.
1. Instala **Unity 2022.3.62f3**, exactamente esa versión, y el **Kinect for Windows SDK 2.0**.
2. Clona el repositorio y abre la carpeta `03_Unity_MiembroInferior` desde Unity Hub. La primera vez tarda porque se regenera la carpeta `Library`.
3. Abre `Assets/ARMyo/Scenes/PruebaPierna` y dale a **Play**, de frente al Kinect v2, a 2–3 m. Los ángulos aparecen arriba a la derecha.
   - Si la escena no tiene `KinectController`, usa el menú **ARMyo → 2. Añadir Kinect v2 a la escena**.
   - Teclas: **0–8** cambian la animación, **R** reinicia el máximo y **P** copia los datos crudos a la Console.
   - Fase 4: menú **ARMyo → 4** crea `EscenaMixta` (cámara a la izquierda, modelo a la derecha). Si se vuelve a ejecutar el menú 3, hay que repetir también el 4.
   - Fase 3: menú **ARMyo → 3. Añadir animación en vivo (dos piernas)**. Desde ese momento cada pierna sigue su rodilla y las teclas 0–8 quedan desactivadas. Sin Kinect: **↑/↓** mueven la pierna derecha, **W/S** la izquierda, **Espacio** hace un ciclo automático con las piernas alternadas y **K** vuelve al Kinect.
4. Lo que **no** está en GitHub porque pesa demasiado, y se pide aparte (USB, Drive…):
   - `01_Referencia_MiembroSuperior/`: **obligatoria** (ver el paso 0). Es el proyecto del brazo, que sirve de plantilla para la Fase 3 (`MovementClassifier`, `MuscleIntensityByAngle`, `MuscleAnatomyData`).
   - `02_Modelos3D/`: el modelo del cuerpo completo, que es solo de respaldo. `PIERNA.fbx` ya está dentro de `Assets`.
   - `onnxruntime_providers_cuda.dll`, que solo hace falta para Azure Kinect, no para Kinect v2.
5. **Siguiente paso: la Fase 3.** La pierna 3D debe imitar en vivo el ángulo del usuario, y sus músculos deben colorearse según el movimiento. Las fuentes de datos ya existen: `MedidorAngulosPierna` publica `OnAngulosCambiados(rodilla, flexiónCadera, abducciónCadera)` y expone las propiedades `FlexionRodilla`, `FlexionCadera` y `AbduccionCadera`.

## 6. Lo que falta (checklist)

### Instalación
- [x] Instalar **Unity 2022.3.62f3** desde Unity Hub (Installs → Install Editor → Archive). No abrir los proyectos con Unity 6.
- [x] Instalar **VS Code** + extensión "Unity" (o Visual Studio Community con "Desarrollo de juegos con Unity").
- [ ] Instalar **Blender** (solo si hay que retocar animaciones o shape keys).
- [x] Conectar el **Kinect v2** con su adaptador para PC y probarlo.
- [x] Instalar **Git** y subir el proyecto a GitHub (19/11/2026).

### Decisiones pendientes
- [x] **Sensor**: decidido **Kinect v2**. Es el que pide la propuesta y el único viable con gráfica Intel. El proyecto del brazo usaba Azure Kinect.
- [x] **Pierna**: decidido el 19/11/2026, **las dos**, juntas como en un cuerpo completo. La izquierda es una copia en espejo de `PIERNA.fbx`, porque el modelo solo trae la derecha.
- [ ] **Pie**: la propuesta lo menciona, pero `PIERNA.fbx` **no trae animaciones de tobillo** (dorsiflexión y plantiflexión). Hay que hacerlas en Blender o dejarlas fuera del alcance.
- [x] **Gesto de activación**: decidido el 19/11/2026 (lo pidió el profesor): **por voz**, no con la mano. Falta definir qué palabras se usan y qué hace cada una.
- [x] **Animación**: decidido el 19/11/2026. El modelo debe seguir el ángulo real del usuario **en vivo**, por ejemplo avanzando cada animación según el ángulo medido.

### Desarrollo en `03_Unity_MiembroInferior`
- [x] Fase 0: abrir el proyecto en 2022.3.62f3 y probar una demo del paquete con el Kinect v2 (19/11/2026: funciona con `Kinect2Interface` en *Connected Sensor*).
- [x] Fase 1: importar `PIERNA.fbx` y sus texturas, y comprobar las 9 animaciones (menú **ARMyo → 1**).
- [x] Fase 2: portar `KinectBridge` → versión pierna y medir el **ángulo de rodilla en vivo**. Probado con el sensor el 19/11/2026: mide bien de pie a unos 2–3 m. Si el usuario se acerca demasiado, las piernas salen del campo de visión y se deja de medir, lo cual es normal en el Kinect v2.
  - `Scripts/Core/LegBodyFrame.cs`: joints de la pierna y cálculo de la flexión de rodilla (3D), la flexión de cadera (plano YZ) y la abducción de cadera (plano XY).
  - `Scripts/Sensores/KinectBridgePierna.cs`: lee el Kinect y publica un `LegBodyFrame` en cada frame. Se elige la pierna derecha o la izquierda.
  - `Scripts/Biomecanica/MedidorAngulosPierna.cs`: suaviza los ángulos y los muestra en pantalla.
  - `Editor/ConfigurarKinect.cs`: menú **ARMyo → 2. Añadir Kinect v2 a la escena**.
- [x] Fase 3: `MovementClassifier` + `MuscleIntensityByAngle` → los músculos cambian de color al flexionar y extender. **Probado con el Kinect v2 el 19/11/2026** (rama `fase3-animacion`): la pierna 3D sigue la rodilla casi sin retraso y reconoce flexión y extensión. El clip `FLEXION_RODILLA` solo llega a **88°**.
  - **Dos piernas** (rama `ambas-piernas`, **probado con el Kinect v2 el 19/11/2026**: cada pierna sigue su rodilla y se dobla bien por encima de 88°): el menú 3 crea `Pierna_Izquierda` como copia en espejo respecto al sacro. Oculta en la copia la columna y el suelo pélvico para que no se dibujen dos veces, y añade un segundo `KinectBridgePierna` + `MedidorAngulosPierna` para la pierna izquierda. `KinectBridgePierna` ya no destruye el segundo puente.
  - **Más de 88° sin Blender**: `PiernaAnimadaEnVivo` sigue doblando la rodilla por código, sobre el eje que usa el clip, hasta `anguloMaxModelo` (135°).
  - `Scripts/Biomecanica/FuenteAngulosPierna.cs`: toma el ángulo de `MedidorAngulosPierna` o lo simula con el teclado (**↑/↓**, **Espacio** = ciclo automático, **K** = volver al Kinect).
  - `Scripts/Biomecanica/MovementClassifier.cs`: flexión / extensión / mantiene / reposo según la velocidad del ángulo, con presets de rodilla y cadera.
  - `Scripts/Animacion/PiernaAnimadaEnVivo.cs`: fija el tiempo de `FLEXION_RODILLA` según el ángulo medido. Al arrancar mide el clip (huesos `Leg_Upper.R` / `Leg_Knee.R`) para que el modelo muestre el mismo ángulo que el usuario.
  - `Scripts/Anatomia/MuscleAnatomyData.cs`, `MuscleIntensityByAngle.cs`, `PanelMovimientoPierna.cs`: nombres y acción de los músculos, color blanco → rojo con `MaterialPropertyBlock` y panel en pantalla.
  - Músculos que se colorean (nombres reales del FBX): flexores `Biceps_Femoris_Long_Head_R`, `Biceps_Femoris_Short_Head_R`, `Semitendinosus_R`, `Semimembranosus_R`, `Gastrocnemius_Lateral_Medial_R`; extensores `Rectus_Femoris_R`, `Vastus_Lateralis_R`, `Vastus_Medialis_R`, `Vastus_Intermedius_R`. Dentro de Unity, la malla que se pinta de cada uno se llama `Nombre.001` (por ejemplo `Rectus_Femoris.001`). **El semitendinoso no tiene malla propia**: sale en pantalla, pero no cambia de color.
  - `Editor/ConfigurarAnimacionEnVivo.cs`: menú **ARMyo → 3. Añadir animación en vivo**.
- [ ] Fase 4: escena mixta (mitad real con el espejo, mitad virtual con el modelo) y la ventana AR sobre la rodilla.
  - **Primera parte hecha** (rama `fase4-escena-mixta`, falta probarla en Unity): menú **ARMyo → 4. Crear escena mixta (cámara | modelo)**. Copia `PruebaPierna` en `Scenes/EscenaMixta.unity` y divide la pantalla en tres columnas: a la izquierda (45 %), la imagen en color del Kinect en espejo (`Scripts/Mixta/VistaCamaraKinect.cs`); en el centro (30 %), las dos piernas virtuales, encuadradas por `EncuadrarModelo`; a la derecha (25 %), los paneles, uno debajo de otro. Los paneles se escalan con el tamaño de la ventana (`Core/EscalaUI.cs`). **E** invierte la imagen. Probado el 19/11/2026: va fluido con la gráfica Intel.
  - Falta: la ventana AR sobre la rodilla.
- [ ] Fase 5: activación **por voz** (`KeywordRecognizer` de Windows, funciona sin internet) + nombres de los músculos por fase.
- [ ] Fase 6: cadera (flexión/extensión, abducción/aducción, rotaciones).
