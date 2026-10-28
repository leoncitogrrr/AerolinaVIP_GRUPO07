# Sistema web de compra y reserva de pasajes – Aerolínea VIP
Primera Entrega · Grupo 7

## Integrantes
- Michael Tonato
- Mirley Ñacato

## Tecnologías
ASP.NET Framework 4.8.1 · MVC · Razor · Bootstrap · jQuery · ASP.NET Identity · SQL Server

## Estructura del repositorio
```
README.md
.gitignore
src/
├── Prog_03.slnx              ← solución de Visual Studio
└── ConAuntenticacion/        ← proyecto ASP.NET MVC (Controllers, Models, Views, Content, Scripts, Global.asax.cs, Web.config)
database/
└── Script_BD_AerolineaVIP_Unificado.sql
docs/
├── Primera_Entrega_Documentacion_Grupo7.pdf
├── UX-UI/                    ← wireframes del docente, wireframes aplicados y mapa de navegación
└── Gestion/                  ← políticas del punto 18.1 (DP-01 a DP-15)
```

## Cómo ejecutar
1. Ejecutar `database/Script_BD_AerolineaVIP_Unificado.sql` en SQL Server (crea `AerolineaVIP` y `AerolineaVIP_Identity`).
2. Revisar en `src/ConAuntenticacion/Web.config` las dos cadenas de conexión: `AerolineaVIPEntities` (dominio) y `DefaultConnection` (Identity). Usan autenticación de Windows (`Integrated Security=True`).
3. Abrir `src/Prog_03.slnx` en Visual Studio, restaurar los paquetes NuGet (clic derecho en la solución → *Restaurar paquetes NuGet*) y ejecutar con F5.
4. Al iniciar, `Global.asax.cs` crea los roles (SuperUsuario, Administrador, Counter y Cliente), los usuarios iniciales y les asigna su rol.

## Usuarios iniciales
`superusuario` · `administrador` · `counter` · `cliente1`

Las contraseñas no se publican en el repositorio.

## Estado
Versión `v1.0-primera-entrega` · 28 de octubre de 2026 · Grupo 7 (Michael Tonato, Mirley Ñacato)
