using AuthApi.Application.Common.Models;
using AuthApi.Application.DTOs.Administrative;
using AuthApi.Application.Features.Administrative;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.WebApi.Controllers;

public class ExportProvincesQuery
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}

public class ExportWardsQuery
{
    public string? ProvinceCode { get; set; }
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class AdministrativeController : ControllerBase
{
    private readonly IAdministrativeService _adminService;

    public AdministrativeController(IAdministrativeService adminService)
    {
        _adminService = adminService;
    }

    // === Public Shared APIs for ERP Apps (HRM, TMS, WMS, EMS) ===

    [AllowAnonymous]
    [HttpGet("provinces")]
    public async Task<ActionResult<List<ProvinceDto>>> GetProvinces([FromQuery] string? search, [FromQuery] bool? isActive)
    {
        var provinces = await _adminService.GetProvincesAsync(search, isActive);
        return Ok(provinces);
    }

    [AllowAnonymous]
    [HttpGet("provinces/{code}")]
    public async Task<ActionResult<ProvinceDto>> GetProvinceByCode(string code)
    {
        var province = await _adminService.GetProvinceByCodeAsync(code);
        if (province == null) return NotFound(new { message = "Không tìm thấy tỉnh/thành phố." });
        return Ok(province);
    }

    [AllowAnonymous]
    [HttpGet("provinces/{provinceCode}/wards")]
    public async Task<ActionResult<List<WardDto>>> GetWardsByProvince(string provinceCode, [FromQuery] string? search, [FromQuery] bool? isActive)
    {
        var wards = await _adminService.GetWardsAsync(provinceCode, search, isActive);
        return Ok(wards);
    }

    [AllowAnonymous]
    [HttpGet("wards")]
    public async Task<ActionResult<List<WardDto>>> GetAllWards([FromQuery] string? provinceCode, [FromQuery] string? search, [FromQuery] bool? isActive)
    {
        var wards = await _adminService.GetWardsAsync(provinceCode, search, isActive);
        return Ok(wards);
    }

    [AllowAnonymous]
    [HttpGet("wards/{code}")]
    public async Task<ActionResult<WardDto>> GetWardByCode(string code)
    {
        var ward = await _adminService.GetWardByCodeAsync(code);
        if (ward == null) return NotFound(new { message = "Không tìm thấy phường/xã." });
        return Ok(ward);
    }

    [AllowAnonymous]
    [HttpGet("tree")]
    public async Task<ActionResult<List<AdministrativeTreeNodeDto>>> GetTree()
    {
        var tree = await _adminService.GetAdministrativeTreeAsync();
        return Ok(tree);
    }

    [AllowAnonymous]
    [HttpGet("search")]
    public async Task<ActionResult<List<object>>> Search([FromQuery] string q, [FromQuery] int limit = 20)
    {
        var results = await _adminService.SearchAdministrativeUnitsAsync(q, limit);
        return Ok(results);
    }

    // === Admin Management Endpoints ===

    [Authorize(Roles = "SuperAdmin,Admin")]
    [HttpPost("provinces")]
    public async Task<ActionResult<ProvinceDto>> CreateProvince([FromBody] CreateProvinceRequest request)
    {
        var province = await _adminService.CreateProvinceAsync(request);
        return Ok(province);
    }

    [Authorize(Roles = "SuperAdmin,Admin")]
    [HttpPut("provinces/{id:guid}")]
    public async Task<ActionResult<ProvinceDto>> UpdateProvince(Guid id, [FromBody] UpdateProvinceRequest request)
    {
        var province = await _adminService.UpdateProvinceAsync(id, request);
        return Ok(province);
    }

    [Authorize(Roles = "SuperAdmin,Admin")]
    [HttpDelete("provinces/{id:guid}")]
    public async Task<IActionResult> DeleteProvince(Guid id)
    {
        var success = await _adminService.DeleteProvinceAsync(id);
        return Ok(new { success });
    }

    [Authorize(Roles = "SuperAdmin,Admin")]
    [HttpPost("wards")]
    public async Task<ActionResult<WardDto>> CreateWard([FromBody] CreateWardRequest request)
    {
        var ward = await _adminService.CreateWardAsync(request);
        return Ok(ward);
    }

    [Authorize(Roles = "SuperAdmin,Admin")]
    [HttpPut("wards/{id:guid}")]
    public async Task<ActionResult<WardDto>> UpdateWard(Guid id, [FromBody] UpdateWardRequest request)
    {
        var ward = await _adminService.UpdateWardAsync(id, request);
        return Ok(ward);
    }

    [Authorize(Roles = "SuperAdmin,Admin")]
    [HttpDelete("wards/{id:guid}")]
    public async Task<IActionResult> DeleteWard(Guid id)
    {
        var success = await _adminService.DeleteWardAsync(id);
        return Ok(new { success });
    }

    // === Excel Import / Export: Provinces ===

    [Authorize(Roles = "SuperAdmin,Admin")]
    [HttpPost("provinces/excel/export")]
    public async Task<IActionResult> ExportProvincesExcel([FromBody] ExportProvincesQuery query)
    {
        var content = await _adminService.ExportProvincesExcelAsync(query.Search, query.IsActive);
        var fileName = $"Danh_Sach_Tinh_Thanh_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [Authorize(Roles = "SuperAdmin,Admin")]
    [HttpGet("provinces/excel/template")]
    [HttpPost("provinces/excel/template")]
    public async Task<IActionResult> DownloadProvinceExcelTemplate()
    {
        var content = await _adminService.DownloadProvinceExcelTemplateAsync();
        var fileName = "Mau_Nhap_Tinh_Thanh.xlsx";
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [Authorize(Roles = "SuperAdmin,Admin")]
    [HttpPost("provinces/excel/import")]
    public async Task<ActionResult<ImportResultDto>> ImportProvincesExcel(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Vui lòng chọn file Excel hợp lệ để tải lên." });

        using var stream = file.OpenReadStream();
        var result = await _adminService.ImportProvincesExcelAsync(stream);
        return Ok(result);
    }

    // === Excel Import / Export: Wards ===

    [Authorize(Roles = "SuperAdmin,Admin")]
    [HttpPost("wards/excel/export")]
    public async Task<IActionResult> ExportWardsExcel([FromBody] ExportWardsQuery query)
    {
        var content = await _adminService.ExportWardsExcelAsync(query.ProvinceCode, query.Search, query.IsActive);
        var fileName = $"Danh_Sach_Phuong_Xa_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [Authorize(Roles = "SuperAdmin,Admin")]
    [HttpGet("wards/excel/template")]
    [HttpPost("wards/excel/template")]
    public async Task<IActionResult> DownloadWardExcelTemplate()
    {
        var content = await _adminService.DownloadWardExcelTemplateAsync();
        var fileName = "Mau_Nhap_Phuong_Xa.xlsx";
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [Authorize(Roles = "SuperAdmin,Admin")]
    [HttpPost("wards/excel/import")]
    public async Task<ActionResult<ImportResultDto>> ImportWardsExcel(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Vui lòng chọn file Excel hợp lệ để tải lên." });

        using var stream = file.OpenReadStream();
        var result = await _adminService.ImportWardsExcelAsync(stream);
        return Ok(result);
    }
}
