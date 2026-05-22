using Fresh.Core.DTOs.Employee;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Fresh.Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private readonly FreshDbContext _context;

    public EmployeeService(FreshDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<EmployeeResponse>> GetAllAsync()
    {
        var employees = await _context.Employees
            .Include(e => e.User)
            .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
            .ToListAsync();

        return employees.Select(MapToResponse);
    }

    public async Task<IEnumerable<EmployeeResponse>> GetActiveAsync()
    {
        var employees = await _context.Employees
            .Include(e => e.User)
            .Where(e => e.IsActive)
            .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
            .ToListAsync();

        return employees.Select(MapToResponse);
    }

    public async Task<EmployeeResponse?> GetByIdAsync(int id)
    {
        var employee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id);

        return employee is not null ? MapToResponse(employee) : null;
    }

    public async Task<EmployeeResponse?> GetByUserIdAsync(int userId)
    {
        var employee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.UserId == userId);

        return employee is not null ? MapToResponse(employee) : null;
    }

    public async Task<EmployeeResponse?> GetByDocumentAsync(string documentType, string documentNumber)
    {
        var employee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.DocumentType == documentType && e.DocumentNumber == documentNumber);

        return employee is not null ? MapToResponse(employee) : null;
    }

    public async Task<EmployeeResponse> CreateAsync(EmployeeRequest request)
    {
        // Validar documento único
        var exists = await _context.Employees.AnyAsync(e => 
            e.DocumentType == request.DocumentType && 
            e.DocumentNumber == request.DocumentNumber);
        
        if (exists)
            throw new InvalidOperationException("Ya existe un empleado con ese documento");

        // Resolver UserId
        int? resolvedUserId = request.UserId;

        if (request.CreateUser)
        {
            // Crear nuevo usuario para este empleado
            var email = request.Email ?? request.PersonalEmail;
            if (string.IsNullOrWhiteSpace(email))
                throw new InvalidOperationException("Se requiere un email para crear el usuario");
            if (string.IsNullOrWhiteSpace(request.UserPassword))
                throw new InvalidOperationException("Se requiere una contraseña para crear el usuario");
            if (request.UserPassword.Length < 6)
                throw new InvalidOperationException("La contraseña debe tener al menos 6 caracteres");

            var emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
            if (emailExists)
                throw new InvalidOperationException("Ya existe un usuario con ese email");

            string hashedPassword;
            try
            {
                hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.UserPassword);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al procesar la contraseña: {ex.Message}");
            }

            var newUser = new User
            {
                Name = $"{request.FirstName} {request.LastName}".Trim(),
                Email = email.ToLower().Trim(),
                Password = hashedPassword,
                Role = request.UserRole ?? "employee",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            resolvedUserId = newUser.Id;
        }
        else if (resolvedUserId.HasValue)
        {
            // Validar que el usuario exista
            var userExists = await _context.Users.AnyAsync(u => u.Id == resolvedUserId.Value);
            if (!userExists)
                throw new InvalidOperationException("El usuario seleccionado no existe");

            // Validar que no esté vinculado a otro empleado
            var alreadyLinked = await _context.Employees.AnyAsync(e => e.UserId == resolvedUserId.Value);
            if (alreadyLinked)
                throw new InvalidOperationException("Este usuario ya está vinculado a un empleado");
        }

        var emailToStore = request.PersonalEmail ?? request.Email;

        var employee = new Employee
        {
            UserId = resolvedUserId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            DocumentType = request.DocumentType,
            DocumentNumber = request.DocumentNumber,
            BirthDate = request.BirthDate.HasValue 
                ? DateTime.SpecifyKind(request.BirthDate.Value, DateTimeKind.Utc) 
                : null,
            Gender = request.Gender,
            MaritalStatus = request.MaritalStatus,
            BloodType = request.BloodType,
            Phone = request.Phone,
            Mobile = request.Mobile,
            PersonalEmail = emailToStore,
            EmergencyContactName = request.EmergencyContactName,
            EmergencyContactPhone = request.EmergencyContactPhone,
            Address = request.Address,
            City = request.City,
            Department = request.Department,
            Neighborhood = request.Neighborhood,
            PostalCode = request.PostalCode,
            Position = request.Position,
            HireDate = request.HireDate.HasValue 
                ? DateTime.SpecifyKind(request.HireDate.Value, DateTimeKind.Utc) 
                : null,
            ContractType = request.ContractType,
            Salary = request.Salary,
            PaymentFrequency = request.PaymentFrequency,
            BankName = request.BankName,
            BankAccountType = request.BankAccountType,
            BankAccountNumber = request.BankAccountNumber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        // Recargar con relaciones
        await _context.Entry(employee).Reference(e => e.User).LoadAsync();

        return MapToResponse(employee);
    }

    public async Task<EmployeeResponse?> UpdateAsync(int id, EmployeeRequest request)
    {
        var employee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee is null) return null;

        // Validar documento único (si cambió)
        if (employee.DocumentType != request.DocumentType || employee.DocumentNumber != request.DocumentNumber)
        {
            var exists = await _context.Employees.AnyAsync(e => 
                e.Id != id &&
                e.DocumentType == request.DocumentType && 
                e.DocumentNumber == request.DocumentNumber);
            
            if (exists)
                throw new InvalidOperationException("Ya existe un empleado con ese documento");
        }

        // El `UserId` no se puede cambiar desde este endpoint de actualización.
        // Para vincular o desvincular usuarios use los endpoints dedicados LinkUser/UnlinkUser.
        if (request.UserId.HasValue && request.UserId != employee.UserId)
        {
            throw new InvalidOperationException("No está permitido cambiar el usuario asignado aquí. Use el endpoint de vinculación de usuario (LinkUser) para asignar otro usuario.");
        }
        employee.FirstName = request.FirstName;
        employee.LastName = request.LastName;
        employee.DocumentType = request.DocumentType;
        employee.DocumentNumber = request.DocumentNumber;
        employee.BirthDate = request.BirthDate.HasValue 
            ? DateTime.SpecifyKind(request.BirthDate.Value, DateTimeKind.Utc) 
            : null;
        employee.Gender = request.Gender;
        employee.MaritalStatus = request.MaritalStatus;
        employee.BloodType = request.BloodType;
        employee.Phone = request.Phone;
        employee.Mobile = request.Mobile;
        employee.PersonalEmail = request.PersonalEmail;
        employee.EmergencyContactName = request.EmergencyContactName;
        employee.EmergencyContactPhone = request.EmergencyContactPhone;
        employee.Address = request.Address;
        employee.City = request.City;
        employee.Department = request.Department;
        employee.Neighborhood = request.Neighborhood;
        employee.PostalCode = request.PostalCode;
        employee.Position = request.Position;
        employee.HireDate = request.HireDate.HasValue 
            ? DateTime.SpecifyKind(request.HireDate.Value, DateTimeKind.Utc) 
            : null;
        employee.ContractType = request.ContractType;
        employee.Salary = request.Salary;
        employee.PaymentFrequency = request.PaymentFrequency;
        employee.BankName = request.BankName;
        employee.BankAccountType = request.BankAccountType;
        employee.BankAccountNumber = request.BankAccountNumber;
        employee.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToResponse(employee);
    }

    public async Task<EmployeeResponse?> LinkUserAsync(int employeeId, LinkUserRequest request)
    {
        var employee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee is null) return null;

        if (request.CreateNewUser)
        {
            // Crear nuevo usuario
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                throw new InvalidOperationException("Email y contraseña son requeridos para crear un nuevo usuario");

            var emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower());
            if (emailExists)
                throw new InvalidOperationException("Ya existe un usuario con ese email");

            var newUser = new User
            {
                Name = employee.FullName,
                Email = request.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            employee.UserId = newUser.Id;
        }
        else if (request.UserId.HasValue)
        {
            // Vincular usuario existente
            var user = await _context.Users.FindAsync(request.UserId.Value);
            if (user is null)
                throw new InvalidOperationException("Usuario no encontrado");

            // Verificar que el usuario no esté vinculado a otro empleado
            var otherEmployee = await _context.Employees
                .AnyAsync(e => e.UserId == request.UserId.Value && e.Id != employeeId);
            
            if (otherEmployee)
                throw new InvalidOperationException("Este usuario ya está vinculado a otro empleado");

            employee.UserId = request.UserId.Value;
        }

        employee.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _context.Entry(employee).Reference(e => e.User).LoadAsync();

        return MapToResponse(employee);
    }

    public async Task<EmployeeResponse?> UnlinkUserAsync(int employeeId)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee is null) return null;

        employee.UserId = null;
        employee.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToResponse(employee);
    }

    public async Task<EmployeeResponse?> TerminateAsync(int id, TerminateEmployeeRequest request)
    {
        var employee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee is null) return null;

        employee.IsActive = false;
        employee.TerminationDate = DateTime.SpecifyKind(request.TerminationDate, DateTimeKind.Utc);
        employee.TerminationReason = request.TerminationReason;
        employee.UpdatedAt = DateTime.UtcNow;

        // También desactivar el usuario si existe
        if (employee.UserId.HasValue)
        {
            var user = await _context.Users.FindAsync(employee.UserId.Value);
            if (user is not null)
            {
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        return MapToResponse(employee);
    }

    public async Task<EmployeeResponse?> ReactivateAsync(int id)
    {
        var employee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee is null) return null;

        employee.IsActive = true;
        employee.TerminationDate = null;
        employee.TerminationReason = null;
        employee.UpdatedAt = DateTime.UtcNow;

        // También reactivar el usuario si existe
        if (employee.UserId.HasValue)
        {
            var user = await _context.Users.FindAsync(employee.UserId.Value);
            if (user is not null)
            {
                user.IsActive = true;
                user.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        return MapToResponse(employee);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee is null) return false;

        _context.Employees.Remove(employee);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<byte[]> GenerateLaborCertificateAsync(int employeeId)
    {
        var employee = await _context.Employees
            .Include(e => e.Affiliations)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee is null)
            throw new InvalidOperationException("Empleado no encontrado");

        // Obtener configuración de la empresa
        var companyName = await GetSettingAsync("company_name") ?? "Fresh Restaurant";
        var companyNit = await GetSettingAsync("company_nit") ?? "900.000.000-0";
        var companyAddress = await GetSettingAsync("company_address") ?? "";
        var companyCity = await GetSettingAsync("company_city") ?? "";

        var sb = new StringBuilder();
        
        // Encabezado
        sb.AppendLine("CERTIFICACIÓN LABORAL");
        sb.AppendLine("".PadRight(50, '='));
        sb.AppendLine();
        sb.AppendLine($"{companyName}");
        sb.AppendLine($"NIT: {companyNit}");
        if (!string.IsNullOrEmpty(companyAddress))
            sb.AppendLine($"Dirección: {companyAddress}");
        if (!string.IsNullOrEmpty(companyCity))
            sb.AppendLine($"Ciudad: {companyCity}");
        sb.AppendLine();
        sb.AppendLine("".PadRight(50, '-'));
        sb.AppendLine();

        sb.AppendLine("CERTIFICA QUE:");
        sb.AppendLine();
        sb.AppendLine($"El/La señor(a) {employee.FullName}, identificado(a) con {employee.DocumentType} No. {employee.DocumentNumber},");
        
        if (employee.IsActive)
        {
            sb.AppendLine($"labora en nuestra empresa desde el {employee.HireDate?.ToString("dd/MM/yyyy") ?? "N/A"}");
            sb.AppendLine($"desempeñando el cargo de: {employee.Position ?? "N/A"}");
            sb.AppendLine($"con un tipo de contrato: {employee.ContractType ?? "N/A"}");
        }
        else
        {
            sb.AppendLine($"laboró en nuestra empresa desde el {employee.HireDate?.ToString("dd/MM/yyyy") ?? "N/A"}");
            sb.AppendLine($"hasta el {employee.TerminationDate?.ToString("dd/MM/yyyy") ?? "N/A"}");
            sb.AppendLine($"desempeñando el cargo de: {employee.Position ?? "N/A"}");
        }
        
        sb.AppendLine();
        sb.AppendLine($"Salario actual/último: ${employee.Salary?.ToString("N2") ?? "N/A"}");
        sb.AppendLine();

        // Afiliaciones
        if (employee.Affiliations?.Any() == true)
        {
            sb.AppendLine("AFILIACIONES:");
            foreach (var aff in employee.Affiliations.Where(a => a.Status == "active"))
            {
                sb.AppendLine($"  - {aff.AffiliationTypeDisplay}: {aff.EntityName}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("".PadRight(50, '-'));
        sb.AppendLine();
        sb.AppendLine("La presente certificación se expide a solicitud del interesado,");
        sb.AppendLine($"en la ciudad de {companyCity}, a los {DateTime.Now:dd} días del mes de {DateTime.Now:MMMM} de {DateTime.Now:yyyy}.");
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("Atentamente,");
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("_______________________________");
        sb.AppendLine("Representante Legal");
        sb.AppendLine(companyName);

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private async Task<string?> GetSettingAsync(string key)
    {
        var setting = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Value;
    }

    private EmployeeResponse MapToResponse(Employee employee)
    {
        var documentsCount = _context.EmployeeDocuments.Count(d => d.EmployeeId == employee.Id);
        var childrenCount = _context.EmployeeChildren.Count(c => c.EmployeeId == employee.Id && c.IsActive);

        return new EmployeeResponse
        {
            Id = employee.Id,
            UserId = employee.UserId,
            UserName = employee.User?.Name,
            UserEmail = employee.User?.Email,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            FullName = employee.FullName,
            DocumentType = employee.DocumentType,
            DocumentNumber = employee.DocumentNumber,
            BirthDate = employee.BirthDate,
            Gender = employee.Gender,
            MaritalStatus = employee.MaritalStatus,
            BloodType = employee.BloodType,
            Phone = employee.Phone,
            Mobile = employee.Mobile,
            PersonalEmail = employee.PersonalEmail,
            EmergencyContactName = employee.EmergencyContactName,
            EmergencyContactPhone = employee.EmergencyContactPhone,
            Address = employee.Address,
            City = employee.City,
            Department = employee.Department,
            Neighborhood = employee.Neighborhood,
            PostalCode = employee.PostalCode,
            Position = employee.Position,
            HireDate = employee.HireDate,
            ContractType = employee.ContractType,
            Salary = employee.Salary,
            PaymentFrequency = employee.PaymentFrequency,
            BankName = employee.BankName,
            BankAccountType = employee.BankAccountType,
            BankAccountNumber = employee.BankAccountNumber,
            IsActive = employee.IsActive,
            TerminationDate = employee.TerminationDate,
            TerminationReason = employee.TerminationReason,
            DocumentsCount = documentsCount,
            ChildrenCount = childrenCount,
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt
        };
    }
}
