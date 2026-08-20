# Contexto de Negocio

## Qué es TheOffice

**TheOffice** es una tienda en línea de artículos de oficina — papelería, mobiliario, tecnología y
organización. Es un producto real en construcción: el repositorio alberga el sistema completo del
ecommerce, del cual hoy existe únicamente la primera pieza, el **backend del catálogo**.

El resto de componentes se construye encima de esa base:

| Componente | Estado | Descripción |
|---|---|---|
| **Backend del catálogo** | ✅ Disponible | API REST de productos, categorías y clientes |
| Frontend del catálogo | 🔜 Por construir | Listado y detalle de producto para el comprador |
| Carrito y pedidos | 🔜 Por construir | Checkout sobre la entidad `Customer` |
| Backoffice | 🔜 Por construir | Administración del catálogo y del inventario |
| Autenticación | 🔜 Por construir | Cuentas de comprador y permisos de escritura |

## Quién paga por esto

La venta es **B2B**: los compradores son **departamentos de compras y secretarias** que abastecen
oficinas. No es un ecommerce de consumo masivo — la compra es recurrente, por catálogo y con
volumen, no impulsiva.

Eso explica varias decisiones que un agente vería como arbitrarias:

- El `PublicId` de producto funciona como **SKU** (`PRD-001`) y viaja en toda la API: un
  departamento de compras referencia productos por código, no por nombre.
- El catálogo se navega por **categoría** (`slug`) y por **búsqueda de texto** sobre nombre y
  descripción — el patrón de "encontrar el ítem que ya sé que necesito", no el de descubrimiento.
- `Stock` está en el resumen de producto, no solo en el detalle: la disponibilidad importa antes
  de armar el pedido.
- `Customer.Source` (`Website`, `Email`, `Phone`, `SocialMedia`) registra por dónde entró el
  cliente. El canal telefónico y el de correo son reales en este mercado.

<!-- TODO: completar modelo de ingresos, márgenes por categoría, condiciones de pago corporativo
     (crédito, orden de compra) o mínimos de pedido, si aplican. -->

## Ecosistema

El backend del catálogo es hoy el único sistema. Está pensado para ser consumido desde otro origen
—ya expone CORS configurable por origen—, así que los frontends futuros pueden vivir en otro
dominio sin cambios adicionales.

No hay integraciones con sistemas externos: pasarela de pago, ERP, facturación electrónica ni
proveedor de envíos. El único "adaptador externo" es
`ConsoleNotificationAdapter`, un stub que escribe a consola.

## Docs relacionados

- [Usuario objetivo](./target-user.md) — quién usa el producto y qué le importa.
- [Arquitectura](./architecture.md) — cómo está construido el sistema.
- [`README.md`](../README.md) — roadmap del producto y decisiones de implementación originales.
