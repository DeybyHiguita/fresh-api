using Fresh.Core.DTOs.MenuItem;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MenuItemsController : ControllerBase
{
    private readonly IMenuItemService _menuItemService;

    public MenuItemsController(IMenuItemService menuItemService)
    {
        _menuItemService = menuItemService;
    }

    /// <summary>
    /// Obtiene todos los productos del menú
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MenuItemResponse>>> GetAll()
    {
        var menuItems = await _menuItemService.GetAllAsync();
        return Ok(menuItems);
    }

    /// <summary>
    /// Obtiene todos los productos del menú (sin autenticación)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("public")]
    public async Task<ActionResult<IEnumerable<MenuItemResponse>>> GetAllPublic()
    {
        var menuItems = await _menuItemService.GetAllAsync();
        return Ok(menuItems);
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
}