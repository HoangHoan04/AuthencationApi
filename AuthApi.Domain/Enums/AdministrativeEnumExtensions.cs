namespace AuthApi.Domain.Enums;

public static class AdministrativeEnumExtensions
{
    public static string ToCode(this ProvinceDivisionType divisionType)
    {
        return divisionType switch
        {
            ProvinceDivisionType.Municipality => "CITY",
            ProvinceDivisionType.Province => "PROVINCE",
            _ => "PROVINCE"
        };
    }

    public static string ToDisplayName(this ProvinceDivisionType divisionType)
    {
        return divisionType switch
        {
            ProvinceDivisionType.Province => "Tỉnh",
            ProvinceDivisionType.Municipality => "Thành phố trực thuộc trung ương",
            _ => "Tỉnh"
        };
    }

    public static ProvinceDivisionType ParseProvinceDivisionType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return ProvinceDivisionType.Province;

        var v = value.Trim().ToLower();
        if (v.Contains("thành phố") || v.Contains("trung ương") || v.Contains("municipality") || v == "city" || v == "2")
        {
            return ProvinceDivisionType.Municipality;
        }

        return ProvinceDivisionType.Province;
    }

    public static string ToCode(this WardDivisionType divisionType)
    {
        return divisionType switch
        {
            WardDivisionType.Ward => "WARD",
            WardDivisionType.Commune => "COMMUNE",
            WardDivisionType.Township => "TOWNSHIP",
            _ => "COMMUNE"
        };
    }

    public static string ToDisplayName(this WardDivisionType divisionType)
    {
        return divisionType switch
        {
            WardDivisionType.Ward => "Phường",
            WardDivisionType.Commune => "Xã",
            WardDivisionType.Township => "Thị trấn",
            _ => "Xã"
        };
    }

    public static WardDivisionType ParseWardDivisionType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return WardDivisionType.Commune;

        var v = value.Trim().ToLower();
        if (v.Contains("phường") || v == "ward" || v == "1")
        {
            return WardDivisionType.Ward;
        }
        if (v.Contains("thị trấn") || v == "township" || v == "town" || v == "3")
        {
            return WardDivisionType.Township;
        }

        return WardDivisionType.Commune;
    }

    public static string ToCode(this AdministrativeRegion region)
    {
        return region switch
        {
            AdministrativeRegion.RedRiverDelta => "RED_RIVER_DELTA",
            AdministrativeRegion.Northeast => "NORTHEAST",
            AdministrativeRegion.Northwest => "NORTHWEST",
            AdministrativeRegion.NorthCentral => "NORTH_CENTRAL",
            AdministrativeRegion.SouthCentralCoast => "SOUTH_CENTRAL_COAST",
            AdministrativeRegion.CentralHighlands => "CENTRAL_HIGHLANDS",
            AdministrativeRegion.Southeast => "SOUTHEAST",
            AdministrativeRegion.MekongDelta => "MEKONG_DELTA",
            _ => region.ToString()
        };
    }

    public static string ToDisplayName(this AdministrativeRegion region)
    {
        return region switch
        {
            AdministrativeRegion.RedRiverDelta => "Đồng bằng sông Hồng",
            AdministrativeRegion.Northeast => "Đông Bắc Bộ",
            AdministrativeRegion.Northwest => "Tây Bắc Bộ",
            AdministrativeRegion.NorthCentral => "Bắc Trung Bộ",
            AdministrativeRegion.SouthCentralCoast => "Duyên hải Nam Trung Bộ",
            AdministrativeRegion.CentralHighlands => "Tây Nguyên",
            AdministrativeRegion.Southeast => "Đông Nam Bộ",
            AdministrativeRegion.MekongDelta => "Đồng bằng sông Cửu Long",
            _ => region.ToString()
        };
    }

    public static AdministrativeRegion? ParseAdministrativeRegion(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var v = value.Trim().ToUpperInvariant();
        return v switch
        {
            "RED_RIVER_DELTA" or "ĐỒNG BẰNG SÔNG HỒNG" or "DONG BANG SONG HONG" => AdministrativeRegion.RedRiverDelta,
            "NORTHEAST" or "ĐÔNG BẮC BỘ" or "DONG BAC BO" or "ĐÔNG BẮC" => AdministrativeRegion.Northeast,
            "NORTHWEST" or "TÂY BẮC BỘ" or "TAY BAC BO" or "TÂY BẮC" => AdministrativeRegion.Northwest,
            "NORTH_CENTRAL" or "BẮC TRUNG BỘ" or "BAC TRUNG BO" => AdministrativeRegion.NorthCentral,
            "SOUTH_CENTRAL_COAST" or "DUYÊN HẢI NAM TRUNG BỘ" or "DUYEN HAI NAM TRUNG BO" => AdministrativeRegion.SouthCentralCoast,
            "CENTRAL_HIGHLANDS" or "TÂY NGUYÊN" or "TAY NGUYEN" => AdministrativeRegion.CentralHighlands,
            "SOUTHEAST" or "ĐÔNG NAM BỘ" or "DONG NAM BO" => AdministrativeRegion.Southeast,
            "MEKONG_DELTA" or "ĐỒNG BẰNG SÔNG CỬU LONG" or "DONG BANG SONG CUU LONG" or "TÂY NAM BỘ" => AdministrativeRegion.MekongDelta,
            _ => null
        };
    }
}
