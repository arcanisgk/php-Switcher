# Registro de Cambios (Changelog) - PHP Switcher C#

Todas las modificaciones notables en el proyecto PHP Switcher C# serán documentadas en este archivo.

## [1.0.1] - 2024-06-12

### Añadido
- Sincronización automática de versiones PHP instaladas con la configuración durante el inicio
- Mejora en la visualización de versiones instaladas con ordenación por versión y tipo
- Mejora en la visualización de versiones disponibles con agrupación por versión mayor/menor
- Información de tooltip para versiones instaladas y disponibles
- Contador de versiones instaladas y disponibles en las pestañas correspondientes
- Método `GetActiveVersion()` en la clase AppConfig para obtener la versión activa
- Implementación mejorada de iconos de aplicación y logo en la interfaz de usuario

### Cambiado
- Mejora en el manejo de nulabilidad en todo el código para reducir advertencias de compilación
- Mejora en la visualización de versiones instaladas y disponibles con indicadores visuales
- Proceso de inicialización actualizado para incluir sincronización de versiones instaladas
- Reposicionamiento del logo PHP en el splash screen para evitar solapamiento con el título
- Optimización del tamaño del logo PHP en el splash screen para mejor visualización

### Corregido
- Problema de sincronización entre versiones PHP instaladas en el sistema de archivos y la configuración
- Advertencias de nulabilidad en la compilación
- Manejo mejorado de referencias potencialmente nulas en el código
- Carga correcta del icono de la aplicación en todas las ventanas y en la barra de tareas
- Visualización mejorada del logo PHP en el splash screen

## [1.0.0] - 2024-06-01

### Añadido
- Migración completa del script PowerShell original a una aplicación C# con Windows Forms
- Implementación de la ventana de carga (SplashForm) con barra de progreso y mensajes de estado
- Implementación de la ventana principal (MainForm) con interfaz de pestañas
- Funcionalidad para verificar y solicitar privilegios de administrador
- Configuración de primera ejecución para establecer el directorio de versiones PHP
- Detección y listado de versiones PHP disponibles desde windows.php.net
- Descarga e instalación de versiones PHP seleccionadas
- Activación de versiones PHP mediante enlaces simbólicos
- Gestión de versiones PHP instaladas (activar, eliminar)
- Barra de estado con información en tiempo real
- Modo consola oculta para experiencia de usuario más limpia
- Modo desarrollo para depuración avanzada
- Sincronización mejorada entre elementos UI al realizar operaciones
- Almacenamiento persistente de configuración en archivo JSON
- Estructura de proyecto modular con separación de responsabilidades (Models, Services, Utils)

### Cambiado
- Reescritura completa del código en C# manteniendo la funcionalidad del script PowerShell original
- Mejora en la interfaz de usuario con controles Windows Forms nativos
- Optimización del proceso de descarga e instalación de versiones PHP

### Corregido
- Manejo mejorado de errores durante operaciones críticas
- Verificación más robusta de privilegios de administrador
- Mejor gestión de enlaces simbólicos para activación de versiones PHP

## [1.0.2] - 2024-06-13

### Añadido
- Generación de ejecutable independiente (self-contained) que incluye todas las dependencias de .NET
- Creación de instalador MSI utilizando WiX Toolset
- Documentación actualizada con instrucciones para compilación y distribución
- Soporte mejorado para versiones de PHP anteriores a 7.4, incluyendo PHP 5.x y 6.x

### Cambiado
- Mejora en el proceso de compilación para incluir todos los recursos necesarios
- Optimización del tamaño del ejecutable final
- Actualización de la expresión regular para detectar versiones de PHP más antiguas
- Mejora en la lógica de procesamiento para manejar versiones x86 cuando no hay versiones x64 disponibles

### Corregido
- Problema con la detección de la versión PHP 7.4.33 y otras versiones específicas
- Uso exclusivo de la URL de archivos para obtener todas las versiones de PHP disponibles
- Expresión regular mejorada para capturar correctamente todos los formatos de nombres de archivo
- Simplificación del proceso de determinación de URL de descarga

## Estado del Proyecto
El desarrollo de PHP Switcher se ha completado satisfactoriamente. Todas las características planeadas han sido implementadas y el proyecto se considera finalizado en su estado actual. La aplicación está lista para ser distribuida a los usuarios finales mediante el instalador MSI o el ejecutable independiente.