# Cambios del fork de JLiveOf

Base: mareczek1113/osu-xbox, rama main. Se conservan sus créditos y licencia.

## Estado real

Se ha instalado y arrancado en la Xbox Series S la versión pública **v0.1.0**, sin modificar. SHA-256 del APPX: `f65b077aadd359f7be4beacf4932fb45e7f35bf10247ac4791191151ffbc838d`.

Las mejoras siguientes son código fuente probado por separado. **No están en el APPX instalado**. No se ha podido compilar el juego completo: el repositorio público omite el osu!framework modificado que referencia `osu.Game.csproj`, además de los scripts de empaquetado citados en BUILDING.md. Ese fork contiene las clases UwpHostBridge, UwpGamepadHandler y el renderer adaptado; el framework oficial no es un reemplazo directo.

## Importaciones más fiables

- Dos observaciones consecutivas con tamaño y fecha iguales, antigüedad mínima y archivo desbloqueado antes de importar.
- Importación individual: un archivo defectuoso no invalida el resto del lote.
- Archivos fallidos conservados en `import/failed`, con un `.error.txt`; no se reintentan indefinidamente. Para reintentar, devolver el archivo a `import`.
- Archivos que el importador no consume conservados en `import/done`. Nombres repetidos reciben un sufijo único: no se borra la copia anterior.
- Exclusión mutua entre exploraciones para evitar importaciones duplicadas.

Pruebas sin dependencias externas:

```powershell
dotnet run --project tests/ImportInbox.Tests
```

Se han superado 17 comprobaciones: 9 del importador y 8 de estado de entrada. El detalle de las mejoras experimentales de ratón y teclado está en [EXTERNAL-INPUT.md](EXTERNAL-INPUT.md).

## Compilación

La ruta del framework se puede pasar como propiedad MSBuild `XboxFrameworkProject`. `check-build.ps1` detecta primero la dependencia ausente y explica el bloqueo.

```powershell
./check-build.ps1 -XboxFrameworkProject C:/ruta/al/fork/osu.Framework/osu.Framework.csproj
```

## Consola

En Dev Home, clasificar osu! como **Game** para ampliar el presupuesto de recursos. Se ha verificado el cambio de 1024 MB a **5120 MB** tras reiniciar el APPX original. No se han verificado partidas completas ni latencia o efectos de audio; la interfaz informa de que el backend SDL aún no admite efectos de audio.

Para cargar mapas: Device Portal > File explorer > LocalAppData > osu! > LocalState > import. Subir archivos .osz; las skins usan .osk. Los cambios de cuarentena descritos arriba solo aplicarán después de compilar e instalar esta versión modificada.

## Publicación

Fork creado: https://github.com/JLiveOf/osu-xbox . Las mejoras se publican en la rama `xbox-external-input`. No se incluyen certificados privados, credenciales de Device Portal, IP de la consola ni registros personales.
