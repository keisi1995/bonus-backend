# Bonos de desempeño: prueba técnica Full Stack

Módulo para registrar bonos de desempeño (monto fijo o porcentaje del sueldo), calcular bruto, retención y neto, anularlos, eliminarlos y pagarlos repartidos por proyecto.

| Capa | Tecnología | Ubicación |
|---|---|---|
| Base de datos | SQL Server 2019 | `database/` (este repo) |
| Backend | .NET 10, ASP.NET Core Web API, EF Core | `src/` y `tests/` (este repo) |
| Frontend | Angular 22 standalone, sin librería de UI | repo `frontend/` |

## Cómo levantarlo

### 1. Base de datos
Ejecuta los scripts en orden sobre SQL Server 2019+:

```
database/01_schema_seed.sql                       -- base del proyecto, sin modificar
database/02_create_bonus.sql                      -- tabla Bonus, checks e índices
database/03_create_bonus_project_distribution.sql -- detalle de pago por proyecto (punto extra 1)
```

Con Docker, por ejemplo:

```bash
docker cp database/02_create_bonus.sql sqlserver2019:/tmp/
docker exec sqlserver2019 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "<password>" -C -I -i /tmp/02_create_bonus.sql
```

Los scripts 02 y 03 se pueden ejecutar más de una vez. Usa `-I` (`QUOTED_IDENTIFIER ON`) en las consultas manuales con `sqlcmd`: SQL Server lo exige para modificar tablas con índices filtrados. La API ya trabaja con esa opción activa.

### 2. API
La cadena de conexión no está versionada: se lee de la variable de entorno `ConnectionStrings__BonusTest`.

```powershell
$env:ConnectionStrings__BonusTest = "Server=localhost,1433;Database=BonusTest;User Id=sa;Password=<password>;TrustServerCertificate=True;"
dotnet run --project src/BonusTest.Api --launch-profile http   # http://localhost:5106/swagger
dotnet test                                                    # pruebas unitarias
```

### 3. Frontend
```bash
cd ../frontend
yarn install
yarn start      # http://localhost:4200/bonuses
```

## Endpoints

| Método | Ruta | Respuesta |
|---|---|---|
| GET | `/api/v1/Bonus?departmentCode=&employeeNumber=&period=&bonusStateCode=&pageIndex=&pageSize=` | `PagedResponse<BonusListItemDto>` |
| GET | `/api/v1/Bonus/employee/{departmentCode}/{employeeNumber}` | Empleado activo |
| POST | `/api/v1/Bonus` | 201 con id y montos calculados |
| PUT | `/api/v1/Bonus/{bonusId}/cancel` | 204 (0201 → 0203) |
| DELETE | `/api/v1/Bonus/{bonusId}` | 204 (eliminación lógica) |
| POST | `/api/v1/Bonus/{bonusId}/pay` | 200 con la distribución por proyecto (0201 → 0202) |

Las operaciones de escritura requieren el header `UserCode`. Toda violación de una regla de negocio responde **409** `{ code, description }`; por ejemplo, `EMPLOYEE_NOT_ACTIVE`, `FUTURE_PERIOD`, `BONUS_ALREADY_EXISTS` o `BONUS_NOT_REGISTERED`.

## Decisiones técnicas

