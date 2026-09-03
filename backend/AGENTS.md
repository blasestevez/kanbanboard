# Backend Directory Rules & Guidelines

- **Scope**: Codigo dentro de /backend.
- **Responsable**: Agente Backend.
- **Skills Principales**:
  - ackend-workflow: Workflow general de APIs, arquitectura y persistencia.
  - dotnet-backend-patterns: Patrones de diseno, Clean Architecture, DI, CQRS/Repository en .NET.
- **Estandares**:
  - Mantener arquitectura limpia y desacoplada (Clean Architecture / Domain-Driven Design / Vertical Slices).
  - Validar todos los payloads de entrada antes de procesar reglas de negocio.
  - Documentar cambios de endpoints o DTOs en docs/api-specs.md para coordinar con Frontend y Testing.
  - Evitar queries N+1 y cuidar la integridad de datos.
