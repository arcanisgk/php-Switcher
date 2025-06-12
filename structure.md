# PHP Switcher C# Project Structure

Este documento describe la estructura de archivos y directorios del proyecto PHP Switcher en su versión C#.

## Estructura de la Aplicación

La aplicación PHP Switcher en C# consiste en una ventana de carga (SplashForm) que realiza tareas de inicialización y una ventana principal (MainForm) con una interfaz de pestañas para gestionar las versiones de PHP.

## Archivos Principales

| Archivo | Descripción |
|---------|-------------|
| `Program.cs` | Punto de entrada principal de la aplicación C#. Inicializa la aplicación, verifica privilegios de administrador y lanza la ventana de carga. |
| `Forms/SplashForm.cs` | Ventana de carga que realiza tareas de inicialización en tiempo real antes de lanzar la ventana principal. |
| `Forms/MainForm.cs` | Ventana principal que proporciona una interfaz de usuario con pestañas para gestionar versiones de PHP. |

## Estructura de Directorios

```
php-Switcher/
├── Forms/                      # Formularios de la interfaz de usuario
│   ├── SplashForm.cs           # Ventana de carga inicial
│   └── MainForm.cs             # Ventana principal con pestañas
├── Models/                     # Modelos de datos
│   └── AppConfig.cs            # Configuración de la aplicación y modelos de versiones PHP
├── Services/                   # Servicios de la aplicación
│   ├── ConfigService.cs        # Servicio para gestionar la configuración
│   ├── LogService.cs           # Servicio de registro de eventos
│   └── PhpVersionService.cs    # Servicio para gestionar versiones de PHP
├── Utils/                      # Utilidades
│   └── AdminUtils.cs           # Utilidades para verificar y solicitar privilegios de administrador
├── Resources/                  # Recursos de la aplicación
│   └── AppResources.cs         # Clase para gestionar recursos como iconos
├── Program.cs                  # Punto de entrada de la aplicación
├── PhpSwitcher.csproj          # Archivo de proyecto de .NET
└── README.md                   # Documentación del proyecto
```

## Estructura de la Ventana de Carga (SplashForm)

La ventana de carga en `SplashForm.cs` es responsable de inicializar la aplicación y realizar varias tareas importantes:

1. **Componentes de UI**:
   - Etiquetas de título y subtítulo
   - Barra de progreso que muestra el progreso de inicialización
   - Etiqueta de estado que muestra la operación actual
   - Etiqueta de versión que muestra la versión de la aplicación

2. **Proceso de Inicialización**:
   - Paso 1 (20%): Verificar privilegios de administrador
   - Paso 2 (40%): Comprobar configuración de primera ejecución y configurar directorio PHP
   - Paso 3 (50%): Sincronizar versiones PHP instaladas con la configuración
   - Paso 4 (60%): Obtener versiones disponibles de PHP desde windows.php.net
   - Paso 5 (100%): Completar inicialización y lanzar aplicación principal

3. **Manejo de Errores**:
   - Continúa la inicialización incluso si ciertos pasos fallan
   - Proporciona mensajes de error detallados en la consola
   - Muestra retroalimentación apropiada al usuario
   - Implementa manejo robusto de errores para operaciones de enlaces simbólicos

## Estructura de la Ventana Principal (MainForm)

La ventana principal en `MainForm.cs` proporciona una interfaz de usuario con pestañas para gestionar versiones de PHP:

1. **Pestañas**:
   - **Installed Versions**: Muestra las versiones de PHP instaladas y permite activarlas o eliminarlas.
   - **Available Versions**: Muestra las versiones de PHP disponibles para descargar e instalar.
   - **Settings**: Permite configurar la aplicación.

2. **Barra de Estado**:
   - Muestra la versión de PHP activa
   - Muestra el estado actual de la aplicación
   - Muestra una barra de progreso durante operaciones como descargas

## Almacenamiento de Configuración

La aplicación almacena su configuración y recursos en los directorios AppData del usuario:

```
%APPDATA%\PHPSwitcher\
└── config.json         # Archivo de configuración que almacena preferencias de usuario e información de versiones PHP
```

## Directorio de Versiones PHP

Por defecto, las versiones de PHP se almacenan en:

```
C:\php-versions\        # Directorio predeterminado para instalaciones PHP
```

Este directorio puede personalizarse durante la primera ejecución o más tarde a través de la pestaña Configuración.

## Modos de Operación

La aplicación admite tres modos de operación diferentes:

1. **Modo Normal** (predeterminado):
   - Ventana de consola oculta para una experiencia de usuario más limpia
   - Registro mínimo para mejorar el rendimiento
   - Adecuado para uso diario

2. **Modo Debug**:
   - Ventana de consola visible para mostrar registros detallados
   - Activado con el parámetro `-c` o `--show-console`
   - Útil para solucionar problemas
   - Accesible a través del botón "Restart in Debug Mode" en la pestaña Settings

3. **Modo Desarrollo**:
   - Registro mejorado con información adicional específica de desarrollo
   - Ventana de consola siempre visible
   - Indicador visual en la UI que muestra "DEVELOPMENT MODE ACTIVE"
   - Activado con el parámetro `-d` o `--dev`
   - Habilita automáticamente las características del modo debug
   - Accesible a través del botón "Restart in Development Mode" en la pestaña Settings

## Notas Adicionales

- El proyecto está diseñado para funcionar en entornos Windows.
- La aplicación requiere privilegios de administrador para modificar variables de entorno del sistema y configuraciones PHP.
- La aplicación utiliza Windows Forms para la interfaz gráfica de usuario con un diseño de pestañas.
- La aplicación incluye registro de consola adecuado para mejor trazabilidad durante la ejecución.
- La aplicación está diseñada para ser fácil de usar con retroalimentación visual clara.
- La configuración se persiste entre sesiones en un archivo JSON.
- La aplicación puede detectar y listar versiones PHP disponibles desde windows.php.net con experiencia de usuario mejorada, incluyendo indicadores visuales de carga y retroalimentación inmediata.
- Los elementos de UI se sincronizan automáticamente al realizar operaciones, eliminando la necesidad de actualización manual en la mayoría de los casos.