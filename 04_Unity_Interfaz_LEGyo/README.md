# Interfaz LEGyo — avance del 19 de noviembre de 2026

Esta carpeta contiene una copia independiente del proyecto `LEGyo_MiembroInferior` con los avances de interfaz, asistente y navegación por voz. Se añade sin modificar los archivos ni el proyecto que ya estaban en el repositorio.

## Abrir el proyecto

1. Descarga o clona el repositorio completo.
2. En Unity Hub, añade esta carpeta: `04_Unity_Interfaz_LEGyo`.
3. Abre con **Unity 2022.3.62f3**.
4. Abre `Assets/MenuPrincipal.unity` y pulsa **Play**.
5. Espera a que termine el robot y di **«Continuar»** cuando aparezca el estado de escucha.

## Avances incluidos

- Interfaz con panel transparente flotante, fondo oscuro y fuente Exo 2.
- Logo biomecánico LEGyo.
- Robot con parpadeo, movimiento de cabeza y boca sincronizada con la narración en español.
- Mensaje de bienvenida en una burbuja de diálogo.
- Navegación por voz local en español mediante Vosk. La primera pantalla no tiene botón para avanzar.
- Segunda pantalla de calibración para Kinect v2, con lector de cámara a color, guía de encuadre y seguimiento de caderas, rodillas, tobillos y pies.

## Requisitos y estado de prueba

- Windows de 64 bits y micrófono habilitado para Unity.
- El modelo de reconocimiento en español se incluye en `Assets/StreamingAssets/vosk-model-small-es-0.42`; no omitir esa carpeta.
- Para la cámara y la calibración: Kinect v2, alimentación, USB 3.0 y Kinect for Windows SDK/Runtime v2 instalado.
- El comando «Continuar» se probó y cambió a la segunda pantalla.
- La cámara y la calibración corporal todavía requieren prueba con el Kinect conectado. Durante la preparación, el sensor figuraba sin conexión.

Se incluyen `Assets`, sus archivos `.meta`, `Packages` y `ProjectSettings`. Unity regenerará `Library` y los demás archivos temporales al abrirlo. Las dependencias y sus avisos de licencia permanecen dentro del proyecto.
