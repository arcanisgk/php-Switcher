# Guía de Compilación - PHP Switcher C#

Esta guía proporciona instrucciones detalladas para compilar y ejecutar el proyecto PHP Switcher C#.

## Requisitos Previos

### Software Necesario
- Visual Studio 2022 o posterior (recomendado)
- .NET 9.0 SDK o posterior
- Windows 10/11

### Permisos
- Privilegios de administrador para ejecutar la aplicación (necesarios para crear enlaces simbólicos)

## Obtener el Código Fuente

1. Clonar el repositorio:
   ```
   git clone https://github.com/yourusername/php-Switcher.git
   cd php-Switcher
   ```

2. Abrir la solución en Visual Studio:
   - Abrir Visual Studio
   - Seleccionar "Abrir un proyecto o solución"
   - Navegar a la carpeta del repositorio clonado
   - Seleccionar el archivo `php-Switcher.sln`

## Compilación del Proyecto

### Compilación desde Visual Studio

1. Seleccionar la configuración de compilación:
   - Para desarrollo: `Debug`
   - Para producción: `Release`

2. Compilar la solución:
   - Menú: Compilación > Compilar Solución
   - O usar el atajo de teclado: Ctrl+Shift+B

### Compilación desde Línea de Comandos

1. Navegar al directorio del proyecto:
   ```
   cd path\to\php-Switcher\php-Switcher
   ```

2. Compilar el proyecto:
   ```
   dotnet build
   ```

3. Para compilar en modo Release:
   ```
   dotnet build -c Release
   ```

## Ejecución del Proyecto

### Ejecución desde Visual Studio

1. Configurar el proyecto como proyecto de inicio (si no lo está ya)
2. Presionar F5 para iniciar la depuración o Ctrl+F5 para iniciar sin depuración

### Ejecución desde Línea de Comandos

1. Navegar al directorio del proyecto:
   ```
   cd path\to\php-Switcher\php-Switcher
   ```

2. Ejecutar el proyecto:
   ```
   dotnet run
   ```

3. Para ejecutar en modo Debug (con consola visible):
   ```
   dotnet run -- -c
   ```

4. Para ejecutar en modo Desarrollo:
   ```
   dotnet run -- -d
   ```

## Creación de un Ejecutable Independiente

Para crear un ejecutable independiente que pueda distribuirse sin necesidad de tener .NET instalado:

1. Navegar al directorio del proyecto:
   ```
   cd path\to\php-Switcher\php-Switcher
   ```

2. Publicar como ejecutable independiente:
   ```
   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
   ```

3. El ejecutable se encontrará en:
   ```
   bin\Release\net9.0-windows\win-x64\publish\PhpSwitcher.exe
   ```

## Solución de Problemas Comunes

### Error: No se puede crear enlace simbólico

**Problema**: La aplicación no puede crear enlaces simbólicos incluso con privilegios de administrador.

**Solución**: 
1. Asegúrate de que la política de seguridad de Windows permite la creación de enlaces simbólicos.
2. Ejecuta la aplicación como administrador.
3. Si estás en Windows 10 Home, es posible que necesites habilitar el modo de desarrollador.

### Error: No se puede conectar a windows.php.net

**Problema**: La aplicación no puede obtener la lista de versiones PHP disponibles.

**Solución**:
1. Verifica tu conexión a Internet.
2. Comprueba si hay un firewall o proxy que esté bloqueando la conexión.
3. Intenta usar la opción "Refresh Available Versions" en la pestaña Available Versions.

### Error: La aplicación se cierra inmediatamente al iniciar

**Problema**: La aplicación se cierra sin mostrar ningún mensaje de error.

**Solución**:
1. Ejecuta la aplicación desde la línea de comandos con el parámetro `-c` para ver los mensajes de error:
   ```
   path\to\PhpSwitcher.exe -c
   ```
2. Verifica los registros de eventos de Windows para posibles errores.

## Notas Adicionales

- La aplicación requiere privilegios de administrador para funcionar correctamente.
- Para desarrollo, se recomienda usar Visual Studio para aprovechar las herramientas de depuración.
- Los archivos de configuración se almacenan en `%APPDATA%\PHPSwitcher\`.
- Las versiones PHP se instalan por defecto en `C:\php-versions\` (configurable).
- La aplicación crea un enlace simbólico en `C:\php` que apunta a la versión PHP activa.