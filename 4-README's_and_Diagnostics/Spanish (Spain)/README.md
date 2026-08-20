# PoE2 Route AutoSplitter

Una herramienta de configuración y autosplitter de LiveSplit para **speedruns de la campaña de Path of Exile 2**.

Versión actual: **v3.0.0 Release Candidate**.

PoE2 Route AutoSplitter ofrece rutas predefinidas y personalizadas para:

* Exploración / finalización de zonas
* Boss Rush
* Exploración + Boss Rush combinados
* Campaña Any%
* Campaña 100%
* Solo jefes obligatorios de campaña
* Jefes Pinnacle 0.5
* Temple of Chaos
* Trial of the Sekhemas
* Rutas personalizadas definidas por el usuario
* Mapas

La aplicación **PoE2RouteSetup** incluida se encarga de la mayor parte de la configuración.

Permite pausar de forma sincronizada el juego y el temporizador de LiveSplit al abrir el menú de pausa.
La opción Game Time de LiveSplit excluye los tiempos de carga y pausa el temporizador cuando la opción correspondiente está activa.

Capturas de pantalla: https://imgur.com/a/VgiRn6o

---
# Políticas de carrera

He intentado que la herramienta sea lo más independiente posible de un reglamento concreto. Los jugadores tienen bastante libertad para decidir cómo gestionar las reglas de su carrera y qué desencadenadores quieren utilizar.

Para los comienzos nuevos en Riverbank, el breve periodo entre despertarse y hablar con The Wounded Man no se cronometra de forma intencionada. Esto da tiempo para corregir ajustes, seleccionar «skip tutorial» o cambiar cualquier otra opción antes de empezar realmente la carrera. Después de interactuar con The Wounded Man, el tiempo comienza en su última línea de diálogo inicial.

Los inicios por transición de zona se activan en cuanto el personaje entra en la zona predefinida. En carreras dinámicas, el temporizador solo empieza y el seguimiento solo se activa cuando el personaje entra en esa zona concreta, aunque la carrera haya comenzado en otra zona.

Debido a la duración del juego, he desarrollado GameTimeWatcher, un programa sencillo que indica a LiveSplit que pause su Game Time mientras estén abiertos el menú Pause Game o el menú de microtransacciones. Esto permite hacer descansos o atender situaciones que requieran toda la atención del jugador. Los demás menús no pausan el temporizador porque el personaje sigue siendo controlable. El temporizador también continúa durante las cinemáticas del juego, ya que el inventario sigue disponible y puede utilizarse para optimizar la carrera. Actualmente, el temporizador solo se pausa durante las pantallas de carga, el menú de pausa y la tienda de microtransacciones.

---

# Descarga

