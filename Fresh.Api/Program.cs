using Fresh.Api.Hubs;
using Fresh.Api.Middleware;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Fresh.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
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
builder.Services.AddScoped<ICustomerCreditService, CustomerCreditService>();
builder.Services.AddScoped<IAppPageService, AppPageService>();
builder.Services.AddScoped<IUserPermissionService, UserPermissionService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IUserSessionService, UserSessionService>();

// JWT Authentication
builder.Services.AddSignalR();

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
        // Allow JWT from query string for SignalR connections
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Controllers + Swagger
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
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
// CORS (para Angular en desarrollo y Docker)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // 👈 Permite cualquier origen dinámicamente
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();           // 👈 Vuelve a habilitar las credenciales
    });
});

var app = builder.Build();

// 👇 --- INICIO DEL RASTREADOR --- 👇
app.Use(async (context, next) =>
{
    // Ignorar el spam de preflights (OPTIONS) para ver los GET/POST reales
    if (context.Request.Method == "OPTIONS") 
    {
        await next(context);
        return;
    }

    Console.WriteLine($"[---> LLEGÓ A C#] {context.Request.Method} {context.Request.Path}");
    var watch = System.Diagnostics.Stopwatch.StartNew();
    
    try
    {
        await next(context);
        watch.Stop();
        Console.WriteLine($"[<--- RESPONDIÓ C#] {context.Request.Method} {context.Request.Path} en {watch.ElapsedMilliseconds}ms (Status: {context.Response.StatusCode})");
    }
    catch (Exception ex)
    {
        watch.Stop();
        Console.WriteLine($"[!!! EXPLOTÓ C#] {context.Request.Method} {context.Request.Path} - ERROR: {ex.Message}");
        throw;
    }
});
// 👆 --- FIN DEL RASTREADOR --- 👆

app.UseRouting();
// ... resto de tu código

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FreshDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("StartupDbPatch");

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

// Startup patch: user_sessions + user_actions
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FreshDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("StartupDbPatch");
    try
    {
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS user_sessions (
                id                    SERIAL PRIMARY KEY,
                user_id               INTEGER       NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                connection_id         VARCHAR(200)  NULL,
                connected_at          TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
                disconnected_at       TIMESTAMPTZ   NULL,
                total_idle_seconds    INTEGER       NOT NULL DEFAULT 0,
                last_known_location   VARCHAR(200)  NULL,
                is_online             BOOLEAN       NOT NULL DEFAULT TRUE
            );
            CREATE INDEX IF NOT EXISTS ix_user_sessions_user_id  ON user_sessions (user_id);
            CREATE INDEX IF NOT EXISTS ix_user_sessions_is_online ON user_sessions (is_online);

            CREATE TABLE IF NOT EXISTS user_actions (
                id           SERIAL PRIMARY KEY,
                session_id   INTEGER       NOT NULL REFERENCES user_sessions(id) ON DELETE CASCADE,
                action_type  VARCHAR(50)   NOT NULL,
                description  VARCHAR(500)  NOT NULL,
                created_at   TIMESTAMPTZ   NOT NULL DEFAULT NOW()
            );
            CREATE INDEX IF NOT EXISTS ix_user_actions_session_id ON user_actions (session_id);
        ");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "No se pudo aplicar patch de startup para user_sessions/user_actions");
    }
}

// Swagger habilitado siempre (app interna)
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ApiLoggingMiddleware>();
app.MapControllers();
app.MapHub<PresenceHub>("/hubs/presence");

app.Run();
