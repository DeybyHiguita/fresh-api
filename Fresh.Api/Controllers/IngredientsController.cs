using Fresh.Core.DTOs.Ingredient;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IngredientsController : ControllerBase
{
    private readonly IIngredientService _ingredientService;

    public IngredientsController(IIngredientService ingredientService)
    {
        _ingredientService = ingredientService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<IngredientResponse>>> GetAll()
    {
        var ingredients = await _ingredientService.GetAllAsync();
        return Ok(ingredients);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<IngredientResponse>> GetById(int id)
    {
        var ingredient = await _ingredientService.GetByIdAsync(id);
        if (ingredient == null)
            return NotFound(new { message = "Ingrediente no encontrado" });

        return Ok(ingredient);
    }

    [HttpPost]
    public async Task<ActionResult<IngredientResponse>> Create([FromBody] IngredientRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var ingredient = await _ingredientService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = ingredient.Id }, ingredient);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<IngredientResponse>> Update(int id, [FromBody] IngredientRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var ingredient = await _ingredientService.UpdateAsync(id, request);
            if (ingredient == null)
                return NotFound(new { message = "Ingrediente no encontrado" });

            return Ok(ingredient);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _ingredientService.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = "Ingrediente no encontrado" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