### Base de datos (`Bonus`)
- **FK compuesta** `(DepartmentCode, EmployeeNumber)` → `Employee`. También hay FK a `BonusType`, `BonusReason` y `State`.
- **Montos en `DECIMAL(16,2)`**, igual que `MonthlySalary`. `BonusValue` guarda el monto o el porcentaje según `BonusTypeId`; un CHECK limita el porcentaje a 100.
- **Copia de `MonthlySalary` y `TaxRate`** al registrar: el bono queda reproducible aunque después cambien los datos del empleado. Los montos calculados se guardan (no se recalculan al listar) y un CHECK asegura `Net = Gross − Tax`.
- **`Period CHAR(7)` (YYYY-MM)** con CHECK de formato. Como texto con ese formato, el orden alfabético coincide con el orden cronológico.
- **Eliminación lógica** con `IsDeleted` y auditoría con `CreatedBy/CreatedAt/ModifiedBy/ModifiedAt`.
- **Índices**
  - `UX_Bonus_Employee_Period_Active`: único y **filtrado** (`IsDeleted = 0 AND BonusStateCode <> '0203'`). La regla de "un bono activo por periodo" queda garantizada por la BD aunque lleguen dos peticiones a la vez. El servicio valida antes para dar un mensaje claro, y si aun así gana la carrera la otra petición, el repositorio convierte el error de índice único en un 409.
  - `IX_Bonus_Employee_Period`: soporta la FK compuesta y el filtro por empleado.
  - `IX_Bonus_Period_State` (filtrado a no eliminados): filtros por periodo y estado del listado.

### Backend
- **Capas**, respetando el proyecto base:
  - `Domain`: la entidad `Bonus` con setters privados. Las transiciones (`Cancel`, `Delete`, `Pay`) validan que el estado sea Registrado. `BonusCalculator` y `BonusDistributor` son funciones puras y fáciles de probar.
  - `Application`: `BonusService` aplica las reglas que necesitan datos (empleado activo, motivo activo, duplicados, periodo no futuro, límites de monto o porcentaje, `UserCode`). Depende de la interfaz `IBonusRepository`, no de EF.
  - `Infrastructure`: `BonusRepository` con EF Core. El listado se construye como `IQueryable` con filtros opcionales, JOINs a los catálogos y `Skip/Take`, que EF traduce a `OFFSET/FETCH`: **la paginación ocurre en SQL Server**. Las columnas `CHAR` se mapean como no Unicode para evitar conversiones implícitas que impidan usar los índices.
  - `Api`: `BonusController` solo traduce HTTP ↔ servicio. No tiene reglas de negocio.
- **Redondeo**: `Math.Round(x, 2, MidpointRounding.AwayFromZero)`. El valor por defecto de .NET es el redondeo bancario (0.125 → 0.12), que no cumple la regla pedida.
- **Fecha actual** mediante `TimeProvider` inyectado, para poder controlarla en pruebas.
- **Pago**: la distribución y el cambio de estado a Pagado se guardan en un único `SaveChangesAsync`, que EF Core ejecuta en una sola transacción. Si hay un solo proyecto, recibe todo; si hay varios, el último recibe la diferencia para que la suma cuadre exacto.

### Frontend
- Sin librería de UI: se extendieron los estilos globales del proyecto base.
- Estado con `signal`/`computed`, formularios con Reactive Forms tipados (`NonNullableFormBuilder`).
- La paginación siempre llama a la API con los **filtros de la última búsqueda**, no con lo que el usuario está escribiendo sin presionar Buscar.
- Modal como componente aparte (`input()` / `output()`). La vista previa replica el cálculo del backend; el monto que se guarda lo calcula siempre el backend.
- Los errores 409 los muestra solo el interceptor existente; los componentes solo manejan el caso de éxito.

## Pruebas
`tests/BonusTest.Domain.Tests` (xUnit, 21 pruebas): el ejemplo del enunciado (500 / 40 / 460), el redondeo de medio hacia arriba en bruto y retención, la retención 0 %, la distribución (un proyecto, proporcional y con ajuste del último) y las transiciones de estado.

## Pendiente / mejoras posibles
- Pruebas de `BonusService` con un repositorio falso y pruebas de integración de la API.
- Control de concurrencia optimista (`rowversion`) para el caso de anular y pagar el mismo bono a la vez. Hoy se valida el estado al leer, pero las dos operaciones podrían cruzarse.
- Ver el detalle de la distribución de un bono pagado desde la grilla (hoy se devuelve en la respuesta de `/pay`).
- Confirmaciones con un diálogo propio en lugar de `window.confirm`.
- Autenticación real: hoy `UserCode` se toma del header, tal como pide el enunciado.
