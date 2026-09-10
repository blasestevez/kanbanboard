# Kanbanboard

Un tablero Kanban interactivo para gestion visual de proyectos y tareas, construido con arquitectura desacoplada y sincronizacion en tiempo real.

El proyecto fue desarrollado como una aplicacion completa para gestion de flujos de trabajo, priorizando una interfaz rapida e intuitiva, actualizacion colaborativa inmediata y un backend estructurado con buenas practicas de ingenieria de software.

## Funcionalidades principales

- Espacios de trabajo y tableros: Organizacion jerarquica de proyectos con membresias y roles.
- Tableros dinamicos: Creacion de columnas y tarjetas con reordenamiento mediante drag and drop fluido.
- Colaboracion en tiempo real: Notificacion inmediata de movimientos y ediciones entre usuarios conectados al mismo tablero a traves de WebSockets.
- Detalle de tarjetas: Soporte para descripciones enriquecidas, listas de verificacion (checklists) con calculo de progreso, comentarios, etiquetas tematicas, fechas de vencimiento y carga de archivos adjuntos.
- Autenticacion y seguridad: Registro e inicio de sesion con tokens JWT, soporte para autenticacion externa (OAuth con Google y GitHub) y proteccion de rutas.
- Personalizacion: Fondos tematicos por tablero y colores de portada por tarjeta.

## Arquitectura y tecnologias

El proyecto sigue una arquitectura desacoplada cliente-servidor:

### Frontend
- Framework: Angular 21 utilizando signals, componentes standalone y el nuevo flujo de control nativo.
- Diseno y estilos: Tailwind CSS, con diseno responsivo y enfocado en la usabilidad.
- Drag and drop: Angular CDK DragDrop para una interaccion natural de arrastrar y soltar.
- Comunicacion en tiempo real: Cliente de SignalR para recepcion de eventos push desde el servidor.
- Hosting: Vercel (distribucion global como Single Page Application con proxy inverso a los endpoints del backend).

### Backend
- Framework: .NET 10 (ASP.NET Core Web API).
- Acceso a datos: Entity Framework Core con PostgreSQL (Npgsql), incluyendo migraciones automaticas en despliegue.
- Comunicacion bidireccional: ASP.NET Core SignalR para difusion de cambios entre miembros del tablero.
- Autenticacion: JWT Bearer Tokens y BCrypt para almacenamiento seguro de credenciales.
- Contenedores: Dockerfile multi-etapa listo para despliegue continuo.

## Estructura del repositorio

- backend/: Solucion en .NET 10 con controladores REST, servicios de aplicacion, repositorios y configuracion de SignalR.
- frontend/: Codigo fuente de la aplicacion en Angular, dividido por modulos funcionales, servicios de estado y componentes.
- tests/: Bateria de pruebas de integracion en .NET con xUnit y FluentAssertions.
- docs/: Contratos de API documentados y arquitectura del flujo de trabajo.

## Como ejecutar el proyecto en local

### Requisitos previos
- .NET 10 SDK
- Node.js 20 o superior
- Instancia de PostgreSQL (local o en Docker)

### 1. Configurar y levantar el Backend
1. Define la cadena de conexion a PostgreSQL en `backend/appsettings.Development.json` o mediante variable de entorno `ConnectionStrings__DefaultConnection`.
2. Posicionate en la carpeta `backend` y corre la aplicacion:
   ```bash
   cd backend
   dotnet run
   ```
El backend aplicara las migraciones y quedara disponible en `https://localhost:5001`.

### 2. Configurar y levantar el Frontend
1. Ingresa a la carpeta `frontend`:
   ```bash
   cd frontend
   npm install
   npm start
   ```
2. Abre tu navegador en `http://localhost:4200`.

## Autor

Desarrollado por Blas Estevez.
