using Fresh.Core.DTOs.Recipe;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecipesController : ControllerBase
{
    private readonly IRecipeService _recipeService;

    public RecipesController(IRecipeService recipeService)
    {
        _recipeService = recipeService;
    }

    [HttpPost("{id}/details")]
    public async Task<ActionResult<RecipeDetailResponse>> AddDetail(int id, [FromBody] RecipeDetailRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var detail = await _recipeService.AddDetailAsync(id, request);
            return Ok(detail);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Receta no encontrada" });
        }
    }

    [HttpDelete("details/{detailId}")]
    public async Task<IActionResult> RemoveDetail(int detailId)
    {
        var result = await _recipeService.RemoveDetailAsync(detailId);
        if (!result) return NotFound(new { message = "Detalle no encontrado" });

        return NoContent();
    }
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var recipes = await _recipeService.GetAllAsync();
        return Ok(recipes);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var recipe = await _recipeService.GetByIdAsync(id);
        if (recipe == null) return NotFound();
        return Ok(recipe);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RecipeRequest request)
    {
        var recipe = await _recipeService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = recipe.Id }, recipe);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] RecipeRequest request)
    {
        var recipe = await _recipeService.UpdateAsync(id, request);
        if (recipe == null) return NotFound();
        return Ok(recipe);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _recipeService.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchByName([FromQuery] string name)
    {
        var recipes = await _recipeService.GetByNameAsync(name);
        return Ok(recipes);
    }
}
