using AuthApi.Application.Common.Helpers;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Common.Models;
using AuthApi.Application.DTOs.Administrative;
using AuthApi.Application.Mappings;
using AuthApi.Domain.Entities.Administrative;
using AuthApi.Domain.Enums;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Application.Features.Administrative;

public interface IAdministrativeService
{
    Task<List<ProvinceDto>> GetProvincesAsync(string? search, bool? isActive = null);
    Task<ProvinceDto?> GetProvinceByCodeAsync(string code);
    Task<ProvinceDto> CreateProvinceAsync(CreateProvinceRequest request);
    Task<ProvinceDto> UpdateProvinceAsync(Guid id, UpdateProvinceRequest request);
    Task<bool> DeleteProvinceAsync(Guid id);
    Task<List<WardDto>> GetWardsAsync(string? provinceCode, string? search, bool? isActive = null);
    Task<WardDto?> GetWardByCodeAsync(string code);
    Task<WardDto> CreateWardAsync(CreateWardRequest request);
    Task<WardDto> UpdateWardAsync(Guid id, UpdateWardRequest request);
    Task<bool> DeleteWardAsync(Guid id);
    Task<List<AdministrativeTreeNodeDto>> GetAdministrativeTreeAsync();
    Task<List<object>> SearchAdministrativeUnitsAsync(string query, int limit = 20);
    Task<byte[]> ExportProvincesExcelAsync(string? search, bool? isActive = null);
    Task<byte[]> DownloadProvinceExcelTemplateAsync();
    Task<ImportResultDto> ImportProvincesExcelAsync(Stream fileStream);
    Task<byte[]> ExportWardsExcelAsync(string? provinceCode, string? search, bool? isActive = null);
    Task<byte[]> DownloadWardExcelTemplateAsync();
    Task<ImportResultDto> ImportWardsExcelAsync(Stream fileStream);
}

public class AdministrativeService : IAdministrativeService
{
    private readonly IApplicationDbContext _context;

