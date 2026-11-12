using Microsoft.AspNetCore.Mvc;
using BonusTest.Application.Bonuses;

namespace BonusTest.Api.Controllers;

/// <summary>
/// Endpoints del módulo de bonos. Solo traduce HTTP a llamadas a <see cref="BonusService"/>;
/// las reglas de negocio viven en la capa Application/Domain.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class BonusController(BonusService bonusService) : ControllerBase
{
    private const string UserCodeHeader = "UserCode";

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] BonusFilterRequest filter, CancellationToken cancellationToken) =>
        Ok(await bonusService.GetPagedAsync(filter, cancellationToken));

    [HttpGet("employee/{departmentCode}/{employeeNumber:int}")]
    public async Task<IActionResult> GetEmployee(string departmentCode, int employeeNumber, CancellationToken cancellationToken) =>
        Ok(await bonusService.GetActiveEmployeeAsync(departmentCode, employeeNumber, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateBonusRequest request,
        [FromHeader(Name = UserCodeHeader)] string? userCode,
        CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await bonusService.CreateAsync(request, userCode, cancellationToken));

    [HttpPut("{bonusId:int}/cancel")]
    public async Task<IActionResult> Cancel(
        int bonusId, [FromHeader(Name = UserCodeHeader)] string? userCode, CancellationToken cancellationToken)
    {
        await bonusService.CancelAsync(bonusId, userCode, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{bonusId:int}")]
    public async Task<IActionResult> Delete(
        int bonusId, [FromHeader(Name = UserCodeHeader)] string? userCode, CancellationToken cancellationToken)
    {
        await bonusService.DeleteAsync(bonusId, userCode, cancellationToken);
        return NoContent();
    }

    [HttpPost("{bonusId:int}/pay")]
    public async Task<IActionResult> Pay(
        int bonusId, [FromHeader(Name = UserCodeHeader)] string? userCode, CancellationToken cancellationToken) =>
        Ok(await bonusService.PayAsync(bonusId, userCode, cancellationToken));
}
