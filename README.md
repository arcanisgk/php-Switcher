# PHP Switcher

PHP Switcher es una aplicación de Windows que permite gestionar y cambiar fácilmente entre diferentes versiones de PHP en tu sistema.

## Características

- Descarga e instala múltiples versiones de PHP desde los repositorios oficiales
- Cambia rápidamente entre versiones instaladas con un solo clic
- Gestiona versiones Thread Safe y Non-Thread Safe
- Interfaz gráfica intuitiva y fácil de usar
- Configuración automática de variables de entorno
- Soporte para PHP 5.x, 7.x y 8.x

## Requisitos del sistema

- Windows 10/11
- .NET 9.0 o superior
- Permisos de administrador (para crear enlaces simbólicos)

## Instalación

1. Descarga la última versión desde la sección de releases
2. Ejecuta el instalador o extrae el archivo ZIP
3. Ejecuta `PhpSwitcher.exe` como administrador

## Uso

### Instalación de versiones de PHP

1. Ve a la pestaña "Available Versions"
2. Selecciona la versión de PHP que deseas instalar
3. Haz clic en "Download Selected Version(s)"
4. Espera a que se complete la descarga e instalación

### Cambiar entre versiones de PHP

1. Ve a la pestaña "Installed Versions"
2. Selecciona la versión de PHP que deseas activar
3. Haz clic en "Activate Selected Version"
4. La versión seleccionada estará disponible inmediatamente en la línea de comandos

### Eliminar versiones de PHP

1. Ve a la pestaña "Installed Versions"
2. Selecciona la versión de PHP que deseas eliminar
3. Haz clic en "Remove Selected Version"
4. Confirma la eliminación

## Cómo funciona

PHP Switcher crea un enlace simbólico en `C:\php` que apunta a la versión de PHP seleccionada. También asegura que `C:\php` esté en la variable de entorno PATH del sistema.

## Desarrollo

### Requisitos para desarrollo

- Visual Studio 2022 o superior
- .NET 9.0 SDK
- Git

### Clonar el repositorio

```bash
git clone https://github.com/tu-usuario/php-Switcher.git
cd php-Switcher
```

### Compilación

```bash
dotnet build
```

### Ejecución

```bash
# Ejecución normal
dotnet run

# Ejecución con consola visible (modo debug)
dotnet run -- -c

# Ejecución en modo desarrollo
dotnet run -- -d
```

### Estructura del proyecto

Para entender la estructura del proyecto, consulta los siguientes archivos:
- [structure.md](structure.md) - Estructura detallada del proyecto
- [features.md](features.md) - Características implementadas
- [task_pending.md](task_pending.md) - Tareas pendientes y mejoras futuras
- [CHANGELOG.md](CHANGELOG.md) - Historial de cambios

## Licencia

Este proyecto está licenciado bajo la licencia MIT - ver el archivo LICENSE para más detalles.