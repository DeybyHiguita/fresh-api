using Fresh.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Data;

public class FreshDbContext : DbContext
{
    public FreshDbContext(DbContextOptions<FreshDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<IngredientProduct> IngredientProducts => Set<IngredientProduct>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<Log> Logs => Set<Log>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<PurchaseBatch> PurchaseBatches => Set<PurchaseBatch>();
    public DbSet<PurchaseDetail> PurchaseDetails => Set<PurchaseDetail>();
    public DbSet<WorkShift> WorkShifts => Set<WorkShift>();
    public DbSet<BreakTime> BreakTimes => Set<BreakTime>();
    public DbSet<DailyWorkedHours> DailyWorkedHours => Set<DailyWorkedHours>();
    public DbSet<MenuItem> MenuItems { get; set; }
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<ExpenseType> ExpenseTypes => Set<ExpenseType>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<CashPeriod> CashPeriods => Set<CashPeriod>();
    public DbSet<CashRegister> CashRegisters => Set<CashRegister>();
    public DbSet<EquipmentCategory> EquipmentCategories => Set<EquipmentCategory>();
    public DbSet<Equipment> Equipments => Set<Equipment>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invoice>(entity =>
{
    entity.ToTable("invoices");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
    
    // Relación y Unique Constraints
    entity.Property(e => e.OrderId).HasColumnName("order_id").IsRequired();
    entity.HasIndex(e => e.OrderId).HasDatabaseName("ix_invoices_order_id").IsUnique();
    
    entity.Property(e => e.InvoiceNumber).HasColumnName("invoice_number").HasMaxLength(50).IsRequired();
    entity.HasIndex(e => e.InvoiceNumber).HasDatabaseName("ix_invoices_invoice_number").IsUnique();

    entity.Property(e => e.CustomerDocument).HasColumnName("customer_document").HasMaxLength(20);
    entity.Property(e => e.CustomerName).HasColumnName("customer_name").HasMaxLength(150);
    
    // Campos Monetarios
    entity.Property(e => e.Subtotal).HasColumnName("subtotal").HasPrecision(10, 2).IsRequired();
    entity.Property(e => e.TaxAmount).HasColumnName("tax_amount").HasPrecision(10, 2).HasDefaultValue(0m);
    entity.Property(e => e.DiscountAmount).HasColumnName("discount_amount").HasPrecision(10, 2).HasDefaultValue(0m);
    entity.Property(e => e.TotalAmount).HasColumnName("total_amount").HasPrecision(10, 2).IsRequired();
    entity.Property(e => e.CashTendered).HasColumnName("cash_tendered").HasPrecision(10, 2).HasDefaultValue(0m);
    entity.Property(e => e.ChangeAmount).HasColumnName("change_amount").HasPrecision(10, 2).HasDefaultValue(0m);
    
    entity.Property(e => e.PaymentMethod).HasColumnName("payment_method").HasMaxLength(50).IsRequired();
    
    // Auditoría (la tabla usa issued_at, no created_at)
    entity.Property(e => e.CreatedAt).HasColumnName("issued_at").HasDefaultValueSql("NOW()");
    entity.Ignore(e => e.UpdatedAt);

    // Relación 1 a 1 con Order
    entity.HasOne(e => e.Order)
          .WithOne() // Asumiendo que agregas "public Invoice? Invoice { get; set; }" en la clase Order
          .HasForeignKey<Invoice>(e => e.OrderId)
          .OnDelete(DeleteBehavior.Restrict);
});
        


modelBuilder.Entity<ExpenseType>(entity =>
{
    entity.ToTable("expense_types");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
    
    entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
    entity.Property(e => e.Description).HasColumnName("description");
    entity.Property(e => e.ExpectedAmount).HasColumnName("expected_amount").HasPrecision(10, 2).HasDefaultValue(0m);
    entity.Property(e => e.Frequency).HasColumnName("frequency").HasMaxLength(50).HasDefaultValue("Mensual");
    entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
    
    entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
    entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
    
    entity.HasIndex(e => e.Name).HasDatabaseName("ix_expense_types_name");
});

modelBuilder.Entity<Expense>(entity =>
{
    entity.ToTable("expenses");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
    
    entity.Property(e => e.ExpenseTypeId).HasColumnName("expense_type_id").IsRequired();
    entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
    
    entity.Property(e => e.AmountPaid).HasColumnName("amount_paid").HasPrecision(10, 2).IsRequired();
    entity.Property(e => e.PaymentDate).HasColumnName("payment_date").IsRequired();
    entity.Property(e => e.PaymentMethod).HasColumnName("payment_method").HasMaxLength(50).HasDefaultValue("Efectivo");
    entity.Property(e => e.Notes).HasColumnName("notes");
    
    entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
    entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

    entity.HasIndex(e => e.ExpenseTypeId).HasDatabaseName("ix_expenses_expense_type_id");
    entity.HasIndex(e => e.UserId).HasDatabaseName("ix_expenses_user_id");
    entity.HasIndex(e => e.PaymentDate).HasDatabaseName("ix_expenses_payment_date");

    // Relaciones
    entity.HasOne(e => e.ExpenseType)
          .WithMany(t => t.Expenses)
          .HasForeignKey(e => e.ExpenseTypeId)
          .OnDelete(DeleteBehavior.Restrict);

    entity.HasOne(e => e.User)
          .WithMany()
          .HasForeignKey(e => e.UserId)
          .OnDelete(DeleteBehavior.Restrict);
});

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(150).IsRequired();
            entity.Property(e => e.Password).HasColumnName("password").IsRequired();
            entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(20).HasDefaultValue("employee");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        });

        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.ToTable("recipes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
            entity.Property(e => e.Instructions).HasColumnName("instructions");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
            entity.HasOne(e => e.Category).WithMany(c => c.Recipes).HasForeignKey(e => e.CategoryId);
        });

        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.ToTable("ingredients");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Unit).HasColumnName("unit").HasMaxLength(20).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        });

        modelBuilder.Entity<RecipeIngredient>(entity =>
        {
            entity.ToTable("recipe_ingredients");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RecipeId).HasColumnName("recipe_id");
            entity.Property(e => e.IngredientId).HasColumnName("ingredient_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity").HasPrecision(10, 2);
            entity.Property(e => e.Unit).HasColumnName("unit").HasMaxLength(20).IsRequired();
            entity.HasOne(e => e.Recipe).WithMany(r => r.RecipeIngredients).HasForeignKey(e => e.RecipeId);
            entity.HasOne(e => e.Ingredient).WithMany(i => i.RecipeIngredients).HasForeignKey(e => e.IngredientId);
        });

        modelBuilder.Entity<IngredientProduct>(entity =>
        {
            entity.ToTable("ingredient_products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.IngredientId).HasColumnName("ingredient_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity").HasPrecision(10, 2);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
            entity.HasOne(e => e.Ingredient)
                  .WithMany(i => i.IngredientProducts)
                  .HasForeignKey(e => e.IngredientId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product)
                  .WithMany(p => p.IngredientProducts)
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.IngredientId).HasDatabaseName("ix_ingredient_products_ingredient_id");
            entity.HasIndex(e => e.ProductId).HasDatabaseName("ix_ingredient_products_product_id");
            entity.HasIndex(e => new { e.IngredientId, e.ProductId }).IsUnique().HasDatabaseName("ux_ingredient_products_ingredient_product");
        });

        modelBuilder.Entity<Log>(entity =>
        {
            entity.ToTable("logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.TransactionId).HasColumnName("transaction_id").HasMaxLength(100).IsRequired();
            entity.Property(e => e.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
            entity.Property(e => e.LogDate).HasColumnName("log_date").HasDefaultValueSql("NOW()");
            entity.Property(e => e.LogLevel).HasColumnName("log_level").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Operation).HasColumnName("operation").HasMaxLength(100);
            entity.Property(e => e.EntityName).HasColumnName("entity_name").HasMaxLength(100);
            entity.Property(e => e.EntityId).HasColumnName("entity_id").HasMaxLength(100);
            entity.Property(e => e.UserId).HasColumnName("user_id").HasMaxLength(100);
            entity.Property(e => e.TransactionStatus).HasColumnName("transaction_status").HasMaxLength(30);
            entity.Property(e => e.DurationMs).HasColumnName("duration_ms");
            entity.Property(e => e.Logger).HasColumnName("logger").HasMaxLength(255);
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.Exception).HasColumnName("exception");
            entity.Property(e => e.TransactionData).HasColumnName("transaction_data");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
            entity.Property(e => e.UnitMeasure).HasColumnName("unit_measure").HasMaxLength(50).IsRequired();
            entity.Property(e => e.CurrentStock).HasColumnName("current_stock").HasPrecision(10, 2).HasDefaultValue(0m);
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
            entity.HasIndex(e => e.Name).HasDatabaseName("ix_products_name");
        });

        modelBuilder.Entity<PurchaseBatch>(entity =>
        {
            entity.ToTable("purchase_batches");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.BatchName).HasColumnName("batch_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
            entity.HasIndex(e => new { e.StartDate, e.EndDate }).HasDatabaseName("ix_purchase_batches_dates");
        });

        modelBuilder.Entity<PurchaseDetail>(entity =>
        {
            entity.ToTable("purchase_details");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.BatchId).HasColumnName("batch_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity").HasPrecision(10, 2);
            entity.Property(e => e.TotalValue).HasColumnName("total_value").HasPrecision(12, 2);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
            entity.HasOne(e => e.Batch)
                  .WithMany(b => b.PurchaseDetails)
                  .HasForeignKey(e => e.BatchId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product)
                  .WithMany(p => p.PurchaseDetails)
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.BatchId).HasDatabaseName("ix_purchase_details_batch_id");
            entity.HasIndex(e => e.ProductId).HasDatabaseName("ix_purchase_details_product_id");
        });

        modelBuilder.Entity<WorkShift>(entity =>
        {
            entity.ToTable("work_shifts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ShiftDate).HasColumnName("shift_date").HasDefaultValueSql("CURRENT_DATE");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.TotalHours).HasColumnName("total_hours").HasPrecision(5, 2).HasDefaultValue(0m);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.UserId).HasDatabaseName("ix_work_shifts_user_id");
            entity.HasIndex(e => e.ShiftDate).HasDatabaseName("ix_work_shifts_date");
        });

        modelBuilder.Entity<BreakTime>(entity =>
        {
            entity.ToTable("break_times");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.ShiftId).HasColumnName("shift_id");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
            entity.HasOne(e => e.Shift)
                  .WithMany(s => s.BreakTimes)
                  .HasForeignKey(e => e.ShiftId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.ShiftId).HasDatabaseName("ix_break_times_shift_id");
        });

        // Vista vw_daily_worked_hours � entidad sin clave (solo lectura)
        modelBuilder.Entity<DailyWorkedHours>(entity =>
        {
            entity.ToView("vw_daily_worked_hours");
            entity.HasNoKey();
            entity.Property(e => e.ShiftId).HasColumnName("shift_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.UserName).HasColumnName("user_name");
            entity.Property(e => e.ShiftDate).HasColumnName("shift_date");
            entity.Property(e => e.ShiftStart).HasColumnName("shift_start");
            entity.Property(e => e.ShiftEnd).HasColumnName("shift_end");
            entity.Property(e => e.GrossHours).HasColumnName("gross_hours").HasPrecision(10, 4);
            entity.Property(e => e.TotalBreakHours).HasColumnName("total_break_hours").HasPrecision(10, 4);
            entity.Property(e => e.NetWorkedHours).HasColumnName("net_worked_hours").HasPrecision(10, 4);
        });

        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.ToTable("menu_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Category).HasColumnName("category").HasMaxLength(100).IsRequired();
            entity.Property(e => e.PreparationCost).HasColumnName("preparation_cost").HasPrecision(10, 2);
            entity.Property(e => e.SalePrice).HasColumnName("sale_price").HasPrecision(10, 2);
            entity.Property(e => e.IsAvailable).HasColumnName("is_available").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();

            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(e => e.CustomerName).HasColumnName("customer_name").HasMaxLength(150);
            entity.Property(e => e.CustomerPhone).HasColumnName("customer_phone").HasMaxLength(20);

            entity.Property(e => e.Subtotal).HasColumnName("subtotal").HasPrecision(10, 2).IsRequired();
            entity.Property(e => e.Discount).HasColumnName("discount").HasPrecision(10, 2).HasDefaultValue(0m);
            entity.Property(e => e.Total).HasColumnName("total").HasPrecision(10, 2).IsRequired();

            entity.Property(e => e.OrderType).HasColumnName("order_type").HasMaxLength(50).HasDefaultValue("Local");
            entity.Property(e => e.PaymentMethod).HasColumnName("payment_method").HasMaxLength(50).HasDefaultValue("Efectivo");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).HasDefaultValue("Completada");

            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

            entity.HasIndex(e => e.UserId).HasDatabaseName("ix_orders_user_id");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("ix_orders_created_at");

            // Relaci�n
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();

            entity.Property(e => e.OrderId).HasColumnName("order_id").IsRequired();
            entity.Property(e => e.MenuItemId).HasColumnName("menu_item_id").IsRequired();

            entity.Property(e => e.Quantity).HasColumnName("quantity").IsRequired().HasDefaultValue(1);
            entity.Property(e => e.UnitPrice).HasColumnName("unit_price").HasPrecision(10, 2).IsRequired();
            entity.Property(e => e.Subtotal).HasColumnName("subtotal").HasPrecision(10, 2).IsRequired();
            entity.Property(e => e.ItemNotes).HasColumnName("item_notes").HasMaxLength(255);

            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

            entity.HasIndex(e => e.OrderId).HasDatabaseName("ix_order_items_order_id");
            entity.HasIndex(e => e.MenuItemId).HasDatabaseName("ix_order_items_menu_item_id");

            // Relaciones
            entity.HasOne(e => e.Order)
                  .WithMany(o => o.OrderItems)
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.MenuItem)
                  .WithMany()
                  .HasForeignKey(e => e.MenuItemId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CashPeriod>(entity =>
        {
            entity.ToTable("cash_periods");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.StartDate).HasColumnName("start_date").IsRequired();
            entity.Property(e => e.EndDate).HasColumnName("end_date").IsRequired();
            entity.Property(e => e.IsClosed).HasColumnName("is_closed").HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
            entity.HasIndex(e => new { e.StartDate, e.EndDate }).HasDatabaseName("ix_cash_periods_dates");
        });

        modelBuilder.Entity<CashRegister>(entity =>
        {
            entity.ToTable("cash_registers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.PeriodId).HasColumnName("period_id").IsRequired();
            entity.Property(e => e.OpenedById).HasColumnName("opened_by").IsRequired();
            entity.Property(e => e.ClosedById).HasColumnName("closed_by");
            entity.Property(e => e.OpeningTime).HasColumnName("opening_time").IsRequired();
            entity.Property(e => e.ClosingTime).HasColumnName("closing_time");
            entity.Property(e => e.OpeningBalance).HasColumnName("opening_balance").HasPrecision(10, 2).IsRequired();
            entity.Property(e => e.ReportedCash).HasColumnName("reported_cash").HasPrecision(10, 2);
            entity.Property(e => e.ReportedTransfer).HasColumnName("reported_transfer").HasPrecision(10, 2);
            entity.Property(e => e.ReportedCard).HasColumnName("reported_card").HasPrecision(10, 2);
            entity.Property(e => e.SystemCash).HasColumnName("system_cash").HasPrecision(10, 2);
            entity.Property(e => e.SystemTransfer).HasColumnName("system_transfer").HasPrecision(10, 2);
            entity.Property(e => e.SystemCard).HasColumnName("system_card").HasPrecision(10, 2);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("Abierta");
            entity.Property(e => e.Observations).HasColumnName("observations");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
            entity.HasIndex(e => e.PeriodId).HasDatabaseName("ix_cash_registers_period_id");
            entity.HasIndex(e => e.Status).HasDatabaseName("ix_cash_registers_status");
            entity.HasOne(e => e.Period).WithMany(p => p.CashRegisters).HasForeignKey(e => e.PeriodId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.OpenedBy).WithMany().HasForeignKey(e => e.OpenedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ClosedBy).WithMany().HasForeignKey(e => e.ClosedById).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EquipmentCategory>(entity =>
        {
            entity.ToTable("equipment_categories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
            entity.HasIndex(e => e.Name).HasDatabaseName("ix_equipment_categories_name");
        });

        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.ToTable("equipments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.CategoryId).HasColumnName("category_id").IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
            entity.Property(e => e.Brand).HasColumnName("brand").HasMaxLength(100);
            entity.Property(e => e.Model).HasColumnName("model").HasMaxLength(100);
            entity.Property(e => e.SerialNumber).HasColumnName("serial_number").HasMaxLength(100);
            entity.Property(e => e.PurchaseDate).HasColumnName("purchase_date");
            entity.Property(e => e.PurchasePrice).HasColumnName("purchase_price").HasPrecision(10, 2).HasDefaultValue(0m);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).HasDefaultValue("Activo");
            entity.Property(e => e.Location).HasColumnName("location").HasMaxLength(100);
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
            entity.HasIndex(e => e.CategoryId).HasDatabaseName("ix_equipments_category_id");
            entity.HasIndex(e => e.Status).HasDatabaseName("ix_equipments_status");
            entity.HasIndex(e => e.SerialNumber).HasDatabaseName("ix_equipments_serial_number");
            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Equipments)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
