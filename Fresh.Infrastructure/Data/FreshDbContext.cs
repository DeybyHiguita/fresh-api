using Fresh.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Data;

public class FreshDbContext : DbContext
{
    public FreshDbContext(DbContextOptions<FreshDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeDetail> RecipeDetails => Set<RecipeDetail>();
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
    public DbSet<MenuItemVariant> MenuItemVariants => Set<MenuItemVariant>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<ExpenseType> ExpenseTypes => Set<ExpenseType>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<CashPeriod> CashPeriods => Set<CashPeriod>();
    public DbSet<CashRegister> CashRegisters => Set<CashRegister>();
    public DbSet<EquipmentCategory> EquipmentCategories => Set<EquipmentCategory>();
    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerCredit> CustomerCredits => Set<CustomerCredit>();
    public DbSet<CreditTransaction> CreditTransactions => Set<CreditTransaction>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<AppPage> AppPages => Set<AppPage>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<UserAction> UserActions => Set<UserAction>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<WhatsappContact> WhatsappContacts => Set<WhatsappContact>();
    public DbSet<WhatsappMessage> WhatsappMessages => Set<WhatsappMessage>();
    public DbSet<Safe> Safes => Set<Safe>();
    public DbSet<SafeTransaction> SafeTransactions => Set<SafeTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.ToTable("app_settings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.Key).HasColumnName("key").HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Key).IsUnique();
            entity.Property(e => e.Value).HasColumnName("value").HasMaxLength(500);
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        });

