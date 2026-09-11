# Kanbanboard

Un tablero Kanban interactivo para gestion visual de proyectos y tareas, construido con arquitectura desacoplada y sincronizacion en tiempo real.

<img width="1719" height="961" alt="board-demo" src="https://github.com/user-attachments/assets/8e977498-5c4e-409b-90e4-d26d71143176" />

El proyecto fue desarrollado como una aplicacion completa para gestion de flujos de trabajo, priorizando una interfaz rapida e intuitiva, actualizacion colaborativa inmediata y un backend estructurado con buenas practicas de ingenieria de software.

## Funcionalidades principales

- Espacios de trabajo y tableros: Organizacion jerarquica de proyectos con membresias y roles (Owner, Member, Observer).
- Tableros dinamicos: Creacion y gestion de columnas y tarjetas con reordenamiento mediante drag and drop fluido tanto horizontal como vertical.
- Colaboracion en tiempo real: Notificacion inmediata de movimientos, creaciones y ediciones entre usuarios conectados al mismo tablero a traves de WebSockets con SignalR.
- Detalle de tarjetas: Soporte para descripciones enriquecidas, listas de tareas (checklists) con calculo automatico de progreso, comentarios en hilo, etiquetas tematicas, fechas de vencimiento y carga de archivos adjuntos.
- Autenticacion y seguridad: Registro e inicio de sesion con tokens JWT, almacenamiento seguro de credenciales mediante ASP.NET Core Identity y proteccion de rutas con guards en el cliente.
- Personalizacion: Paleta de temas visuales por tablero y barras de color de portada por tarjeta.

## Arquitectura y tecnologias

El proyecto sigue una arquitectura desacoplada cliente-servidor:

### Frontend
- Framework: Angular 21 utilizando signals, componentes standalone y la sintaxis de control flow nativa (@if, @for).
- Diseno y estilos: SCSS modular con variables CSS, transiciones suaves y diseno responsivo.
- Drag and drop: Angular CDK DragDrop para una interaccion natural y accesible de arrastrar y soltar.
- Comunicacion en tiempo real: Cliente de SignalR con gestion de reconexion automatica e identificacion por tablero.
- Testing: Vitest integrado con Angular TestBed para pruebas unitarias de componentes y servicios.

### Backend
- Framework: .NET 10 (C# / ASP.NET Core Web API).
- Acceso a datos: Entity Framework Core con PostgreSQL (Npgsql), incluyendo migraciones automaticas en el arranque.
- Comunicacion bidireccional: ASP.NET Core SignalR con agrupamiento por canal (board-id).
- Seguridad y autenticacion: ASP.NET Core Identity con JWT Bearer Tokens.
- Testing: Bateria de pruebas de integracion en .NET con xUnit, FluentAssertions y WebApplicationFactory.
- Contenedores: Dockerfile multi-etapa optimizado para produccion.

## Estructura del repositorio

- backend/: Solucion en .NET 10 con controladores REST, servicios de aplicacion, DTOs, entidades y configuracion de SignalR.
- frontend/: Codigo fuente de la aplicacion en Angular, estructurado en carpetas core, features, layout y shared.
- tests/: Pruebas de integracion del backend que validan autenticacion, tableros, listas y autorizacion de roles.

## Como ejecutar el proyecto en local

### Requisitos previos
- .NET 10 SDK
- Node.js 20 o superior y npm
- Docker (para la base de datos PostgreSQL)

### 1. Iniciar la base de datos (PostgreSQL)

Si utilizas Docker, puedes iniciar el contenedor de base de datos con el siguiente comando:

```bash
docker run --name kanbanboard-pg -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=kanbanboard -p 5432:5432 -d postgres
```

### 2. Configurar y levantar el Backend

1. Verifica la cadena de conexion en `backend/appsettings.Development.json` (por defecto apunta a `localhost:5432` con usuario y contrasena `postgres`).
2. Desde la raiz del proyecto, inicia la API:
   ```bash
   dotnet run --project backend/Trellochocero.Api.csproj
   ```
3. El backend ejecutara las migraciones automaticamente y quedara escuchando en `http://localhost:5000` y `https://localhost:5001`.
4. La documentacion interactiva de endpoints esta disponible en `https://localhost:5001/` (Swagger UI).

### 3. Configurar y levantar el Frontend

1. En una nueva terminal, ingresa a la carpeta `frontend`:
   ```bash
   cd frontend
   npm install
   npm start
   ```
2. Abre tu navegador en `http://localhost:4200`.

### 4. Ejecucion de pruebas

- Pruebas del Backend:
  ```bash
  dotnet test tests/Trellochocero.Tests/Trellochocero.Tests.csproj
  ```
- Pruebas del Frontend:
  ```bash
  cd frontend
  npm test
  ```

## Despliegue y Hosting

- Frontend: Desplegado en Vercel como Single Page Application con reglas de rewrite para el routing de Angular y proxy reverso hacia la API.
- Backend y Base de Datos: Desplegado en Railway con contenedor Docker para la API de .NET 10 y base de datos gestionada PostgreSQL.
