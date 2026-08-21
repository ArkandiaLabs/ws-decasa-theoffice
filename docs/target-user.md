# Usuario Objetivo — TheOffice

> Este doc captura a quién le sirve el producto. Está escrito para orientar decisiones de producto
> y de API, no para reemplazar research con usuarios reales. Lo confirmado por el equipo es el
> segmento; lo demás está marcado como pendiente de validar.

## Segmento principal

**Departamentos de compras y secretarias** que abastecen oficinas. Compra B2B, recurrente y por
catálogo.

### Departamento de compras

- Compra por volumen y con proceso: cotiza, compara, deja constancia.
- Piensa en **códigos de producto**, no en nombres comerciales. El SKU es su identificador de
  trabajo.
- Necesita saber disponibilidad (`Stock`) **antes** de comprometer un pedido.
- Repite pedidos: el mismo conjunto de ítems, mes a mes.

### Secretaria / asistente administrativa

- Compra más pequeña y más frecuente, con urgencia ("se acabó el papel").
- Navega por **categoría** para encontrar rápido lo conocido, o busca por texto directo.
- Le importa el precio unitario y que el ítem llegue pronto; no está comparando marcas a fondo.

## Qué le importa a ambos

| Necesidad | Cómo la atiende el sistema hoy |
|---|---|
| Encontrar rápido un ítem conocido | Búsqueda por texto sobre nombre y descripción (`?search=`) |
| Navegar el catálogo por familia | Filtro por `slug` de categoría (`?category=`) |
| Referenciar un producto sin ambigüedad | `PublicId` con forma de SKU (`PRD-001`) en todas las rutas |
| Saber si hay existencias | `Stock` viene en el listado, no solo en el detalle |
| Ver solo lo que se puede comprar | El listado paginado filtra por `IsActive` |

## Lo que el sistema todavía no le da

Esto no es una lista de deseos: es contexto para que un agente no asuma capacidades inexistentes.

- **No hay cuenta de usuario.** `Customer` se registra, pero no hay login ni sesión.
- **No hay carrito ni pedido.** No se puede comprar; solo consultar el catálogo.
- **No hay historial ni repetición de pedidos**, que es justamente el comportamiento central del
  comprador recurrente.
- **No hay cotizaciones, órdenes de compra ni precios por volumen** — mecánicas habituales del B2B.
- **Una sola imagen por producto** (`ImageUrl`), sin variantes.

## Pendiente de validar

<!-- TODO: los siguientes puntos son inferencias del código y del segmento declarado, no research
     confirmado. Validar con usuarios reales antes de tratarlos como requisitos:
     - Si la decisión de compra la toma quien navega el catálogo o alguien que aprueba después.
     - Si el pedido se cierra en el sitio o se traslada a correo/teléfono (Customer.Source sugiere
       que los tres canales conviven).
     - Qué tan crítico es el precio unitario vs. el tiempo de entrega.
     - Si hace falta multi-usuario por empresa (varios compradores, un mismo cliente). -->

## Docs relacionados

- [Contexto de negocio](./business.md) — qué es el producto y quién paga.
- [Arquitectura](./architecture.md) — cómo está construido.
- [Modelo de datos](./data-model.md) — las entidades que representan a este usuario.
