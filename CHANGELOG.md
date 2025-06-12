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

### Cambiado
- Mejora en el manejo de nulabilidad en todo el código para reducir advertencias de compilación
- Mejora en la visualización de versiones instaladas y disponibles con indicadores visuales
- Proceso de inicialización actualizado para incluir sincronización de versiones instaladas

### Corregido
- Problema de sincronización entre versiones PHP instaladas en el sistema de archivos y la configuración
- Advertencias de nulabilidad en la compilación
- Manejo mejorado de referencias potencialmente nulas en el código

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

## Pendiente para Futuras Versiones
- Integración con servidores web (IIS, Apache)
- Gestión de extensiones PHP
- Optimización de rendimiento de descarga
- Aplicación ejecutable independiente
- Selección de espejos de descarga
- Descargas paralelas
- Configuración PHP avanzada
- Mejoras en la gestión de iconos de barra de tareas
- Implementación de datos de aplicación persistentes
- Soporte para múltiples idiomas
- Mejoras de accesibilidad
- Sistema de actualizaciones automáticas