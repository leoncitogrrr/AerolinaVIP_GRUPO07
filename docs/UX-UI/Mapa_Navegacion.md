# Mapa de navegación (apartado 11.4.2) – Grupo 7

La barra superior del layout (`Views/Shared/_Layout.cshtml`) muestra las opciones según el rol del usuario.

## Área pública (sin sesión)
- Inicio → `/Home/Index` (pantalla 1)
- Vuelos → `/Vuelos/Buscar` (5) → Resultados (6) → Disponibilidad (7)
- Iniciar sesión → `/Account/Login` (2) → ¿Olvidó su contraseña? (4)
- Registrarse → `/Account/Register` (3), solo con rol Cliente

## Área del cliente
- Flujo de compra: Selección de asiento (8) → Datos del pasajero (9) → Reserva (10) → Compra y pago (11) → Confirmación (12)
- Mis reservas (13) → Gestión y cancelación (14)
- Check-in (15) → Pase de abordar
- Comprobantes (16) → Ver / Descargar PDF

## Área de counter (menú «Counter»)
- Pasajeros (CRUD sin eliminar)
- Reservas de clientes (20)
- Check-in (15)
- Comprobantes (16)
- Puede usar el flujo de compra (8 a 12) a nombre de un cliente registrado

## Área administrativa (menú «Administración»: Administrador y SuperUsuario)
- Panel de accesos (17, sin dashboard)
- Vuelos e itinerarios (18) · Aeronaves (19) · Usuarios y reservas (20) · Reportes (21)
- CRUD con scaffolding: Aeropuertos, Rutas, Frecuencias, Vuelos, Itinerarios, Tramos de itinerario, Modelos de aeronave, Aeronaves y Pasajeros

## Configuración (menú «Configuración»: solo SuperUsuario)
- Registrar usuario con rol
- Marcas de tarjeta · Métodos de pago · Monedas · Tipos de documento · Datos del emisor
