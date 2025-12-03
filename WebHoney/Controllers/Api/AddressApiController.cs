using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WebHoney.Models.ViewModels;

namespace WebHoney.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class AddressApiController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AddressApiController> _logger;
    private const string BaseUrl = "https://provinces.open-api.vn/api/";

    public AddressApiController(IHttpClientFactory httpClientFactory, ILogger<AddressApiController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // Helper method để lấy giá trị string an toàn từ JSON element (hỗ trợ cả string và number)
    private static string GetSafeStringValue(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var prop))
            return "";
        
        try
        {
            if (prop.ValueKind == JsonValueKind.String)
                return prop.GetString() ?? "";
            else if (prop.ValueKind == JsonValueKind.Number)
                return prop.GetRawText();
            else if (prop.ValueKind == JsonValueKind.Null || prop.ValueKind == JsonValueKind.Undefined)
                return "";
            else
                return prop.ToString();
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// Lấy danh sách tất cả tỉnh/thành phố
    /// GET: /api/AddressApi/provinces
    /// </summary>
    [HttpGet("provinces")]
    public async Task<ActionResult<List<ProvinceViewModel>>> GetProvinces()
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            
            var url = $"{BaseUrl}p/";
            _logger.LogInformation("Fetching provinces from: {Url}", url);
            
            string? response = null;
            try
            {
                response = await client.GetStringAsync(url);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch from {Url}, trying alternative: {Message}", url, ex.Message);
                // Thử URL thay thế
                try
                {
                    url = "https://raw.githubusercontent.com/daohoangson/dvhcvn/master/data/tinh_tp.json";
                    _logger.LogInformation("Trying alternative URL: {Url}", url);
                    response = await client.GetStringAsync(url);
                }
                catch (Exception ex2)
                {
                    _logger.LogError(ex2, "All API endpoints failed. Error: {Message}", ex2.Message);
                    // Trả về dữ liệu tĩnh nếu API không hoạt động
                    return Ok(GetStaticProvinces());
                }
            }

            if (string.IsNullOrWhiteSpace(response))
            {
                _logger.LogWarning("Empty response, using static data");
                return Ok(GetStaticProvinces());
            }

            var jsonDoc = JsonDocument.Parse(response);
            var provinces = new List<ProvinceViewModel>();

            // Xử lý cả 2 format: array hoặc object với property
            if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in jsonDoc.RootElement.EnumerateArray())
                {
                    try
                    {
                        var code = GetSafeStringValue(element, "code");
                        if (string.IsNullOrEmpty(code))
                            code = GetSafeStringValue(element, "province_code");
                        if (string.IsNullOrEmpty(code))
                            code = GetSafeStringValue(element, "id");
                        
                        var name = GetSafeStringValue(element, "name");
                        if (string.IsNullOrEmpty(name))
                            name = GetSafeStringValue(element, "province_name");

                        if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(name))
                        {
                            provinces.Add(new ProvinceViewModel
                            {
                                Code = code,
                                Name = name
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing province element: {Message}", ex.Message);
                        continue;
                    }
                }
            }
            else if (jsonDoc.RootElement.ValueKind == JsonValueKind.Object)
            {
                // Nếu là object, có thể có property chứa array
                foreach (var property in jsonDoc.RootElement.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var element in property.Value.EnumerateArray())
                        {
                            try
                            {
                                var code = GetSafeStringValue(element, "code");
                                if (string.IsNullOrEmpty(code))
                                    code = GetSafeStringValue(element, "province_code");
                                
                                var name = GetSafeStringValue(element, "name");
                                if (string.IsNullOrEmpty(name))
                                    name = GetSafeStringValue(element, "province_name");

                                if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(name))
                                {
                                    provinces.Add(new ProvinceViewModel
                                    {
                                        Code = code,
                                        Name = name
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Error parsing province element in object: {Message}", ex.Message);
                                continue;
                            }
                        }
                    }
                }
            }

            if (provinces.Count == 0)
            {
                _logger.LogWarning("No provinces found in response, using static data");
                return Ok(GetStaticProvinces());
            }

            _logger.LogInformation("Loaded {Count} provinces", provinces.Count);
            return Ok(provinces.OrderBy(p => p.Name).ToList());
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching provinces: {Message}, using static data", ex.Message);
            return Ok(GetStaticProvinces());
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout fetching provinces, using static data");
            return Ok(GetStaticProvinces());
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error: {Message}, using static data", ex.Message);
            return Ok(GetStaticProvinces());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching provinces: {Message}, using static data", ex.Message);
            return Ok(GetStaticProvinces());
        }
    }

    /// <summary>
    /// Lấy danh sách quận/huyện theo mã tỉnh/thành phố
    /// GET: /api/AddressApi/districts/{provinceCode}
    /// </summary>
    [HttpGet("districts/{provinceCode}")]
    public async Task<ActionResult<List<DistrictViewModel>>> GetDistricts(string provinceCode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(provinceCode))
            {
                return BadRequest(new { error = "Mã tỉnh/thành phố không hợp lệ" });
            }

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            
            string? response = null;
            Exception? lastException = null;
            
            // Thử endpoint chính: lấy province với districts nested
            try
            {
                var url = $"{BaseUrl}p/{provinceCode}?depth=2";
                _logger.LogInformation("Fetching districts from: {Url}", url);
                response = await client.GetStringAsync(url);
                if (!string.IsNullOrWhiteSpace(response))
                {
                    _logger.LogInformation("Successfully fetched districts from primary endpoint");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Primary endpoint failed: {Message}", ex.Message);
                lastException = ex;
                
                // Thử lấy tất cả districts rồi filter
                try
                {
                    var url2 = $"{BaseUrl}d/";
                    _logger.LogInformation("Trying alternative: fetch all districts from: {Url}", url2);
                    response = await client.GetStringAsync(url2);
                }
                catch (Exception ex2)
                {
                    _logger.LogWarning(ex2, "Alternative endpoint also failed: {Message}", ex2.Message);
                    lastException = ex2;
                }
            }

            if (string.IsNullOrWhiteSpace(response))
            {
                _logger.LogWarning("All API endpoints failed for districts. ProvinceCode: {ProvinceCode}", provinceCode);
                return Ok(new List<DistrictViewModel>());
            }

            var jsonDoc = JsonDocument.Parse(response);
            var districts = new List<DistrictViewModel>();

            // Xử lý nhiều format JSON khác nhau
            if (jsonDoc.RootElement.TryGetProperty("districts", out var districtsElement) && 
                districtsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in districtsElement.EnumerateArray())
                {
                    try
                    {
                        var code = GetSafeStringValue(element, "code");
                        var name = GetSafeStringValue(element, "name");
                        
                        if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(name))
                        {
                            districts.Add(new DistrictViewModel
                            {
                                Code = code,
                                Name = name,
                                ProvinceCode = provinceCode
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing district element: {Message}", ex.Message);
                        continue; // Bỏ qua element lỗi, tiếp tục với element khác
                    }
                }
            }
            else if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
            {
                // Nếu response là array trực tiếp (tất cả districts), filter theo province code
                foreach (var element in jsonDoc.RootElement.EnumerateArray())
                {
                    try
                    {
                        // Xử lý province_code có thể là string hoặc number
                        string elementProvinceCode = GetSafeStringValue(element, "province_code");
                        if (string.IsNullOrEmpty(elementProvinceCode))
                            elementProvinceCode = GetSafeStringValue(element, "provinceCode");
                        if (string.IsNullOrEmpty(elementProvinceCode))
                            elementProvinceCode = GetSafeStringValue(element, "province");
                        
                        // Nếu có province code, chỉ lấy districts của province đó
                        if (!string.IsNullOrEmpty(elementProvinceCode) && elementProvinceCode != provinceCode)
                        {
                            continue;
                        }
                        
                        var code = GetSafeStringValue(element, "code");
                        var name = GetSafeStringValue(element, "name");
                        
                        if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(name))
                        {
                            districts.Add(new DistrictViewModel
                            {
                                Code = code,
                                Name = name,
                                ProvinceCode = provinceCode
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing district element in array: {Message}", ex.Message);
                        continue;
                    }
                }
            }
            else if (jsonDoc.RootElement.ValueKind == JsonValueKind.Object)
            {
                // Nếu là object, tìm property chứa districts
                foreach (var property in jsonDoc.RootElement.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Array && 
                        (property.Name.Contains("district", StringComparison.OrdinalIgnoreCase) ||
                         property.Name.Contains("quan", StringComparison.OrdinalIgnoreCase) ||
                         property.Name.Contains("huyen", StringComparison.OrdinalIgnoreCase)))
                    {
                        foreach (var element in property.Value.EnumerateArray())
                        {
                            try
                            {
                                var code = GetSafeStringValue(element, "code");
                                var name = GetSafeStringValue(element, "name");
                                
                                if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(name))
                                {
                                    districts.Add(new DistrictViewModel
                                    {
                                        Code = code,
                                        Name = name,
                                        ProvinceCode = provinceCode
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Error parsing district element: {Message}", ex.Message);
                                continue;
                            }
                        }
                    }
                }
            }


            _logger.LogInformation("Loaded {Count} districts for province {ProvinceCode}", districts.Count, provinceCode);
            return Ok(districts.OrderBy(d => d.Name).ToList());
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching districts for province {ProvinceCode}: {Message}", provinceCode, ex.Message);
            // Trả về danh sách rỗng thay vì lỗi để form vẫn hoạt động
            return Ok(new List<DistrictViewModel>());
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout fetching districts for province {ProvinceCode}", provinceCode);
            return Ok(new List<DistrictViewModel>());
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error for districts: {Message}", ex.Message);
            return Ok(new List<DistrictViewModel>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching districts for province {ProvinceCode}: {Message}", provinceCode, ex.Message);
            return Ok(new List<DistrictViewModel>());
        }
    }

    /// <summary>
    /// Lấy danh sách phường/xã theo mã quận/huyện
    /// GET: /api/AddressApi/wards/{districtCode}
    /// </summary>
    [HttpGet("wards/{districtCode}")]
    public async Task<ActionResult<List<WardViewModel>>> GetWards(string districtCode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(districtCode))
            {
                return BadRequest(new { error = "Mã quận/huyện không hợp lệ" });
            }

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            
            var url = $"{BaseUrl}d/{districtCode}?depth=2";
            _logger.LogInformation("Fetching wards from: {Url}", url);
            
            var response = await client.GetStringAsync(url);
            
            if (string.IsNullOrWhiteSpace(response))
            {
                _logger.LogWarning("Empty response from wards API");
                return StatusCode(500, new { error = "API trả về dữ liệu rỗng" });
            }

            var jsonDoc = JsonDocument.Parse(response);
            var wards = new List<WardViewModel>();

            if (jsonDoc.RootElement.TryGetProperty("wards", out var wardsElement) && 
                wardsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in wardsElement.EnumerateArray())
                {
                    try
                    {
                        var code = GetSafeStringValue(element, "code");
                        var name = GetSafeStringValue(element, "name");
                        
                        if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(name))
                        {
                            wards.Add(new WardViewModel
                            {
                                Code = code,
                                Name = name,
                                DistrictCode = districtCode
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing ward element: {Message}", ex.Message);
                        continue;
                    }
                }
            }
            else
            {
                _logger.LogWarning("Unexpected JSON format for wards. DistrictCode: {DistrictCode}", districtCode);
                return StatusCode(500, new { error = "Định dạng dữ liệu không đúng" });
            }

            _logger.LogInformation("Loaded {Count} wards for district {DistrictCode}", wards.Count, districtCode);
            return Ok(wards.OrderBy(w => w.Name).ToList());
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching wards for district {DistrictCode}: {Message}", districtCode, ex.Message);
            // Trả về danh sách rỗng thay vì lỗi để form vẫn hoạt động
            return Ok(new List<WardViewModel>());
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout fetching wards for district {DistrictCode}", districtCode);
            return Ok(new List<WardViewModel>());
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error for wards: {Message}", ex.Message);
            return Ok(new List<WardViewModel>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching wards for district {DistrictCode}: {Message}", districtCode, ex.Message);
            return Ok(new List<WardViewModel>());
        }
    }

    // Dữ liệu tĩnh fallback khi API không hoạt động
    private List<ProvinceViewModel> GetStaticProvinces()
    {
        return new List<ProvinceViewModel>
        {
            new() { Code = "01", Name = "Hà Nội" },
            new() { Code = "79", Name = "Hồ Chí Minh" },
            new() { Code = "48", Name = "Đà Nẵng" },
            new() { Code = "92", Name = "Cần Thơ" },
            new() { Code = "24", Name = "Hải Phòng" },
            new() { Code = "30", Name = "Hải Dương" },
            new() { Code = "31", Name = "Hưng Yên" },
            new() { Code = "33", Name = "Hà Nam" },
            new() { Code = "34", Name = "Nam Định" },
            new() { Code = "35", Name = "Thái Bình" },
            new() { Code = "36", Name = "Ninh Bình" },
            new() { Code = "38", Name = "Thanh Hóa" },
            new() { Code = "40", Name = "Nghệ An" },
            new() { Code = "42", Name = "Hà Tĩnh" },
            new() { Code = "45", Name = "Quảng Bình" },
            new() { Code = "46", Name = "Quảng Trị" },
            new() { Code = "49", Name = "Quảng Nam" },
            new() { Code = "51", Name = "Quảng Ngãi" },
            new() { Code = "52", Name = "Bình Định" },
            new() { Code = "54", Name = "Phú Yên" },
            new() { Code = "56", Name = "Khánh Hòa" },
            new() { Code = "58", Name = "Ninh Thuận" },
            new() { Code = "60", Name = "Bình Thuận" },
            new() { Code = "62", Name = "Kon Tum" },
            new() { Code = "64", Name = "Gia Lai" },
            new() { Code = "66", Name = "Đắk Lắk" },
            new() { Code = "67", Name = "Đắk Nông" },
            new() { Code = "68", Name = "Lâm Đồng" },
            new() { Code = "70", Name = "Bình Phước" },
            new() { Code = "72", Name = "Tây Ninh" },
            new() { Code = "74", Name = "Bình Dương" },
            new() { Code = "75", Name = "Đồng Nai" },
            new() { Code = "77", Name = "Bà Rịa - Vũng Tàu" },
            new() { Code = "80", Name = "Long An" },
            new() { Code = "82", Name = "Tiền Giang" },
            new() { Code = "83", Name = "Bến Tre" },
            new() { Code = "84", Name = "Trà Vinh" },
            new() { Code = "86", Name = "Vĩnh Long" },
            new() { Code = "87", Name = "Đồng Tháp" },
            new() { Code = "89", Name = "An Giang" },
            new() { Code = "91", Name = "Kiên Giang" },
            new() { Code = "93", Name = "Hậu Giang" },
            new() { Code = "94", Name = "Sóc Trăng" },
            new() { Code = "95", Name = "Bạc Liêu" },
            new() { Code = "96", Name = "Cà Mau" }
        };
    }
}
