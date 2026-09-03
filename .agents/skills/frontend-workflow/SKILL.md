---
name: frontend-workflow
description: >-
  Use this skill when developing UI components, state management, layouts, client-side routing,
  and user experience workflows for the frontend application.
---

# Frontend Agent Workflow & Guidelines

Este documento y skill define las pautas, procesos y estandares para el agente de Frontend.

## Responsabilidades Principales
1. Construir interfaces de usuario responsivas, accesibles y modulares.
2. Gestionar el estado de la aplicacion (stores, contexts, cache de datos).
3. Integrar endpoints del backend respetando contratos de datos (REST/GraphQL/WebSocket).
4. Mantener la consistencia visual y sistema de componentes.

## Flujo de Trabajo
1. **Revisar Contratos de API**: Consultar especificaciones provistas por el agente de Backend o en docs/api-specs.md.
2. **Modularizacion**: Separar componentes presentacionales de hooks y logica de negocio.
3. **Manejo de Errores y Carga**: Garantizar estados explicitos de loading, error, empty y success.
4. **Verificacion**: Comprobar estilos, renderizado en diferentes viewports y validacion de tipos.
