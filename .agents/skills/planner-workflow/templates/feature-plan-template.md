# Feature Plan: [NOMBRE DE LA FEATURE]

**Fecha**: YYYY-MM-DD
**Estado**: Pendiente | En progreso | Completada
**Prioridad**: Alta | Media | Baja

---

## Descripcion

Breve descripcion de la feature y el valor que aporta al usuario.

## Requisitos Funcionales

1. ...
2. ...

## Requisitos No Funcionales

- Performance: ...
- Seguridad: ...
- Accesibilidad: ...

---

## Contrato de API

### Endpoints Nuevos / Modificados

| Metodo | Ruta              | Request Body       | Response Body      | Descripcion        |
|:-------|:------------------|:-------------------|:-------------------|:-------------------|
| POST   | /api/example      | CreateExampleDto | ExampleResponse  | Crea un ejemplo    |
| GET    | /api/example/{id} | —                  | ExampleResponse  | Obtiene un ejemplo |

### DTOs

```csharp
public record CreateExampleDto(string Name, string Description);
public record ExampleResponse(int Id, string Name, string Description, DateTime CreatedAt);
```

---

## Tareas

| ID       | Titulo                               | Agente   | Dependencias | Prioridad | Estado     |
|:---------|:-------------------------------------|:---------|:-------------|:----------|:-----------|
| TASK-001 | Crear entidad y migracion Example    | Backend  | —            | Alta      | Pendiente  |
| TASK-002 | Implementar ExampleService           | Backend  | TASK-001     | Alta      | Pendiente  |
| TASK-003 | Crear ExampleController              | Backend  | TASK-002     | Alta      | Pendiente  |
| TASK-004 | Tests unitarios de ExampleService    | Testing  | TASK-002     | Alta      | Pendiente  |
| TASK-005 | Tests de integracion de API          | Testing  | TASK-003     | Media     | Pendiente  |
| TASK-006 | Componente UI ExampleForm            | Frontend | TASK-003     | Alta      | Pendiente  |
| TASK-007 | Integrar frontend con API            | Frontend | TASK-003,006 | Alta      | Pendiente  |
| TASK-008 | Tests E2E del flujo completo         | Testing  | TASK-007     | Media     | Pendiente  |

---

## Diagrama de Dependencias

```mermaid
graph TD
    T1[TASK-001: Entidad + Migracion] --> T2[TASK-002: Service]
    T2 --> T3[TASK-003: Controller]
    T2 --> T4[TASK-004: Unit Tests]
    T3 --> T5[TASK-005: Integration Tests]
    T3 --> T6[TASK-006: UI Component]
    T3 --> T7[TASK-007: Frontend + API]
    T6 --> T7
    T7 --> T8[TASK-008: E2E Tests]
```

---

## Notas y Decisiones de Diseño

- ...

## Riesgos Identificados

- ...