        modelBuilder.Entity<WhatsappContact>(entity =>
        {
            entity.ToTable("whatsapp_contacts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.WaId).HasColumnName("wa_id").HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.WaId).IsUnique();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(150);
            entity.Property(e => e.LastMessageAt).HasColumnName("last_message_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UnreadCount).HasColumnName("unread_count").HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        });

        modelBuilder.Entity<WhatsappMessage>(entity =>
        {
            entity.ToTable("whatsapp_messages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.ContactId).HasColumnName("contact_id").IsRequired();
            entity.Property(e => e.Direction).HasColumnName("direction").HasMaxLength(3).IsRequired();
            entity.Property(e => e.Body).HasColumnName("body").IsRequired();
            entity.Property(e => e.WaMessageId).HasColumnName("wa_message_id").HasMaxLength(200);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("sent");
            entity.Property(e => e.MediaType).HasColumnName("media_type").HasMaxLength(20);
            entity.Property(e => e.MediaId).HasColumnName("media_id").HasMaxLength(200);
            entity.Property(e => e.MediaName).HasColumnName("media_name").HasMaxLength(255);
            entity.Property(e => e.ReplyToWaMessageId).HasColumnName("reply_to_wa_message_id").HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.HasOne(e => e.Contact)
                  .WithMany(c => c.Messages)
                  .HasForeignKey(e => e.ContactId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

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

        modelBuilder.Entity<AppPage>(entity =>
        {
            entity.ToTable("app_pages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Route).HasColumnName("route").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Icon).HasColumnName("icon").HasMaxLength(50);
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

            entity.HasIndex(e => e.Route).HasDatabaseName("ix_app_pages_route").IsUnique();
        });

        modelBuilder.Entity<UserPermission>(entity =>
        {
            entity.ToTable("user_permissions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(e => e.PageId).HasColumnName("page_id").IsRequired();
            entity.Property(e => e.CanAccess).HasColumnName("can_access").HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

            entity.HasIndex(e => e.UserId).HasDatabaseName("ix_user_permissions_user_id");
            entity.HasIndex(e => e.PageId).HasDatabaseName("ix_user_permissions_page_id");
            entity.HasIndex(e => new { e.UserId, e.PageId }).HasDatabaseName("ux_user_permissions_user_page").IsUnique();

            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Page).WithMany(p => p.UserPermissions).HasForeignKey(e => e.PageId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();

            entity.Property(e => e.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.DocumentNumber).HasColumnName("document_number").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20);
            entity.Property(e => e.Address).HasColumnName("address").HasMaxLength(255);

            entity.Property(e => e.ReferenceName).HasColumnName("reference_name").HasMaxLength(150);
            entity.Property(e => e.ReferencePhone).HasColumnName("reference_phone").HasMaxLength(20);

            entity.Property(e => e.CreatedById).HasColumnName("created_by").IsRequired();
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);

            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

            entity.HasIndex(e => e.DocumentNumber).HasDatabaseName("ix_customers_document").IsUnique();
            entity.HasIndex(e => new { e.FirstName, e.LastName }).HasDatabaseName("ix_customers_name");

            entity.HasOne(e => e.CreatedBy).WithMany().HasForeignKey(e => e.CreatedById).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CustomerCredit>(entity =>
        {
            entity.ToTable("customer_credits");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();

            entity.Property(e => e.CustomerId).HasColumnName("customer_id").IsRequired();

            entity.Property(e => e.CreditLimit).HasColumnName("credit_limit").HasPrecision(10, 2).HasDefaultValue(0m);
            entity.Property(e => e.PaymentFrequency).HasColumnName("payment_frequency").HasMaxLength(50).HasDefaultValue("Mensual");
            entity.Property(e => e.CurrentBalance).HasColumnName("current_balance").HasPrecision(10, 2).HasDefaultValue(0m);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).HasDefaultValue("Al día");

            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

            entity.HasIndex(e => e.CustomerId).HasDatabaseName("ix_customer_credits_customer_id").IsUnique();

            entity.HasOne(e => e.Customer).WithOne(c => c.CreditInfo).HasForeignKey<CustomerCredit>(e => e.CustomerId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CreditTransaction>(entity =>
        {
            entity.ToTable("credit_transactions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();

            entity.Property(e => e.CustomerCreditId).HasColumnName("customer_credit_id").IsRequired();
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(20).HasDefaultValue("Cargo");
            entity.Property(e => e.Amount).HasColumnName("amount").HasPrecision(10, 2).IsRequired();
            entity.Property(e => e.BalanceBefore).HasColumnName("balance_before").HasPrecision(10, 2).IsRequired();
            entity.Property(e => e.BalanceAfter).HasColumnName("balance_after").HasPrecision(10, 2).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(300);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

            entity.HasIndex(e => e.CustomerCreditId).HasDatabaseName("ix_credit_tx_credit_id");
            entity.HasIndex(e => e.OrderId).HasDatabaseName("ix_credit_tx_order_id");

            entity.HasOne(e => e.CustomerCredit)
                  .WithMany()
                  .HasForeignKey(e => e.CustomerCreditId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Order)
                  .WithMany()
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .IsRequired(false);
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
            entity.Property(e => e.PurchaseBatchId).HasColumnName("purchase_batch_id");

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

            entity.HasOne(e => e.PurchaseBatch)
                  .WithMany()
                  .HasForeignKey(e => e.PurchaseBatchId)
                  .OnDelete(DeleteBehavior.SetNull);
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

        // ...
        modelBuilder.Entity<RecipeDetail>(entity =>
        {
            entity.ToTable("recipe_details");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.RecipeId).HasColumnName("recipe_id").IsRequired();
            entity.Property(e => e.IngredientId).HasColumnName("ingredient_id").IsRequired(false);
            entity.Property(e => e.ProductId).HasColumnName("product_id").IsRequired(false);
            entity.Property(e => e.Quantity).HasColumnName("quantity").HasPrecision(10, 2);
            entity.Property(e => e.Unit).HasColumnName("unit").HasMaxLength(20).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

            entity.HasOne<Recipe>()
                  .WithMany(r => r.Details)
                  .HasForeignKey(d => d.RecipeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Ingredient)
                  .WithMany()
                  .HasForeignKey(d => d.IngredientId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.Product)
                  .WithMany()
                  .HasForeignKey(d => d.ProductId)
                  .OnDelete(DeleteBehavior.SetNull);
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
            entity.Property(e => e.UnitPrice).HasColumnName("unit_price").HasPrecision(12, 4).HasDefaultValue(0m);
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
            entity.Property(e => e.ImgUrl).HasColumnName("img_url");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();

            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.CustomerName).HasColumnName("customer_name").HasMaxLength(150);
            entity.Property(e => e.CustomerPhone).HasColumnName("customer_phone").HasMaxLength(20);

            entity.Property(e => e.Subtotal).HasColumnName("subtotal").HasPrecision(10, 2).IsRequired();
            entity.Property(e => e.Discount).HasColumnName("discount").HasPrecision(10, 2).HasDefaultValue(0m);
            entity.Property(e => e.Total).HasColumnName("total").HasPrecision(10, 2).IsRequired();

            entity.Property(e => e.OrderType).HasColumnName("order_type").HasMaxLength(50).HasDefaultValue("Local");
            entity.Property(e => e.PaymentMethod).HasColumnName("payment_method").HasMaxLength(50).HasDefaultValue("Efectivo");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).HasDefaultValue("Completada");

            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.IsCreditPaid).HasColumnName("is_credit_paid").HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

            entity.HasIndex(e => e.UserId).HasDatabaseName("ix_orders_user_id");
            entity.HasIndex(e => e.CustomerId).HasDatabaseName("ix_orders_customer_id");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("ix_orders_created_at");

            // Relaci�n
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Customer)
                  .WithMany()
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .IsRequired(false);
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
            entity.Property(e => e.AmountToSafe).HasColumnName("amount_to_safe").HasPrecision(12, 2).HasDefaultValue(0m);
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

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.ToTable("user_sessions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(e => e.ConnectionId).HasColumnName("connection_id").HasMaxLength(200);
            entity.Property(e => e.ConnectedAt).HasColumnName("connected_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.DisconnectedAt).HasColumnName("disconnected_at");
            entity.Property(e => e.TotalIdleSeconds).HasColumnName("total_idle_seconds").HasDefaultValue(0);
            entity.Property(e => e.LastKnownLocation).HasColumnName("last_known_location").HasMaxLength(200);
            entity.Property(e => e.IsOnline).HasColumnName("is_online").HasDefaultValue(true);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.UserId).HasDatabaseName("ix_user_sessions_user_id");
            entity.HasIndex(e => e.IsOnline).HasDatabaseName("ix_user_sessions_is_online");
        });

        modelBuilder.Entity<UserAction>(entity =>
        {
            entity.ToTable("user_actions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.SessionId).HasColumnName("session_id").IsRequired();
            entity.Property(e => e.ActionType).HasColumnName("action_type").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.HasOne(e => e.Session)
                  .WithMany(s => s.Actions)
                  .HasForeignKey(e => e.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.SessionId).HasDatabaseName("ix_user_actions_session_id");
        });

        modelBuilder.Entity<MenuItemVariant>(entity =>
        {
            entity.ToTable("menu_item_variants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.MenuItemId).HasColumnName("menu_item_id").IsRequired();
            entity.Property(e => e.VariantName).HasColumnName("variant_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.SalePrice).HasColumnName("sale_price").HasPrecision(10, 2).IsRequired();
            entity.Property(e => e.PreparationCost).HasColumnName("preparation_cost").HasPrecision(10, 2).HasDefaultValue(0m);
            entity.Property(e => e.IsAvailable).HasColumnName("is_available").HasDefaultValue(true);
            entity.Property(e => e.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
            entity.HasIndex(e => e.MenuItemId).HasDatabaseName("ix_menu_item_variants_menu_item_id");
            entity.HasOne(e => e.MenuItem)
                  .WithMany(m => m.Variants)
                  .HasForeignKey(e => e.MenuItemId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Safe>(entity =>
        {
            entity.ToTable("safe");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.Balance).HasColumnName("balance").HasPrecision(12, 2).HasDefaultValue(0m);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        });

        modelBuilder.Entity<SafeTransaction>(entity =>
        {
            entity.ToTable("safe_transactions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Amount).HasColumnName("amount").HasPrecision(12, 2).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").IsRequired();
            entity.Property(e => e.BalanceBefore).HasColumnName("balance_before").HasPrecision(12, 2);
            entity.Property(e => e.BalanceAfter).HasColumnName("balance_after").HasPrecision(12, 2);
            entity.Property(e => e.CashRegisterId).HasColumnName("cash_register_id");
            entity.Property(e => e.CreatedById).HasColumnName("created_by_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.HasIndex(e => e.Type).HasDatabaseName("idx_safe_transactions_type");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("idx_safe_transactions_created_at");
            entity.HasOne(e => e.CashRegister).WithMany().HasForeignKey(e => e.CashRegisterId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
            entity.HasOne(e => e.CreatedBy).WithMany().HasForeignKey(e => e.CreatedById).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
        });
    }
}
