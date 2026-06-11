using Fresh.Core.DTOs.MenuItem;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MenuItemsController : ControllerBase
{
    private readonly IMenuItemService _menuItemService;
    private int StoreId => int.TryParse(User.FindFirst("store_id")?.Value, out var id) ? id : 0;

    public MenuItemsController(IMenuItemService menuItemService)
    {
        _menuItemService = menuItemService;
    }

    /// <summary>
    /// Obtiene todos los productos del menú con estado por tienda
    /// </summary>
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MenuItemResponse>>> GetAll()
    {
        var menuItems = await _menuItemService.GetAllAsync(StoreId);
        return Ok(menuItems);
    }

    /// <summary>
    /// Obtiene todos los productos del menú (sin autenticación)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("public")]
    public async Task<ActionResult<IEnumerable<MenuItemResponse>>> GetAllPublic()
    {
        var menuItems = await _menuItemService.GetAllAsync(0);
        return Ok(menuItems);
    }

    /// <summary>
    /// Activa/desactiva un producto en la tienda activa
    /// </summary>
    [Authorize]
    [HttpPatch("{id}/toggle-store")]
    public async Task<IActionResult> ToggleStoreEnabled(int id)
    {
        if (StoreId == 0)
            return BadRequest(new { message = "Se requiere contexto de tienda." });

        var isEnabled = await _menuItemService.ToggleStoreMenuItemAsync(id, StoreId);
        return Ok(new { isEnabled });
    }

    /// <summary>
    /// Obtiene un producto del menú por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<MenuItemResponse>> GetById(int id)
    {
        var menuItem = await _menuItemService.GetByIdAsync(id);
        if (menuItem == null)
            return NotFound(new { message = "Producto no encontrado en el menú" });

        return Ok(menuItem);
    }

    /// <summary>
    /// Crea un nuevo producto para el menú
    /// </summary>
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<MenuItemResponse>> Create([FromBody] MenuItemRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var menuItem = await _menuItemService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = menuItem.Id }, menuItem);
    }

    /// <summary>
    /// Actualiza un producto existente
    /// </summary>
    [Authorize]
    [HttpPut("{id}")]
    public async Task<ActionResult<MenuItemResponse>> Update(int id, [FromBody] MenuItemRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var menuItem = await _menuItemService.UpdateAsync(id, request);
        if (menuItem == null)
            return NotFound(new { message = "Producto no encontrado" });

        return Ok(menuItem);
    }

    /// <summary>
    /// Elimina un producto del menú
    /// </summary>
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _menuItemService.DeleteAsync(id);
        if (!result)
            return NotFound(new { message = "Producto no encontrado" });

        return NoContent();
    }

    /// <summary>
    /// Reordena los productos del menú en lote
    /// </summary>
    [Authorize]
    [HttpPatch("reorder")]
    public async Task<IActionResult> Reorder([FromBody] ReorderMenuItemsRequest request)
    {
        await _menuItemService.ReorderAsync(request.Items);
        return NoContent();
    }

    // ── Variants ────────────────────────────────────────────────

    [Authorize]
    [HttpPost("{id}/variants")]
    public async Task<ActionResult<MenuItemVariantResponse>> AddVariant(int id, [FromBody] MenuItemVariantRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var variant = await _menuItemService.AddVariantAsync(id, request);
        return Ok(variant);
    }

    [Authorize]
    [HttpPut("{id}/variants/{variantId}")]
    public async Task<ActionResult<MenuItemVariantResponse>> UpdateVariant(int id, int variantId, [FromBody] MenuItemVariantRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var variant = await _menuItemService.UpdateVariantAsync(id, variantId, request);
        return variant == null ? NotFound() : Ok(variant);
    }

    [Authorize]
    [HttpDelete("{id}/variants/{variantId}")]
    public async Task<IActionResult> DeleteVariant(int id, int variantId)
    {
        var result = await _menuItemService.DeleteVariantAsync(id, variantId);
        return result ? NoContent() : NotFound();
    }
}