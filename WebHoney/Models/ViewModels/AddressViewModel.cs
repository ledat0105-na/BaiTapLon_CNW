namespace WebHoney.Models.ViewModels;

public class ProvinceViewModel
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class DistrictViewModel
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ProvinceCode { get; set; } = string.Empty;
}

public class WardViewModel
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DistrictCode { get; set; } = string.Empty;
}

