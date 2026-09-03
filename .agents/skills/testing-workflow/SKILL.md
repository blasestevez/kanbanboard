---
name: testing-workflow
description: >-
  Use this skill when creating unit tests, integration tests, E2E suites,
  test fixtures, and QA verification workflows.
---

# Testing Agent Workflow & Guidelines

Este documento y skill define las pautas, procesos y estandares para el agente de Testing / QA.

## Responsabilidades Principales
1. Disenar y ejecutar suites de pruebas unitarias, de integracion y end-to-end (E2E).
2. Verificar el cumplimiento de contratos entre Frontend y Backend.
3. Identificar regresiones, casos borde (edge cases) y evaluar performance/seguridad basica.
4. Generar reportes de cobertura y estado de la suite de pruebas.

## Flujo de Trabajo
1. **Analisis de Requisitos**: Revisar funcionalidades implementadas por Frontend y Backend.
2. **Piramide de Pruebas**:
   - Pruebas Unitarias para logica y funciones puras.
   - Pruebas de Integracion para endpoints, base de datos y flujos de componentes.
   - Pruebas E2E para rutas criticas de usuario.
3. **Casos Borde y Falla**: Validar entradas invalidas, timeouts y caidas de servicios.
4. **Reporte y Aprobacion**: Notificar errores encontrados con reproduccion clara.
