# IpScope Pro

Aplicación de escritorio para **monitorización y escaneo de red** en Windows. Permite vigilar hosts con ping (ICMP/TCP) en tiempo real, descubrir dispositivos de la red y recibir alertas cuando algo se cae.

## Características

- **Monitores (probes)**: ping ICMP y TCP con estado en tiempo real, latencia (media o última), historial y estadísticas de paquetes.
- **Escáner de red**: rangos de IP, CIDR y listas; escaneo de puertos (modo rápido o exhaustivo); resolución de MAC y fabricante (base OUI embebida).
- **Exportación**: resultados del escáner a CSV, Excel y JSON; monitores y ajustes a JSON (ajustes también cifrado).
- **Alertas**: notificaciones emergentes, toast de Windows, correo (SMTP) y Telegram.
- **Personalización**: tema claro/oscuro, colores configurables por estado y tamaño de fuente.
- **Idiomas**: español e inglés.
- **Bandeja del sistema**: minimizar a la bandeja e iniciar con Windows (en modo instalado).

## Modo portable vs. instalado

- **Portable (por defecto)**: el `.exe` se ejecuta desde cualquier carpeta y no guarda nada. Los ajustes y monitores viven en memoria y se pierden al cerrar. Para conservarlos, usa *Exportar/Importar*. El log a fichero está desactivado por defecto y su ruta es configurable.
- **Instalado**: desde *Ajustes → Instalación → Instalar en Windows* se abre un asistente donde eliges la carpeta de instalación (por defecto *Archivos de programa*), si crear accesos directos en el escritorio y el menú Inicio, y la app se registra en Windows (aparece en *Aplicaciones y características* con su desinstalador). Los ajustes y monitores se guardan en `%LOCALAPPDATA%\IpScopePro` y se habilita *Iniciar con Windows*.

## Requisitos

- Windows 10/11 (x64).
- La versión autocontenida no requiere tener .NET instalado.
- Para compilar: [.NET 9 SDK](https://dotnet.microsoft.com/download).

## Descargar / Ejecutar

Cada release incluye dos variantes para Windows x64:

| Archivo | Tamaño aprox. | Requiere |
|---|---|---|
| `IpScopePro-vX.Y.Z-win-x64-self-contained` | ~78 MB | Nada |
| `IpScopePro-vX.Y.Z-win-x64-framework-dependent` | ~39 MB | .NET 9 Desktop Runtime |

- **self-contained**: lleva todo incluido; basta ejecutar `IpScopePro.exe`.
- **framework-dependent**: más ligera; necesita el [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) instalado.

En ambas basta con ejecutar `IpScopePro.exe` (no requiere instalación). La variante autocontenida incluye 5 DLL nativos de WPF junto al `.exe`.

## Compilar desde el código

```powershell
# Autocontenido (recomendado, no requiere .NET en el destino)
.\publish.ps1

# Más pequeño (~39 MB) pero requiere .NET 9 Desktop Runtime en el destino
.\publish.ps1 -SelfContained:$false
```

O directamente:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o publish
```

## Tecnologías

- .NET 9 (WPF) y C#.
- CommunityToolkit.Mvvm (arquitectura MVVM).
- Microsoft.Extensions.DependencyInjection (inyección de dependencias).
- ClosedXML (exportación a Excel).
- Hardcodet.NotifyIcon.Wpf (bandeja del sistema).

## Licencia

MIT. Consulta el archivo [LICENSE](LICENSE).
