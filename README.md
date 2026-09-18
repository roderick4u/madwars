# Android Warlocks — prototipo low poly

Personaje de warlock modular, animaciones y efectos de fuego para un prototipo de MOBA móvil en Unity. Proyecto en desarrollo; todavía no es un juego completo ni está perfilado en un dispositivo móvil.

## Abrir el proyecto

1. Instala Git LFS y ejecuta `git lfs install` antes de clonar. Si ya clonaste, ejecuta `git lfs pull`.
2. En Unity Hub, añade **`UnityProject/`**, no la raíz del repositorio.
3. Usa **Unity 6000.6.0f1**. Espera la importación de assets y la restauración de los paquetes declarados en `Packages/manifest.json` y `packages-lock.json`.
4. Abre `Assets/WarlockDemo/Scenes/WarlockArena.unity` y pulsa Play.

La escena predeterminada de Build Settings de la plantilla puede seguir siendo SampleScene. Para crear un build, selecciona WarlockArena como escena inicial. La importación necesita acceso al registro de paquetes de Unity; algunas herramientas del Editor incluidas son preliminares y no son necesarias para el gameplay.

## Controles del demo

- **WASD:** mover en relación con la cámara; correr por defecto.
- **Shift:** caminar.
- **Clic derecho en el plano:** mover al punto seleccionado.
- **Espacio:** activar/desactivar el recorrido automático.

La cámara es isométrica. La fireball realiza vuelos repetidos de demostración: no representa todavía un ataque activable por el jugador.

## Contenido y estado

| Elemento | Estado |
| --- | --- |
| Cabeza, torso, brazos, piernas y ensamblado | Fuentes editables y exportaciones en `output/` |
| Idle, caminar y correr | Integrados en Unity |
| Capa independiente | Unity Cloth, 384 triángulos, 221 vértices y cápsulas de colisión |
| Bastón | 868 triángulos, agarre ajustado y balanceo según la mano derecha |
| Fireball | Malla de 140 triángulos, toon y deformación de vértices en GPU |
| Estela y chispas | Cubos/tetraedros, máximo combinado de 64 partículas, reducción lineal y giro XYZ |
| Acabado del fuego | Trail Renderer, luz puntual sin sombras, emisión HDR y Volume con Bloom |
| Casteo | Vista previa Blender y FBX; lanzamiento a 0,5 s, clip total de 1 s con recuperación |

**Pendiente:** conectar el clip de casteo al Animator y al lanzamiento real en la dirección del jugador; añadir colisiones, daño, controles táctiles y pruebas de rendimiento móvil. La física de la capa funciona en Unity; las fuentes antiguas de Blender conservan sus distintas etapas de desarrollo.

## Estructura

```text
UnityProject/    Assets, Packages y ProjectSettings del demo guardado
output/          Modelos Blender, FBX/GLB, texturas y validaciones
*.py             Scripts de modelado, animación y comprobación en Blender
tools/           Copia portable del proyecto y auditoría del repositorio
docs/            Guía de publicación y notas de producción
```

El código C# y los shaders que se deben editar están en `UnityProject/Assets/WarlockDemo/`. Los scripts `Editor/Setup*.cs` son herramientas de construcción: no hace falta ejecutarlos para abrir la escena. Algunos reconstruyen assets o escenas; haz commit antes de usarlos. Las copias y fragmentos de instalación antiguos de la raíz están excluidos de Git.

## Fuentes Blender

Usa **Blender 5.2.2**. Los archivos `.blend` contienen texturas empaquetadas.

- Ensamblado: `output/assembled_v2/Warlock_Complete.blend`
- Idle: `output/idle_cycle/Warlock_Idle.blend`
- Caminata: `output/walk_natural/Warlock_Walk.blend`
- Carrera: `output/run_dynamic/Warlock_Run.blend`
- Bastón: `output/staff/Warlock_Staff.blend`
- Casteo: `output/cast_fireball/Warlock_Cast_Preview.blend`

En el casteo, el marcador **RELEASE_FIREBALL** está en el fotograma **16**, con inicio en el 1 y **30 FPS**: `(16 − 1) / 30 = 0,5 s`. El objeto `PREVIEW_ONLY_Fireball` es una ayuda visual y no se exporta como gameplay. Los archivos `timing.json` y `validation.json` documentan la temporización y las comprobaciones de pies, raíz y agarre.

Para ejecutar un script de Blender desde la raíz:

```sh
blender --background --factory-startup --python animate_cast.py
```

Los scripts usan `bpy` y el NumPy incluido con Blender. Conserva la estructura de carpetas: los scripts posteriores dependen de los modelos de etapas anteriores. Renderizar previsualizaciones puede tardar. Pillow solo es necesario para generar GIFs fuera de Blender.

![Vista previa del casteo](output/cast_fireball/Cast_preview.gif)

## Git y publicación

Modelos, imágenes y animaciones binarias se almacenan mediante **Git LFS**. Los `.meta` de Unity se versionan: no los borres ni los regeneres al mover assets. Cachés, backups `.blend1`, secuencias de render, credenciales y configuración local están excluidos.

Consulta [docs/PUBLISHING.md](docs/PUBLISHING.md) para crear el primer commit y publicar. No se ha elegido una licencia de distribución ni incluido material de referencia descargado de terceros.