La descarga puede encontrarse [aquí](https://github.com/ScottHoppe414/PoE2RouteAutoSplitter/tags)

O

Ve a la sección **Releases** de este repositorio de GitHub y descarga la versión más reciente:

**`PoE2RouteAutoSplitter-vX.X.X-Setup.exe`**

Para la mayoría de los usuarios, el instalador es el método recomendado.

También puede estar disponible un ZIP portátil para quienes prefieran no usar el instalador. En ese caso será necesario usar PowerShell para ejecutar `\Setup-UI[Configuration]\Build.ps1` y generar `RouteSetup.exe`.

---

# Inicio rápido

## 1. Instalar PoE2 Route AutoSplitter

Ejecuta:

`PoE2RouteAutoSplitter-vX.X.X-Setup.exe`

Sigue las instrucciones de instalación.

Después de instalarlo, abre:

**PoE2 Route AutoSplitter**

Esto inicia la aplicación de configuración de rutas.

---

## 2. Elegir una ruta

La aplicación Setup ofrece una lista de rutas predefinidas.

Selecciona la ruta que quieras utilizar.

Algunos ejemplos:

* Campaña Any%
* Campaña 100%
* Solo jefes obligatorios
* Rutas de exploración
* Rutas Boss Rush
* Rutas combinadas de Exploración + Boss Rush

También puedes seleccionar **Custom Route** para crear tu propia ruta.

---

## 3. Generar la configuración de LiveSplit

Después de seleccionar la ruta, pulsa el botón Generate.

La aplicación creará los archivos necesarios dentro del directorio:

`LiveSplit Target`

Esta carpeta contiene los archivos que LiveSplit necesita para la ruta seleccionada.

El contenido de **LiveSplit Target** se sustituye cada vez que generas una configuración nueva.

---

# Configuración de LiveSplit

Hay que configurar dos elementos en LiveSplit:

1. El archivo de splits (`.lss`)
2. El Scriptable Auto Splitter (`.asl`)

## Cargar el archivo de splits

Dentro de la carpeta **LiveSplit Target** generada, localiza el archivo `.lss`.

Ábrelo con LiveSplit.

También puedes cargarlo manualmente desde LiveSplit mediante:

**File → Open Splits → From File**

Selecciona el archivo `.lss` generado.

---

## Añadir el Scriptable Auto Splitter

El script del autosplitter debe añadirse manualmente al diseño de LiveSplit.

En LiveSplit:

1. Haz clic derecho en LiveSplit.
2. Selecciona **Edit Layout**.
3. Pulsa el botón **+**.
4. Selecciona:

   **Control → Scriptable Auto Splitter**

5. Selecciona el nuevo componente **Scriptable Auto Splitter**.
6. Busca el archivo `.asl` dentro de tu carpeta **LiveSplit Target**.
7. Guarda el diseño.

Solo tendrás que cambiar esta ruta si mueves los archivos generados o cambias a una configuración que utilice otro archivo ASL.

> PoE2 Route AutoSplitter **no** genera ni sustituye tu diseño de LiveSplit.

Tu diseño sigue estando bajo tu control.

---

# Configuración de Boss Rush

Las rutas que siguen jefes utilizan el programa **BossWatcher** incluido.

BossWatcher lee los nombres de los jefes en el juego y envía sus eventos al autosplitter.

Si la ruta seleccionada necesita BossWatcher, utiliza el botón:

**Start BossWatcher**

dentro de PoE2 Route Setup.

Aparecerá una ventana de consola.

Durante el uso normal, BossWatcher solo muestra eventos útiles, como:

* Jefe encontrado
* Jefe derrotado
* Duración del combate

Ejemplo:

`[21:42:18] Encountered: The Executioner`

`[21:43:07] Defeated: The Executioner | Fight time: 49.213 s`

No necesitas interactuar con la consola de BossWatcher durante la carrera.

Déjala abierta mientras haces el speedrun.

---

# Rutas de exploración

Las rutas de exploración detectan cuándo el personaje entra en zonas concretas de Path of Exile 2.

BossWatcher **no es necesario** para rutas exclusivamente de exploración.

El autosplitter lee automáticamente la información de transición entre zonas de Path of Exile 2.

---

# Exploración + Boss Rush combinados

Las rutas combinadas registran tanto:

* Finalización de zonas
* Derrotas de jefes

Para estas rutas:

1. Carga el `.lss` generado.
2. Haz que Scriptable Auto Splitter apunte al `.asl` generado.
3. Inicia BossWatcher desde PoE2 Route Setup.
4. Comienza la carrera.

Los objetivos de zonas y de jefes serán gestionados por la misma ruta.

---

# Rutas personalizadas

Selecciona **Custom Route** en PoE2 Route Setup para crear tu propia ruta.

Puedes incluir:

* Zonas
* Jefes
* Zonas y jefes

Añade los objetivos que quieras y ordénalos según tus preferencias.

Cuando termines, genera la configuración.

La aplicación creará los siguientes archivos personalizados dentro de **LiveSplit Target**:

* `.lss`
* `.asl`
* Configuración de ruta

Carga estos archivos siguiendo las mismas instrucciones de LiveSplit indicadas anteriormente.

---

# Pruebas

Diseñado para Trial of the Sekhemas y Temple of Chaos.

La condición de inicio se cumple al entrar por primera vez en la prueba propiamente dicha. El vestíbulo en el que realizas la preparación no se registra.

Hay dos condiciones de finalización:

1. Seleccionas hasta qué profundidad de la prueba quieres llegar y, al derrotar al jefe de esa profundidad, la prueba finaliza correctamente. No completar la prueba se considera una carrera fallida y requiere un reinicio manual.

2. Salir de la prueba la marca como completada. Esta opción está pensada para quienes quieran considerar la salida de la arena como condición de finalización. En ese caso, recoger botín, abrir alijos, visitar al mercader y seleccionar la Ascendencia forman parte de la carrera.

---

# Ruinas Vaal

El vestíbulo se considera una zona límite por motivos de transición. Esto significa que entrar en la sala de la consola desde un mapa se trata como salir del mapa y no como entrar en una subzona del mismo.

Las Ruinas Vaal siguen en desarrollo.

---

# Mapas

La preparación de un mapa no se cronometra mientras el jugador está en un escondite u otro tipo de centro de mapas. Al entrar en el mapa, el temporizador se inicia automáticamente y se realiza un split en la primera salida después de derrotar al jefe de la zona. Si se sale del mapa antes de derrotar al jefe, el temporizador continúa. Esto permite ir rápidamente a por el jefe, derrotarlo, salir y volver a entrar en el mismo mapa para hacer contenido adicional con el temporizador pausado. (Consulta la política alternativa más abajo.)

Las carreras de mapas tienen varias condiciones de finalización:

* Número fijo de mapas
* Hasta la primera muerte (carrera sin muertes)
* Finalización manual
* Derrotar a un jefe Pinnacle específico

También puedes activar el seguimiento de muertes con tres opciones:
* Sin seguimiento de muertes
* Solo la primera muerte
* Registrar muertes

Al seleccionar la primera muerte o el registro de muertes, tendrás que introducir el nombre de tu personaje exactamente como aparece en el juego. El programa lee los registros del cliente para identificar la muerte del personaje.

Hay dos políticas de pausa:

* La derrota de un jefe se utiliza como evento de finalización del mapa y el split termina en la primera salida después de derrotarlo. Es similar a la política de finalización de mapas de PoE2.
* Política alternativa: el temporizador solo se pausa en pantallas de carga, durante una pausa manual o en el menú de microtransacciones (si está activado). El resto del tiempo sigue corriendo, incluida la preparación del mapa, la gestión del inventario y la revisión del botín.

# Cambiar de ruta

Para cambiar a otra ruta:

1. Abre PoE2 Route Setup.
2. Selecciona la nueva ruta.
3. Genera la configuración de nuevo.
4. Abre el nuevo `.lss` en LiveSplit.
5. Comprueba que Scriptable Auto Splitter apunta al `.asl` dentro de **LiveSplit Target**.
6. Inicia BossWatcher si la nueva ruta requiere detección de jefes.

Se sustituirá el contenido anterior de **LiveSplit Target**.

---

# Comenzar una carrera

Una vez terminada la configuración:

1. Abre Path of Exile 2.
2. Abre LiveSplit.
3. Carga el `.lss` de tu ruta.
4. Comprueba que el componente Scriptable Auto Splitter usa el `.asl` correcto.
5. Inicia BossWatcher si la ruta incluye jefes.
6. Comienza la carrera.

El autosplitter gestionará automáticamente los objetivos de ruta configurados.

---

# Actualización

Cuando se publique una versión más reciente:

1. Descarga el instalador más reciente desde **GitHub Releases**.
2. Ejecuta el instalador.
3. Abre PoE2 Route Setup.
4. Genera de nuevo tu ruta.

No es necesario sustituir tu diseño personal de LiveSplit.

---

# Solución de problemas

## Los jefes no generan splits

Comprueba que:

* BossWatcher está en ejecución.
* Has iniciado BossWatcher desde PoE2 Route Setup.
* La ruta seleccionada contiene objetivos de jefes.
* El Scriptable Auto Splitter de LiveSplit apunta al `.asl` generado.

---

## Las zonas no generan splits

Comprueba que:

* Path of Exile 2 está en ejecución.
* El Scriptable Auto Splitter de LiveSplit apunta al `.asl` correcto.
* Has generado la ruta de exploración correcta.
* Está cargado el `.lss` correcto.

---

## LiveSplit abre unos splits incorrectos

Abre el `.lss` directamente desde:

`LiveSplit Target`

o utiliza:

**File → Open Splits → From File**

---

## He cambiado de ruta y algo ha dejado de funcionar

Genera de nuevo la nueva ruta y comprueba:

* Está cargado el `.lss` correcto.
* Scriptable Auto Splitter apunta al `.asl` actual dentro de **LiveSplit Target**.

---

## BossWatcher muestra un error

Cierra BossWatcher y vuelve a iniciarlo mediante el botón **Start BossWatcher** de PoE2 Route Setup.

Si el problema continúa, incluye el error mostrado al informar del problema.

---
## BossWatcher hace un split prematuro o al morir el jugador

BossWatcher registra cuándo la barra de vida del jefe desaparece de la pantalla. Esto puede ocurrir por distintos motivos. Corresponde al usuario determinar si el split es correcto. El programa asume que el jefe ha muerto y realiza el split. Si el split ocurre sin haber completado el jefe, deshacerlo devuelve LiveSplit al estado anterior y permite volver a intentar el jefe con el tiempo actual. El atajo para deshacer un split se encuentra en los ajustes de LiveSplit.

---

# Archivos generados para LiveSplit

Según la ruta seleccionada, **LiveSplit Target** puede contener:

### `.lss`

La lista de splits de LiveSplit.

### `.asl`

El script del autosplitter utilizado por el componente Scriptable Auto Splitter de LiveSplit.

### Archivos de ruta/configuración

Indican al autosplitter qué zonas y/o jefes pertenecen a la ruta seleccionada.

### Archivos de eventos de jefes

Utilizados por BossWatcher y los autosplitters que incluyen jefes.

No edites estos archivos manualmente salvo que sepas exactamente qué estás cambiando.

Para el uso normal, genéralos mediante **PoE2 Route Setup**.

---

# Importante

PoE2 Route AutoSplitter **no** controla ni sustituye tu diseño personal de LiveSplit.

Tú eres responsable de:

* Apariencia del temporizador
* Colores de los splits
* Fuentes
* Tamaño de la ventana
* Ajustes de comparación
* Otros componentes de LiveSplit

PoE2 Route AutoSplitter solo proporciona los splits de la ruta y la configuración del autosplitter.

---

# Informar de problemas

Al informar de un problema, incluye:

* Versión de PoE2 Route AutoSplitter
* Ruta/modo utilizado
* Si BossWatcher estaba en ejecución
* Qué esperabas que ocurriera
* Qué ocurrió realmente
* Cualquier mensaje de error mostrado por PoE2 Route Setup, BossWatcher o LiveSplit

Esto hace que los problemas sean mucho más fáciles de reproducir y corregir.

---

# Verificación del paquete y diagnósticos

Los manifiestos SHA-256 usados para verificar los archivos de la versión o del runtime se almacenan en:

`3 - verification files`

También se guardan allí los manifiestos de validación de la configuración, los manifiestos SHA-256 de cada run, los registros de auditoría y los resúmenes legibles de los runs. Se mantienen fuera de `LiveSplit Target` para que generar una ruta nueva no elimine los archivos de auditoría de runs anteriores.

Los registros de diagnóstico de SetupUI, BossWatcher y GameTimeWatcher se centralizan en:

`4-README's_and_Diagnostics\Diagnostics`

Las capturas PNG de diagnóstico se almacenan en:

`4-README's_and_Diagnostics\Diagnostics\images`

---

# Versión principal actual

**PoE2 Route AutoSplitter 3.x**

La versión 3 añade compatibilidad multilingüe para SetupUI y el idioma del juego, nombres localizados y verificados de jefes y zonas cuando están disponibles, políticas ampliadas para Campaña, Pruebas, Ruinas Vaal y Mapas, diagnósticos y archivos de verificación centralizados y una geometría de captura adaptativa de BossWatcher basada en la altura para clientes de juego 16:9, ultrapanorámicos y superultrapanorámicos.
