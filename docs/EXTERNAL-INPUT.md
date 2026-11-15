# Ratón y teclado externos (rama experimental)

Este código **todavía no está compilado ni instalado**. La Xbox ejecuta el APPX original v0.1.0. El framework adaptado que exige la compilación no está incluido en el repositorio público ni en su historial.

## Ratón

Se ha añadido `mouse` como modo optativo en `LocalState/pointer.txt`. Tras compilar esta rama, escribir `mouse` en ese archivo y reiniciar la aplicación:

- Desactiva el cursor emulado por el mando.
- Lee desplazamientos relativos con `Windows.Devices.Input.MouseDevice.MouseMoved` y limita el cursor al tamaño lógico de la superficie.
- Los botones y ambas ruedas usan eventos CoreWindow; no se combinan con el antiguo duplicado de eventos XAML y su filtro temporal de 500 ms.
- Conserva la posición relativa al pulsar, evitando saltar a la posición obsoleta del cursor del sistema.
- Registra `raw mouse moves` para distinguir eventos físicos recibidos de una mera suscripción correcta.

Esto usa la API pública; no concede permisos adicionales ni garantiza que Xbox entregue eventos del dispositivo. El soporte USB real requiere probar la compilación en consola. `hybrid` sigue siendo el valor predeterminado; para volver al mando, restaurar ese valor y reiniciar.

Referencia de la API: https://learn.microsoft.com/en-us/uwp/api/windows.devices.input.mousedevice

Referencia de modos de ratón UWP de Microsoft: https://github.com/microsoft/Xbox-ATG-Samples/tree/main/UWPSamples/System/MouseCursor

## Teclado

- Las transiciones de cada tecla física se envían una sola vez, conservando los modificadores izquierdo y derecho.
- Los códigos virtuales del mando no se reenvían como teclado, porque el framework ya procesa el mando.
- Al perder foco o suspenderse, se liberan las teclas y botones físicos que seguían pulsados para evitar entradas bloqueadas.
- Se mantiene el flujo existente de texto Unicode y teclado en pantalla, independiente de las teclas de juego.

## Validación

`dotnet run --project tests/ImportInbox.Tests`: 17 comprobaciones superadas (9 de importación y 8 de estado de entrada). Estas pruebas verifican lógica independiente de UWP; no acreditan recepción de eventos USB ni compilación completa de MainPage.

Pruebas pendientes en una compilación completa: movimiento USB, clic izquierdo/derecho/central, rueda vertical/horizontal, Z/X mantenidas, modificadores, escritura con teclado español, abrir y cerrar la guía Xbox mientras se mantiene una tecla, desconectar/reconectar dispositivos y alternar con el mando.

No se debe etiquetar el soporte externo como funcional hasta completar esas pruebas. Hace falta obtener primero el osu!framework modificado del autor; `check-build.ps1` comprueba su presencia.
