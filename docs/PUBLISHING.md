# Publicar en GitHub

El repositorio se prepara en esta carpeta. `UnityProject/` es una copia del proyecto de trabajo; los cambios futuros hechos en otra ubicación no se sincronizan automáticamente.

## Antes del primer push

```sh
git lfs install --local
python tools/audit_repo.py
git status --short
git add .
git diff --cached --stat
git lfs status
git commit -m "Initial warlock prototype: models, animations and Unity demo"
```

Revisa el contenido preparado antes del commit. Si Git pide identidad, configura tu propio `user.name` y `user.email`; no uses una identidad inventada. Decide si el repositorio será privado o público y qué licencia quieres aplicar.

En GitHub, crea un repositorio **vacío**, sin README, `.gitignore` ni licencia inicial. Después:

```sh
git remote add origin https://github.com/TU_USUARIO/TU_REPOSITORIO.git
git push -u origin HEAD
```

Sustituye los valores de ejemplo por los tuyos. No pegues tokens dentro de la URL: usa el gestor de credenciales o GitHub CLI. Git LFS sube los binarios durante el push; revisa el almacenamiento y ancho de banda de tu cuenta antes de publicarlo.

## Actualizar la copia Unity

Guarda el proyecto de trabajo y sal de Play Mode antes de copiarlo:

```sh
python tools/snapshot_unity.py "RUTA_AL_PROYECTO_UNITY"
python tools/audit_repo.py
git diff --stat
```

La herramienta copia `Assets`, `Packages` y `ProjectSettings`, conservando `.meta`. No elimina archivos antiguos del destino: revisa manualmente las eliminaciones o renombrados que hayas hecho en el proyecto original. No incluye `Library`, `Logs`, `UserSettings`, capturas ni recuperaciones automáticas. Omite asociaciones del proyecto con la organización/cuenta de Unity en la copia.

## Validación

La auditoría comprueba estructura, archivos excesivamente grandes fuera de LFS, configuración de paquetes, rutas absolutas en código y archivos `.meta` ausentes. Es una comprobación estática: no sustituye abrir el proyecto clonado, dejar que Unity importe los paquetes y probar WarlockArena en Play Mode. Todavía no hay build Android validado.