    public AdministrativeService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProvinceDto>> GetProvincesAsync(string? search, bool? isActive = null)
    {
        var query = _context.Provinces
            .Include(p => p.Wards)
            .AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(s) || p.FullName.ToLower().Contains(s) || p.Code.ToLower().Contains(s));
        }

        var list = await query
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Code)
            .ToListAsync();

        return list.Select(ProvinceMapper.ToDto).ToList();
    }

    public async Task<ProvinceDto?> GetProvinceByCodeAsync(string code)
    {
        var province = await _context.Provinces
            .Include(p => p.Wards)
            .FirstOrDefaultAsync(p => p.Code == code);

        return province == null ? null : ProvinceMapper.ToDto(province);
    }

    public async Task<ProvinceDto> CreateProvinceAsync(CreateProvinceRequest request)
    {
        var code = request.Code.Trim();
        var exists = await _context.Provinces.AnyAsync(p => p.Code == code);
        if (exists)
        {
            throw new InvalidOperationException($"Mã Tỉnh/Thành phố '{code}' đã tồn tại trong hệ thống.");
        }

        var province = new Province
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = request.Name.Trim(),
            FullName = string.IsNullOrWhiteSpace(request.FullName) ? request.Name.Trim() : request.FullName.Trim(),
            DivisionType = request.DivisionType,
            AdministrativeRegion = request.AdministrativeRegion?.Trim(),
            SortOrder = request.SortOrder,
            IsActive = request.IsActive,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.Provinces.Add(province);
        await _context.SaveChangesAsync();

        return ProvinceMapper.ToDto(province);
    }

    public async Task<ProvinceDto> UpdateProvinceAsync(Guid id, UpdateProvinceRequest request)
    {
        var province = await _context.Provinces
            .Include(p => p.Wards)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (province == null)
        {
            throw new KeyNotFoundException("Không tìm thấy Tỉnh/Thành phố để cập nhật.");
        }

        province.Name = request.Name.Trim();
        province.FullName = string.IsNullOrWhiteSpace(request.FullName) ? request.Name.Trim() : request.FullName.Trim();
        if (request.DivisionType.HasValue)
        {
            province.DivisionType = request.DivisionType.Value;
        }
        province.AdministrativeRegion = request.AdministrativeRegion?.Trim();
        province.SortOrder = request.SortOrder;
        province.IsActive = request.IsActive;
        province.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        return ProvinceMapper.ToDto(province);
    }

    public async Task<bool> DeleteProvinceAsync(Guid id)
    {
        var province = await _context.Provinces
            .Include(p => p.Wards)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (province == null) return false;

        _context.Wards.RemoveRange(province.Wards);
        _context.Provinces.Remove(province);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<WardDto>> GetWardsAsync(string? provinceCode, string? search, bool? isActive = null)
    {
        var query = _context.Wards
            .Include(w => w.Province)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(provinceCode))
        {
            var pCode = provinceCode.Trim().ToLower();
            query = query.Where(w => w.Province != null && w.Province.Code.ToLower() == pCode);
        }

        if (isActive.HasValue)
        {
            query = query.Where(w => w.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(w => w.Name.ToLower().Contains(s) || w.FullName.ToLower().Contains(s) || w.Code.ToLower().Contains(s));
        }

        var list = await query
            .OrderBy(w => w.SortOrder)
            .ThenBy(w => w.Code)
            .ToListAsync();

        return list.Select(WardMapper.ToDto).ToList();
    }

    public async Task<WardDto?> GetWardByCodeAsync(string code)
    {
        var ward = await _context.Wards
            .Include(w => w.Province)
            .FirstOrDefaultAsync(w => w.Code == code);

        return ward == null ? null : WardMapper.ToDto(ward);
    }

    public async Task<WardDto> CreateWardAsync(CreateWardRequest request)
    {
        var code = request.Code.Trim();
        var exists = await _context.Wards.AnyAsync(w => w.Code == code);
        if (exists)
        {
            throw new InvalidOperationException($"Mã Phường/Xã '{code}' đã tồn tại trong hệ thống.");
        }

        var province = await _context.Provinces.FirstOrDefaultAsync(p => p.Id == request.ProvinceId);
        if (province == null)
        {
            throw new InvalidOperationException("Tỉnh/Thành phố trực thuộc không tồn tại.");
        }

        var ward = new Ward
        {
            Id = Guid.NewGuid(),
            ProvinceId = province.Id,
            ProvinceCode = province.Code,
            Code = code,
            Name = request.Name.Trim(),
            FullName = string.IsNullOrWhiteSpace(request.FullName) ? request.Name.Trim() : request.FullName.Trim(),
            DivisionType = request.DivisionType,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.Wards.Add(ward);
        await _context.SaveChangesAsync();

        ward.Province = province;
        return WardMapper.ToDto(ward);
    }

    public async Task<WardDto> UpdateWardAsync(Guid id, UpdateWardRequest request)
    {
        var ward = await _context.Wards
            .Include(w => w.Province)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (ward == null)
        {
            throw new KeyNotFoundException("Không tìm thấy Phường/Xã để cập nhật.");
        }

        ward.Name = request.Name.Trim();
        ward.FullName = string.IsNullOrWhiteSpace(request.FullName) ? request.Name.Trim() : request.FullName.Trim();
        if (request.DivisionType.HasValue)
        {
            ward.DivisionType = request.DivisionType.Value;
        }
        ward.SortOrder = request.SortOrder;
        ward.IsActive = request.IsActive;
        ward.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        return WardMapper.ToDto(ward);
    }

    public async Task<bool> DeleteWardAsync(Guid id)
    {
        var ward = await _context.Wards.FirstOrDefaultAsync(w => w.Id == id);
        if (ward == null) return false;

        _context.Wards.Remove(ward);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<AdministrativeTreeNodeDto>> GetAdministrativeTreeAsync()
    {
        var provinces = await _context.Provinces
            .Include(p => p.Wards)
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Code)
            .ToListAsync();

        var tree = provinces.Select(p => new AdministrativeTreeNodeDto
        {
            Value = p.Code,
            Label = p.Name,
            IsLeaf = false,
            Children = p.Wards
                .Where(w => w.IsActive)
                .OrderBy(w => w.SortOrder)
                .ThenBy(w => w.Code)
                .Select(w => new AdministrativeTreeNodeDto
                {
                    Value = w.Code,
                    Label = w.Name,
                    IsLeaf = true,
                    Children = null
                })
                .ToList()
        }).ToList();

        return tree;
    }

    public async Task<List<object>> SearchAdministrativeUnitsAsync(string query, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<object>();

        var q = query.Trim().ToLower();

        var provinces = await _context.Provinces
            .Where(p => p.Name.ToLower().Contains(q) || p.FullName.ToLower().Contains(q) || p.Code.ToLower().Contains(q))
            .Take(limit)
            .Select(p => new
            {
                Type = "Province",
                p.Code,
                p.Name,
                p.FullName,
                DivisionType = p.DivisionType.ToString(),
                DivisionTypeName = p.DivisionType == ProvinceDivisionType.Municipality ? "Thành phố trực thuộc trung ương" : "Tỉnh",
                p.AdministrativeRegion,
                ProvinceCode = (string?)null,
                ProvinceName = (string?)null,
                Display = p.FullName
            })
            .ToListAsync();

        var wards = await _context.Wards
            .Include(w => w.Province)
            .Where(w => w.Name.ToLower().Contains(q) || w.FullName.ToLower().Contains(q) || w.Code.ToLower().Contains(q))
            .Take(limit)
            .Select(w => new
            {
                Type = "Ward",
                w.Code,
                w.Name,
                w.FullName,
                DivisionType = w.DivisionType.ToString(),
                DivisionTypeName = w.DivisionType == WardDivisionType.Ward ? "Phường" : w.DivisionType == WardDivisionType.Township ? "Thị trấn" : "Xã",
                ProvinceCode = w.Province != null ? w.Province.Code : null,
                ProvinceName = w.Province != null ? w.Province.Name : null,
                Display = $"{w.FullName}, {(w.Province != null ? w.Province.Name : "")}"
            })
            .ToListAsync();

        var result = new List<object>();
        result.AddRange(provinces);
        result.AddRange(wards);

        return result.Take(limit).ToList();
    }

    public async Task<byte[]> ExportProvincesExcelAsync(string? search, bool? isActive = null)
    {
        var provinces = await GetProvincesAsync(search, isActive);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("DanhSachTinhThanh");

        string[] headers = ["STT", "Mã Tỉnh/Thành", "Tên Tỉnh/Thành", "Tên Đầy Đủ", "Cấp Hành Chính", "Vùng Địa Lý", "Thứ Tự", "Trạng Thái"];
        for (int i = 0; i < headers.Length; i++)
        {
            ExcelHelper.WriteStyledHeaderCell(ws, i + 1, headers[i], i == 1 || i == 2);
        }
        ws.Row(1).Height = 28;

        for (int i = 0; i < provinces.Count; i++)
        {
            var p = provinces[i];
            int row = i + 2;

            ws.Cell(row, 1).SetValue(i + 1);
            ws.Cell(row, 2).SetValue(p.Code);
            ws.Cell(row, 3).SetValue(p.Name);
            ws.Cell(row, 4).SetValue(string.IsNullOrWhiteSpace(p.FullName) ? p.Name : p.FullName);
            ws.Cell(row, 5).SetValue(p.DivisionType.ToDisplayName());
            ws.Cell(row, 6).SetValue(p.AdministrativeRegion ?? "");
            ws.Cell(row, 7).SetValue(p.SortOrder);
            ws.Cell(row, 8).SetValue(p.IsActive ? "Hoạt động" : "Tạm dừng");

            for (int col = 1; col <= 8; col++)
            {
                var cell = ws.Cell(row, col);
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                if (col == 1 || col == 2 || col == 5 || col == 7 || col == 8)
                {
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
            }
        }

        ExcelHelper.ApplyColumnWidths(ws);
        ExcelHelper.FreezeHeaderRow(ws);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public Task<byte[]> DownloadProvinceExcelTemplateAsync()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("MauNhapTinhThanh");

        string[] headers = ["Mã Tỉnh/Thành", "Tên Tỉnh/Thành", "Tên Đầy Đủ", "Cấp Hành Chính", "Vùng Địa Lý", "Thứ Tự", "Trạng Thái"];
        for (int i = 0; i < headers.Length; i++)
        {
            ExcelHelper.WriteStyledHeaderCell(ws, i + 1, headers[i], i == 0 || i == 1);
        }
        ws.Row(1).Height = 28;

        ws.Cell(2, 1).SetValue("79");
        ws.Cell(2, 2).SetValue("Hồ Chí Minh");
        ws.Cell(2, 3).SetValue("Thành phố Hồ Chí Minh");
        ws.Cell(2, 4).SetValue("Thành phố trực thuộc trung ương");
        ws.Cell(2, 5).SetValue("Đông Nam Bộ");
        ws.Cell(2, 6).SetValue(1);
        ws.Cell(2, 7).SetValue("1");
        ws.Cell(3, 1).SetValue("01");
        ws.Cell(3, 2).SetValue("Hà Nội");
        ws.Cell(3, 3).SetValue("Thành phố Hà Nội");
        ws.Cell(3, 4).SetValue("Thành phố trực thuộc trung ương");
        ws.Cell(3, 5).SetValue("Đồng bằng sông Hồng");
        ws.Cell(3, 6).SetValue(2);
        ws.Cell(3, 7).SetValue("1");
        ws.Cell(4, 1).SetValue("48");
        ws.Cell(4, 2).SetValue("Đà Nẵng");
        ws.Cell(4, 3).SetValue("Thành phố Đà Nẵng");
        ws.Cell(4, 4).SetValue("Thành phố trực thuộc trung ương");
        ws.Cell(4, 5).SetValue("Duyên hải Nam Trung Bộ");
        ws.Cell(4, 6).SetValue(3);
        ws.Cell(4, 7).SetValue("1");

        for (int r = 2; r <= 4; r++)
        {
            for (int col = 1; col <= 7; col++)
            {
                var cell = ws.Cell(r, col);
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }
        }

        ExcelHelper.ApplyColumnWidths(ws);
        ExcelHelper.FreezeHeaderRow(ws);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return Task.FromResult(stream.ToArray());
    }

    public async Task<ImportResultDto> ImportProvincesExcelAsync(Stream fileStream)
    {
        var result = new ImportResultDto();

        using var workbook = new XLWorkbook(fileStream);
        var ws = workbook.Worksheets.FirstOrDefault();
        if (ws == null)
        {
            result.Errors.Add("File Excel không có bất kỳ sheet nào.");
            result.ErrorCount = 1;
            return result;
        }

        var rows = ws.RowsUsed().Skip(1).ToList();
        result.TotalRows = rows.Count;

        if (result.TotalRows == 0)
        {
            result.Errors.Add("File Excel không chứa dòng dữ liệu nào để nhập.");
            result.ErrorCount = 1;
            return result;
        }

        var existingProvinces = await _context.Provinces.ToListAsync();
        var provinceMap = existingProvinces.ToDictionary(p => p.Code.Trim().ToLower(), p => p);

        int rowIndex = 1;
        foreach (var row in rows)
        {
            rowIndex++;
            var code = ExcelHelper.GetString(row, 1);
            var name = ExcelHelper.GetString(row, 2);
            var fullName = ExcelHelper.GetString(row, 3) ?? name;
            var divisionTypeStr = ExcelHelper.GetString(row, 4);
            var administrativeRegion = ExcelHelper.GetString(row, 5);
            var sortOrder = ExcelHelper.GetInt(row, 6) ?? 0;
            var isActive = ExcelHelper.GetBool(row, 7, true);

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                result.ErrorCount++;
                result.Errors.Add($"Dòng {rowIndex}: Thiếu 'Mã Tỉnh/Thành' hoặc 'Tên Tỉnh/Thành'.");
                continue;
            }

            var divisionType = AdministrativeEnumExtensions.ParseProvinceDivisionType(divisionTypeStr);
            var key = code.Trim().ToLower();
            if (provinceMap.TryGetValue(key, out var existing))
            {
                existing.Name = name.Trim();
                existing.FullName = string.IsNullOrWhiteSpace(fullName) ? name.Trim() : fullName.Trim();
                existing.DivisionType = divisionType;
                existing.AdministrativeRegion = administrativeRegion?.Trim();
                existing.SortOrder = sortOrder;
                existing.IsActive = isActive;
                existing.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                var newProvince = new Province
                {
                    Id = Guid.NewGuid(),
                    Code = code.Trim(),
                    Name = name.Trim(),
                    FullName = string.IsNullOrWhiteSpace(fullName) ? name.Trim() : fullName.Trim(),
                    DivisionType = divisionType,
                    AdministrativeRegion = administrativeRegion?.Trim(),
                    SortOrder = sortOrder,
                    IsActive = isActive,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _context.Provinces.Add(newProvince);
                provinceMap[key] = newProvince;
            }

            result.SuccessCount++;
        }

        if (result.SuccessCount > 0)
        {
            await _context.SaveChangesAsync();
        }

        return result;
    }

    public async Task<byte[]> ExportWardsExcelAsync(string? provinceCode, string? search, bool? isActive = null)
    {
        var wards = await GetWardsAsync(provinceCode, search, isActive);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("DanhSachPhuongXa");

        string[] headers = ["STT", "Mã Phường/Xã", "Tên Phường/Xã", "Tên Đầy Đủ", "Cấp Hành Chính", "Mã Tỉnh/Thành", "Tên Tỉnh/Thành", "Thứ Tự", "Trạng Thái"];
        for (int i = 0; i < headers.Length; i++)
        {
            ExcelHelper.WriteStyledHeaderCell(ws, i + 1, headers[i], i == 1 || i == 2 || i == 5);
        }
        ws.Row(1).Height = 28;

        for (int i = 0; i < wards.Count; i++)
        {
            var w = wards[i];
            int row = i + 2;

            ws.Cell(row, 1).SetValue(i + 1);
            ws.Cell(row, 2).SetValue(w.Code);
            ws.Cell(row, 3).SetValue(w.Name);
            ws.Cell(row, 4).SetValue(string.IsNullOrWhiteSpace(w.FullName) ? w.Name : w.FullName);
            ws.Cell(row, 5).SetValue(w.DivisionType.ToDisplayName());
            ws.Cell(row, 6).SetValue(w.ProvinceCode ?? "");
            ws.Cell(row, 7).SetValue(w.ProvinceName ?? "");
            ws.Cell(row, 8).SetValue(w.SortOrder);
            ws.Cell(row, 9).SetValue(w.IsActive ? "Hoạt động" : "Tạm dừng");

            for (int col = 1; col <= 9; col++)
            {
                var cell = ws.Cell(row, col);
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                if (col == 1 || col == 2 || col == 5 || col == 6 || col == 8 || col == 9)
                {
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
            }
        }

        ExcelHelper.ApplyColumnWidths(ws);
        ExcelHelper.FreezeHeaderRow(ws);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> DownloadWardExcelTemplateAsync()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("MauNhapPhuongXa");

        string[] headers = ["Mã Phường/Xã", "Tên Phường/Xã", "Tên Đầy Đủ", "Cấp Hành Chính", "Mã Tỉnh/Thành", "Thứ Tự", "Trạng Thái"];
        for (int i = 0; i < headers.Length; i++)
        {
            ExcelHelper.WriteStyledHeaderCell(ws, i + 1, headers[i], i == 0 || i == 1 || i == 4);
        }
        ws.Row(1).Height = 28;

        ws.Cell(2, 1).SetValue("26734");
        ws.Cell(2, 2).SetValue("Bến Nghé");
        ws.Cell(2, 3).SetValue("Phường Bến Nghé");
        ws.Cell(2, 4).SetValue("Phường");
        ws.Cell(2, 5).SetValue("79");
        ws.Cell(2, 6).SetValue(1);
        ws.Cell(2, 7).SetValue("1");
        ws.Cell(3, 1).SetValue("00001");
        ws.Cell(3, 2).SetValue("Phúc Xá");
        ws.Cell(3, 3).SetValue("Phường Phúc Xá");
        ws.Cell(3, 4).SetValue("Phường");
        ws.Cell(3, 5).SetValue("01");
        ws.Cell(3, 6).SetValue(2);
        ws.Cell(3, 7).SetValue("1");

        for (int r = 2; r <= 3; r++)
        {
            for (int col = 1; col <= 7; col++)
            {
                var cell = ws.Cell(r, col);
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }
        }

        ExcelHelper.ApplyColumnWidths(ws);
        ExcelHelper.FreezeHeaderRow(ws);
        var provinces = await _context.Provinces
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Code)
            .Select(p => new { p.Code, p.Name })
            .ToListAsync();

        ExcelHelper.WriteReferenceSheet(
            workbook,
            "DanhSachTinhThanh",
            "Mã Tỉnh/Thành (Code)",
            "Tên Tỉnh/Thành (Name)",
            provinces.Select(p => (p.Code, p.Name))
        );

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<ImportResultDto> ImportWardsExcelAsync(Stream fileStream)
    {
        var result = new ImportResultDto();

        using var workbook = new XLWorkbook(fileStream);
        var ws = workbook.Worksheets.FirstOrDefault();
        if (ws == null)
        {
            result.Errors.Add("File Excel không có bất kỳ sheet nào.");
            result.ErrorCount = 1;
            return result;
        }

        var rows = ws.RowsUsed().Skip(1).ToList();
        result.TotalRows = rows.Count;

        if (result.TotalRows == 0)
        {
            result.Errors.Add("File Excel không chứa dòng dữ liệu nào để nhập.");
            result.ErrorCount = 1;
            return result;
        }

        var provinces = await _context.Provinces.ToListAsync();
        var provinceMap = provinces.ToDictionary(p => p.Code.Trim().ToLower(), p => p);

        var existingWards = await _context.Wards.ToListAsync();
        var wardMap = existingWards.ToDictionary(w => w.Code.Trim().ToLower(), w => w);

        int rowIndex = 1;
        foreach (var row in rows)
        {
            rowIndex++;
            var code = ExcelHelper.GetString(row, 1);
            var name = ExcelHelper.GetString(row, 2);
            var fullName = ExcelHelper.GetString(row, 3) ?? name;
            var divisionTypeStr = ExcelHelper.GetString(row, 4);
            var provinceCode = ExcelHelper.GetString(row, 5);
            var sortOrder = ExcelHelper.GetInt(row, 6) ?? 0;
            var isActive = ExcelHelper.GetBool(row, 7, true);

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(provinceCode))
            {
                result.ErrorCount++;
                result.Errors.Add($"Dòng {rowIndex}: Thiếu 'Mã Phường/Xã', 'Tên Phường/Xã', hoặc 'Mã Tỉnh/Thành'.");
                continue;
            }

            var pKey = provinceCode.Trim().ToLower();
            if (!provinceMap.TryGetValue(pKey, out var province))
            {
                result.ErrorCount++;
                result.Errors.Add($"Dòng {rowIndex}: Mã Tỉnh/Thành '{provinceCode}' không tồn tại trong hệ thống.");
                continue;
            }

            var divisionType = AdministrativeEnumExtensions.ParseWardDivisionType(divisionTypeStr);
            var wKey = code.Trim().ToLower();
            if (wardMap.TryGetValue(wKey, out var existingWard))
            {
                existingWard.ProvinceId = province.Id;
                existingWard.ProvinceCode = province.Code;
                existingWard.Name = name.Trim();
                existingWard.FullName = string.IsNullOrWhiteSpace(fullName) ? name.Trim() : fullName.Trim();
                existingWard.DivisionType = divisionType;
                existingWard.SortOrder = sortOrder;
                existingWard.IsActive = isActive;
                existingWard.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                var newWard = new Ward
                {
                    Id = Guid.NewGuid(),
                    ProvinceId = province.Id,
                    ProvinceCode = province.Code,
                    Code = code.Trim(),
                    Name = name.Trim(),
                    FullName = string.IsNullOrWhiteSpace(fullName) ? name.Trim() : fullName.Trim(),
                    DivisionType = divisionType,
                    SortOrder = sortOrder,
                    IsActive = isActive,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _context.Wards.Add(newWard);
                wardMap[wKey] = newWard;
            }

            result.SuccessCount++;
        }

        if (result.SuccessCount > 0)
        {
            await _context.SaveChangesAsync();
        }

        return result;
    }
}
