# ADR-0003: Result Pattern propio en vez de excepciones

## Estado

Aceptado. Documentado como decisión 3 en el [`README.md`](../../README.md).

## Contexto

Un backend de catálogo tiene muchas fallas *esperadas*: una categoría que no existe, un
`CustomerSource` inválido, un insert que viola el índice único de `PublicId`. Modelarlas como
excepciones mezcla el control de flujo normal con el manejo de errores excepcionales, encarece el
testing (hay que capturar) y deja al controller adivinando qué puede reventar.

Hacía falta una forma explícita y uniforme de decir "esto falló, y por esto" a través de las capas.

### Opciones consideradas

- **Excepciones + middleware de manejo centralizado** — idiomático en ASP.NET Core y sin código
  extra, pero convierte fallas de negocio esperadas en excepciones y pierde la explicitud en la
  firma de los métodos.
- **[ErrorOr](https://github.com/amantinband/error-or)** — librería madura de Result para .NET.
- **[FluentResults](https://github.com/altmann/FluentResults)** — alternativa equivalente.
- **Implementación propia de `Result` / `Result<T>`** — control total, cero dependencias, y una
  superficie mínima que el equipo entiende completa.

## Decisión

Implementación propia en `TheOffice.Domain.Common`: `Result` y `Result<T>`, con constructor
privado, factories `Success` / `Failure` y un `Error` de tipo `string`. `Result<T>` tiene una
conversión implícita desde `T`.

La regla de uso: **no se lanzan excepciones para control de flujo** en servicios ni repositorios;
las fallas esperadas devuelven `Result.Failure(...)`. Los controllers revisan `IsSuccess`/`Error`
y traducen a `BadRequest` / `Ok`.

Los repositorios sí capturan `Exception` en las rutas de escritura, pero solo en la frontera con
EF Core y únicamente para convertir la excepción en un `Result.Failure`.

## Consecuencias

### Más fácil

- **Separación de responsabilidades**: éxito y error se manejan de forma explícita.
- **Estandarización**: un solo tipo de retorno para reglas de negocio, validaciones y acceso a datos.
- **Claridad**: la firma del método dice que la operación puede fallar.
- **Testing**: se prueban respuestas esperadas en vez de capturar excepciones.
- Cero dependencias externas para algo tan central.

### Más difícil

- **Sobrecarga de código**: cada capa desempaqueta y reenvía el `Result`.
- **Se pierde el stack trace** de la falla original — el `Error` es solo un `string`.
- **Requiere adopción de todo el equipo**: basta con que alguien lance una excepción para romper la
  uniformidad, y nada lo verifica automáticamente.
- **Convivencia con `null`**: hoy "no encontrado" se expresa devolviendo `Result<T>?` nulo, no un
  `Result.Failure`. Son dos mecanismos distintos para dos clases de ausencia, y hay que respetarlo
  al escribir código nuevo.
- El `Error` como `string` no permite distinguir tipos de falla programáticamente — todo termina
  en `BadRequest`, incluso lo que semánticamente sería un conflicto o un 422.

### Revisitar cuando

- Se necesite distinguir categorías de error para mapear a distintos códigos HTTP (entonces:
  un `Error` estructurado, o migrar a ErrorOr/FluentResults).
- La sobrecarga de desempaquetar resultados se vuelva ruido dominante en los servicios.
