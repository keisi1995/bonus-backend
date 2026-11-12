using BonusTest.Application.Common;

namespace BonusTest.Application.Bonuses;

/// <summary>Filtros opcionales del listado; se reciben por query string.</summary>
public class BonusFilterRequest : PaginationRequest
{
    public string? DepartmentCode { get; set; }
    public int? EmployeeNumber { get; set; }
    public string? Period { get; set; }
    public string? BonusStateCode { get; set; }
}

public record BonusListItemDto(
    int BonusId,
    string BonusStateCode,
    string BonusStateDescription,
    string Period,
    string DepartmentCode,
    string DepartmentName,
    int EmployeeNumber,
    string EmployeeFullName,
    int BonusTypeId,
    string BonusTypeDescription,
    decimal BonusValue,
    string ReasonCode,
    string ReasonDescription,
    decimal GrossAmount,
    decimal TaxAmount,
    decimal NetAmount);

public record EmployeeDto(
    string DepartmentCode,
    int EmployeeNumber,
    string FullName,
    DateTime HireDate,
    decimal MonthlySalary,
    decimal TaxRate);

public record CreateBonusRequest(
    string DepartmentCode,
    int EmployeeNumber,
    string Period,
    int BonusTypeId,
    decimal BonusValue,
    string ReasonCode);

public record BonusCreatedDto(int BonusId, decimal GrossAmount, decimal TaxAmount, decimal NetAmount);

public record BonusDistributionDto(string ProjectName, decimal Hours, decimal Amount);

public record BonusPaymentDto(int BonusId, decimal GrossAmount, IReadOnlyList<BonusDistributionDto> Distributions);
