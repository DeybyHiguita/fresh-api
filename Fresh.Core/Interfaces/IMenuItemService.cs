using Fresh.Core.DTOs.MenuItem;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fresh.Core.Interfaces
{
    public interface IMenuItemService
    {
        Task<IEnumerable<MenuItemResponse>> GetAllAsync();
        Task<MenuItemResponse?> GetByIdAsync(int id);
        Task<MenuItemResponse> CreateAsync(MenuItemRequest request);
        Task<MenuItemResponse?> UpdateAsync(int id, MenuItemRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
