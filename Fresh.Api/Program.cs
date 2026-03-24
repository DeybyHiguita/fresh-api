using System.Text;
using Fresh.Api.Middleware;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Fresh.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<FreshDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IIngredientService, IngredientService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IPurchaseBatchService, PurchaseBatchService>();
builder.Services.AddScoped<IWorkShiftService, WorkShiftService>();
builder.Services.AddScoped<IMenuItemService, MenuItemService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IExpenseTypeService, ExpenseTypeService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<ICashPeriodService, CashPeriodService>();
builder.Services.AddScoped<ICashRegisterService, CashRegisterService>();
builder.Services.AddScoped<IEquipmentCategoryService, EquipmentCategoryService>();
builder.Services.AddScoped<IEquipmentService, EquipmentService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ICustomerCreditService, CustomerCreditService>();<<<<<<< feature/signal
builder.Services.AddScoped<IUserSessionService, UserSessionService>();
builder.Services.AddSignalR(); // ¡Habilita WebSockets!
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();


// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Fresh API",
        Version = "v1",
        Description = "API para gestión de bebidas - Fresh"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Ingresa tu token JWT"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS (para Angular en desarrollo y Docker)
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
    {
        builder.WithOrigins("http://localhost:4200")
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FreshDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("StartupDbPatch");

    try
    {
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS user_permissions (
                id          SERIAL      PRIMARY KEY,
                user_id     INT         NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                page        VARCHAR(50) NOT NULL,
                can_access  BOOLEAN     NOT NULL DEFAULT false,
                updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                CONSTRAINT ux_user_permissions_user_page UNIQUE (user_id, page)
            );
            CREATE INDEX IF NOT EXISTS ix_user_permissions_user_id ON user_permissions (user_id);
        ");

        // Garantizar que admin@admin.com sea siempre admin activo
        await db.Database.ExecuteSqlRawAsync(@"
            UPDATE users SET role='admin', is_active=true, updated_at=NOW()
            WHERE email='admin@admin.com';
        ");

        // Inicializar permisos completos para admins (INSERT o UPDATE a true si ya existen)
        await db.Database.ExecuteSqlRawAsync(@"
            INSERT INTO user_permissions (user_id, page, can_access, updated_at)
            SELECT u.id, p.page, true, NOW()
            FROM users u
            CROSS JOIN (VALUES
                ('dashboard'), ('recipes'), ('ingredients'), ('inventory'),
                ('orders'), ('menu-items'), ('cash-registers'), ('work-shifts'),
                ('customers'), ('expenses'), ('equipments')
            ) AS p(page)
            WHERE u.role = 'admin'
            ON CONFLICT (user_id, page) DO UPDATE
                SET can_access = true, updated_at = NOW();
        ");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "No se pudo aplicar patch de startup para user_permissions");
    }
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FreshDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("StartupDbPatch");

    try
    {
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ingredient_products (
                id              SERIAL PRIMARY KEY,
                ingredient_id   INTEGER         NOT NULL REFERENCES ingredients(id) ON DELETE CASCADE,
                product_id      INTEGER         NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
                quantity        NUMERIC(10,2)   NOT NULL CHECK (quantity > 0),
                created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                CONSTRAINT ux_ingredient_products_ingredient_product UNIQUE (ingredient_id, product_id)
            );

            CREATE INDEX IF NOT EXISTS ix_ingredient_products_ingredient_id ON ingredient_products (ingredient_id);
            CREATE INDEX IF NOT EXISTS ix_ingredient_products_product_id ON ingredient_products (product_id);
        ");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "No se pudo aplicar patch de startup para ingredient_products");
    }
}

// Swagger habilitado siempre (app interna)
app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();

app.UseCors("CorsPolicy"); // Asegúrate de llamar a la política ANTES de Authentication y Authorization

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ApiLoggingMiddleware>();
app.MapControllers();
app.MapHub<Fresh.Api.Hubs.PresenceHub>("/hubs/presence");
app.Run();
