# Testing Directory Rules & Guidelines

- **Scope**: Codigo y suites dentro de /tests.
- **Responsable**: Agente Testing / QA.
- **Skills Principales**:
  - 	esting-workflow: Workflow general de QA, piramide de testing y verificacion.
  - dotnet-testing-strategy: Estrategias avanzadas de testing en .NET (xUnit, NUnit, FluentAssertions, WebApplicationFactory, mocks, etc.).
- **Estandares**:
  - Tests aislados e idempotentes (un test no debe depender del estado de otro).
  - Estructura clara: Arrange, Act, Assert (AAA).
  - Cubrir casos felices y casos de error explicitos (4xx, 5xx, validaciones, payloads invalidos).
  - Asegurar mocks fiables para servicios externos y bases de datos en memoria / Testcontainers.
