namespace AuthApi.Domain.Enums;

public static class AdministrativeEnumExtensions
{
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
        if (v.Contains("thành phố") || v.Contains("trung ương") || v.Contains("municipality") || v == "2")
        {
            return ProvinceDivisionType.Municipality;
        }

        return ProvinceDivisionType.Province;
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
        if (v.Contains("phường") || v.Contains("ward") || v == "1")
        {
            return WardDivisionType.Ward;
        }
        if (v.Contains("thị trấn") || v.Contains("township") || v.Contains("town") || v == "3")
        {
            return WardDivisionType.Township;
        }

        return WardDivisionType.Commune;
    }
}
