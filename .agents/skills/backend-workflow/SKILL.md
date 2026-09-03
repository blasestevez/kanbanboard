---
name: backend-workflow
description: >-
  Use this skill when designing APIs, server logic, database models, migrations,
  authentication, and business service layers.
---

# Backend Agent Workflow & Guidelines

Este documento y skill define las pautas, procesos y estandares para el agente de Backend.

## Responsabilidades Principales
1. Disenar e implementar APIs robustas, seguras y escalables (REST/GraphQL/etc.).
2. Modelar bases de datos, migraciones y esquemas de persistencia.
3. Implementar reglas de negocio, validaciones y autenticacion/autorizacion.
4. Documentar contratos de API para el agente de Frontend y Testing.

## Flujo de Trabajo
1. **Definicion de Contrato**: Definir el esquema de datos (DTOs/schemas) y documentar endpoints.
2. **Capa de Servicios**: Desacoplar la logica de negocio de los controladores/rutas.
3. **Manejo de Errores Centralizado**: Respuestas consistentes con codigos HTTP apropiados y mensajes claros.
4. **Seguridad y Validacion**: Validar entradas (inputs/payloads) y sanitizar parametros.
