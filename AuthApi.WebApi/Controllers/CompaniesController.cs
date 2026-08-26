using AuthApi.Application.DTOs.Companies;
using AuthApi.Application.Features.Companies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.WebApi.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin,Admin")]
[Route("api/admin/[controller]")]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyService _companyService;

    public CompaniesController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    [HttpGet]
    public async Task<ActionResult<List<CompanyDto>>> GetCompanies()
    {
        var companies = await _companyService.GetCompaniesAsync();
        return Ok(companies);
    }

    [HttpPost]
    public async Task<ActionResult<CompanyDto>> CreateCompany([FromBody] CreateCompanyRequest request)
    {
        var company = await _companyService.CreateCompanyAsync(request);
        return Ok(company);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CompanyDto>> UpdateCompany(Guid id, [FromBody] UpdateCompanyRequest request)
    {
        var company = await _companyService.UpdateCompanyAsync(id, request);
        return Ok(company);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCompany(Guid id)
    {
        var success = await _companyService.DeleteCompanyAsync(id);
        return Ok(new { success });
    }
}
