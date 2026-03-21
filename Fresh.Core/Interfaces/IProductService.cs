using Fresh.Core.DTOs.Product;

namespace Fresh.Core.Interfaces;

public interface IProductService
{
    Task<IEnumerable<ProductResponse>> GetAllAsync(bool onlyActive = true);
    Task<ProductResponse?> GetByIdAsync(int id);
    Task<ProductResponse> CreateAsync(ProductRequest request);
    Task<ProductResponse?> UpdateAsync(int id, ProductRequest request);
    Task<bool> DeleteAsync(int id);
}
