---
name: planner-workflow
description: >-
  Use this skill when planning project features, breaking down requirements into tasks,
  defining implementation order, assigning work to specialized agents (frontend, backend,
  testing), coordinating API contracts between layers, and tracking overall project progress.
  This is the orchestration brain of the multi-agent workflow.
---

# Planner Agent Workflow & Orchestration Guide

Este skill define el rol, las responsabilidades y el proceso de trabajo del **Agente Planner**:
el coordinador central del workflow multi-agente.

---

## Rol del Planner

El Planner **no escribe codigo de produccion**. Su trabajo es:

1. **Analizar requisitos** del usuario y traducirlos en features concretas.
2. **Descomponer features** en tareas atómicas asignables a un agente especializado.
3. **Definir el orden de ejecucion** respetando dependencias entre capas.
4. **Diseñar contratos de datos** (DTOs, schemas, endpoints) que sirvan de interfaz entre Backend y Frontend.
5. **Coordinar la ejecucion** delegando a los agentes y verificando resultados.
6. **Gestionar riesgos** identificando bloqueos, dependencias cruzadas y decisiones de diseño.

---

## Agentes Disponibles

| Agente       | Directorio   | Skills                                          | Responsabilidad                              |
|:-------------|:-------------|:------------------------------------------------|:---------------------------------------------|
| **Frontend** | rontend/  | rontend-workflow, impeccable               | UI, componentes, estado, UX                  |
| **Backend**  | ackend/   | ackend-workflow, dotnet-backend-patterns   | APIs, servicios, modelos, auth, persistencia |
| **Testing**  | 	ests/     | 	esting-workflow, dotnet-testing-strategy   | Unit tests, integracion, E2E, cobertura      |

---

## Proceso de Planificacion

### Fase 1 — Analisis de Requisitos

1. Recibir el requisito del usuario (feature, bugfix, mejora).
2. Clarificar ambigüedades: hacer preguntas concretas antes de planificar.
3. Identificar el **alcance**: ¿afecta solo frontend? ¿solo backend? ¿full-stack?
4. Evaluar si requiere cambios de esquema de BD, nuevos endpoints, o solo UI.

### Fase 2 — Diseño de Alto Nivel

1. Definir la **arquitectura de la feature**:
   - Nuevas entidades / tablas / migraciones necesarias.
   - Endpoints requeridos (método, ruta, request/response DTOs).
   - Componentes de UI nuevos o modificados.
   - Flujos de usuario (happy path + edge cases).
2. Documentar el **contrato de API** en docs/api-specs.md.
3. Crear un diagrama de flujo si la feature es compleja.

### Fase 3 — Descomposicion en Tareas

Crear una lista de tareas ordenada por **dependencias**, siguiendo esta prioridad:

`
1. Modelos / Migraciones de BD          → Backend
2. Capa de Servicios / Logica de negocio → Backend
3. Endpoints / Controllers               → Backend
4. Tests unitarios del backend            → Testing
5. Tests de integracion de API            → Testing
6. Componentes UI / Paginas               → Frontend
7. Integracion frontend ↔ API             → Frontend
8. Tests E2E del flujo completo           → Testing
`

Cada tarea debe incluir:
- **ID**: Identificador unico (ej: TASK-001).
- **Titulo**: Descripcion corta.
- **Agente**: Frontend / Backend / Testing.
- **Dependencias**: IDs de tareas que deben completarse antes.
- **Criterios de aceptacion**: Que se verifica para dar la tarea como terminada.
- **Prioridad**: Alta / Media / Baja.

### Fase 4 — Delegacion y Ejecucion

1. **Enviar instrucciones claras** a cada agente con:
   - Contexto de la feature.
   - Tarea especifica asignada.
   - Contratos de datos relevantes.
   - Criterios de aceptacion.
2. **Respetar dependencias**: No lanzar frontend hasta que los endpoints existan.
3. **Paralelizar** donde sea posible: backend y diseño de componentes UI pueden avanzar en paralelo si el contrato esta definido.

### Fase 5 — Verificacion y Cierre

1. Confirmar que cada tarea cumple sus criterios de aceptacion.
2. Verificar que los tests pasan (unitarios + integracion + E2E).
3. Revisar coherencia entre las capas (los tipos coinciden, los endpoints responden correctamente).
4. Marcar la feature como completada.

---

## Formato del Plan de Feature

Usar el template en 	emplates/feature-plan-template.md para documentar cada feature planificada.

---

## Reglas de Coordinacion

- **Contrato primero**: Siempre definir DTOs y endpoints antes de implementar.
- **No duplicar logica**: La validación de negocio vive en Backend; el frontend solo valida UX.
- **Un agente, una responsabilidad**: No pedir al agente de Testing que escriba codigo de produccion.
- **Comunicacion via docs**: Cambios de contrato se reflejan en docs/api-specs.md antes de implementar.
- **Iterar incrementalmente**: Features grandes se dividen en slices entregables.
