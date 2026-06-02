# Fase 3 — Repository Pattern + Unit of Work (DIP)

**Principio:** Dependency Inversion Principle (DIP)  
**Esfuerzo estimado:** 2–3 días  
**Riesgo:** Alto — toca los 36 servicios de Infrastructure  
**Prioridad:** Media — mejora testabilidad y desacoplamiento de EF Core  

---

## Problema actual

Los 36 servicios de Infrastructure dependen directamente de `FreshDbContext`:

```csharp
public class OrderService : IOrderService
{
    private readonly FreshDbContext _context; // ← dependencia concreta
}
```

Esto significa:
- No se puede testear un servicio sin levantar una base de datos real
- Si se cambia ORM (EF Core → Dapper), hay que tocar los 36 servicios
- La capa de Core "conoce" detalles de Infrastructure a través del contexto

**DIP dice:** los módulos de alto nivel (servicios) no deben depender de módulos de bajo nivel (EF Core). Ambos deben depender de abstracciones.

---

## Solución: IRepository\<T\> + IUnitOfWork

### Paso 1 — Definir las interfaces en Fresh.Core

Nuevo archivo: `Fresh.Core/Interfaces/IRepository.cs`

```csharp
namespace Fresh.Core.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
}
```

Nuevo archivo: `Fresh.Core/Interfaces/IUnitOfWork.cs`

```csharp
namespace Fresh.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<Order>      Orders      { get; }
    IRepository<Invoice>    Invoices    { get; }
    IRepository<Customer>   Customers   { get; }
    IRepository<MenuItem>   MenuItems   { get; }
    IRepository<User>       Users       { get; }
    // ... un repositorio por entidad

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
```

### Paso 2 — Implementar en Infrastructure

Nuevo archivo: `Fresh.Infrastructure/Data/Repository.cs`

```csharp
namespace Fresh.Infrastructure.Data;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly FreshDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(FreshDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id) =>
        await _dbSet.FindAsync(id);

    public async Task<IEnumerable<T>> GetAllAsync() =>
        await _dbSet.ToListAsync();

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
        await _dbSet.Where(predicate).ToListAsync();

    public async Task AddAsync(T entity) =>
        await _dbSet.AddAsync(entity);

    public void Update(T entity) =>
        _dbSet.Update(entity);

    public void Remove(T entity) =>
        _dbSet.Remove(entity);
}
```

Nuevo archivo: `Fresh.Infrastructure/Data/UnitOfWork.cs`

```csharp
namespace Fresh.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly FreshDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(FreshDbContext context)
    {
        _context = context;
        Orders    = new Repository<Order>(context);
        Invoices  = new Repository<Invoice>(context);
        Customers = new Repository<Customer>(context);
        MenuItems = new Repository<MenuItem>(context);
        Users     = new Repository<User>(context);
        // ...
    }

    public IRepository<Order>    Orders    { get; }
    public IRepository<Invoice>  Invoices  { get; }
    public IRepository<Customer> Customers { get; }
    public IRepository<MenuItem> MenuItems { get; }
    public IRepository<User>     Users     { get; }

    public async Task<int> SaveChangesAsync() =>
        await _context.SaveChangesAsync();

    public async Task BeginTransactionAsync() =>
        _transaction = await _context.Database.BeginTransactionAsync();

    public async Task CommitAsync() =>
        await _transaction!.CommitAsync();

    public async Task RollbackAsync() =>
        await _transaction!.RollbackAsync();

    public void Dispose() => _context.Dispose();
}
```

### Paso 3 — Registrar en Program.cs

```csharp
// Reemplazar los AddScoped<FreshDbContext> individuales
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```

### Paso 4 — Migrar servicios

`OrderService` pasa de esto:

```csharp
public class OrderService : IOrderService
{
    private readonly FreshDbContext _context;
    // ...
    var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId);
}
```

A esto:

```csharp
public class OrderService : IOrderService
{
    private readonly IUnitOfWork _uow;
    // ...
    var users = await _uow.Users.FindAsync(u => u.Id == request.UserId);
    if (!users.Any()) throw new KeyNotFoundException(...);
}
```

---

## Estrategia de migración (para minimizar riesgo)

No migrar los 36 servicios de golpe. Usar este orden:

1. Implementar `IRepository<T>` y `IUnitOfWork` sin tocar servicios existentes
2. Migrar `OrderService` primero (el más complejo — si funciona, todo funciona)
3. Migrar servicios que son dependencias de `OrderService`: `InvoiceService`, `CustomerCreditService`
4. Migrar el resto en lotes de 5–6 servicios por sesión de trabajo
5. Cuando todos estén migrados, eliminar las inyecciones directas de `FreshDbContext` en servicios

---

## Nota sobre queries con Include

`IRepository<T>.GetAllAsync()` no cubre casos con `.Include()` anidados que sí usa `OrderService`. Para esos casos, agregar métodos específicos en el repositorio o usar `IQueryable`:

```csharp
// Opción: extender la interfaz para casos específicos
public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetWithItemsAsync(int id);
    Task<IEnumerable<Order>> GetAllWithDetailsAsync();
}
```

---

## Criterios de aceptación

- [ ] Ningún servicio de Infrastructure inyecta `FreshDbContext` directamente
- [ ] `IUnitOfWork` e `IRepository<T>` están definidos en `Fresh.Core`
- [ ] `UnitOfWork` y `Repository<T>` están implementados en `Fresh.Infrastructure`
- [ ] `OrderService` usa `IUnitOfWork` y los tests de integración pasan
- [ ] El resto de servicios migrados compilan y responden correctamente
