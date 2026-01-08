//using System.Net.Http.Headers;
//using System.Text.Json;
//using Microsoft.Extensions.Options;
//using Doctor_AppointmentSystem.Models;

//namespace Doctor_AppointmentSystem.Services
//{
//    public class SslCommerzInitResponse
//    {
//        public string? status { get; set; }
//        public string? sessionkey { get; set; }
//        public string? GatewayPageURL { get; set; }
//        public string? failedreason { get; set; }
//    }

//    public class SslCommerzValidationResponse
//    {
//        public string? status { get; set; }          // VALID / INVALID / FAILED etc
//        public string? tran_id { get; set; }
//        public string? val_id { get; set; }
//        public string? amount { get; set; }
//        public string? currency { get; set; }
//        public string? bank_tran_id { get; set; }
//        public string? card_type { get; set; }
//        public string? store_amount { get; set; }
//    }

//    public interface ISslCommerzService
//    {
//        Task<SslCommerzInitResponse> InitPaymentAsync(Dictionary<string, string> fields);
//        Task<SslCommerzValidationResponse> ValidateAsync(string valId);
//    }

//    public class SslCommerzService : ISslCommerzService
//    {
//        private readonly HttpClient _http;
//        private readonly SslCommerzSettings _settings;

//        public SslCommerzService(HttpClient http, IOptions<SslCommerzSettings> settings)
//        {
//            _http = http;
//            _settings = settings.Value;
//        }

//        public async Task<SslCommerzInitResponse> InitPaymentAsync(Dictionary<string, string> fields)
//        {
//            var url = $"{_settings.BaseUrl}/gwprocess/v3/api.php";

//            // SSLCOMMERZ expects form-urlencoded
//            using var content = new FormUrlEncodedContent(fields);
//            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

//            using var res = await _http.PostAsync(url, content);
//            var json = await res.Content.ReadAsStringAsync();

//            // If something goes wrong, return a clear failure
//            if (!res.IsSuccessStatusCode)
//            {
//                return new SslCommerzInitResponse
//                {
//                    status = "FAILED",
//                    failedreason = $"HTTP {res.StatusCode}: {json}"
//                };
//            }

//            var data = JsonSerializer.Deserialize<SslCommerzInitResponse>(json, new JsonSerializerOptions
//            {
//                PropertyNameCaseInsensitive = true
//            });

//            return data ?? new SslCommerzInitResponse { status = "FAILED", failedreason = "Empty response from gateway." };
//        }

//        public async Task<SslCommerzValidationResponse> ValidateAsync(string valId)
//        {
//            // Recommended format=json for easier parsing
//            var url =
//                $"{_settings.BaseUrl}/validator/api/validationserverAPI.php" +
//                $"?val_id={Uri.EscapeDataString(valId)}" +
//                $"&store_id={Uri.EscapeDataString(_settings.StoreId)}" +
//                $"&store_passwd={Uri.EscapeDataString(_settings.StorePassword)}" +
//                $"&v=1&format=json";

//            using var res = await _http.GetAsync(url);
//            var json = await res.Content.ReadAsStringAsync();

//            if (!res.IsSuccessStatusCode)
//            {
//                return new SslCommerzValidationResponse { status = "FAILED" };
//            }

//            var data = JsonSerializer.Deserialize<SslCommerzValidationResponse>(json, new JsonSerializerOptions
//            {
//                PropertyNameCaseInsensitive = true
//            });

//            return data ?? new SslCommerzValidationResponse { status = "FAILED" };
//        }
//    }
//}


using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Doctor_AppointmentSystem.Models;

namespace Doctor_AppointmentSystem.Services
{
    public class SslCommerzInitResponse
    {
        public string? status { get; set; }
        public string? sessionkey { get; set; }
        public string? GatewayPageURL { get; set; }
        public string? failedreason { get; set; }
    }

    public class SslCommerzValidationResponse
    {
        public string? status { get; set; }          // VALID / INVALID / FAILED etc
        public string? tran_id { get; set; }
        public string? val_id { get; set; }
        public string? amount { get; set; }
        public string? currency { get; set; }
        public string? bank_tran_id { get; set; }
        public string? card_type { get; set; }
        public string? store_amount { get; set; }
    }

    public interface ISslCommerzService
    {
        Task<SslCommerzInitResponse> InitPaymentAsync(Dictionary<string, string> fields);
        Task<SslCommerzValidationResponse> ValidateAsync(string valId);
    }

    public class SslCommerzService : ISslCommerzService
    {
        private readonly HttpClient _http;
        private readonly SslCommerzSettings _settings;

        public SslCommerzService(HttpClient http, IOptions<SslCommerzSettings> settings)
        {
            _http = http;
            _settings = settings.Value;
        }

        public async Task<SslCommerzInitResponse> InitPaymentAsync(Dictionary<string, string> fields)
        {
            var url = $"{_settings.BaseUrl}/gwprocess/v3/api.php";

            using var content = new FormUrlEncodedContent(fields);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            HttpResponseMessage res;
            try
            {
                res = await _http.PostAsync(url, content);
            }
            catch (Exception ex)
            {
                return new SslCommerzInitResponse
                {
                    status = "FAILED",
                    failedreason = $"SSLCOMMERZ unreachable (sandbox): {ex.Message}"
                };
            }

            string json;
            try
            {
                json = await res.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                return new SslCommerzInitResponse
                {
                    status = "FAILED",
                    failedreason = $"Failed to read gateway response: {ex.Message}"
                };
            }

            if (!res.IsSuccessStatusCode)
            {
                return new SslCommerzInitResponse
                {
                    status = "FAILED",
                    failedreason = $"HTTP {res.StatusCode}: {json}"
                };
            }

            SslCommerzInitResponse? data;
            try
            {
                data = JsonSerializer.Deserialize<SslCommerzInitResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                return new SslCommerzInitResponse
                {
                    status = "FAILED",
                    failedreason = $"Invalid JSON from gateway: {ex.Message}"
                };
            }

            return data ?? new SslCommerzInitResponse
            {
                status = "FAILED",
                failedreason = "Empty response from gateway."
            };
        }

        public async Task<SslCommerzValidationResponse> ValidateAsync(string valId)
        {
            var url =
                $"{_settings.BaseUrl}/validator/api/validationserverAPI.php" +
                $"?val_id={Uri.EscapeDataString(valId)}" +
                $"&store_id={Uri.EscapeDataString(_settings.StoreId)}" +
                $"&store_passwd={Uri.EscapeDataString(_settings.StorePassword)}" +
                $"&v=1&format=json";

            HttpResponseMessage res;
            try
            {
                res = await _http.GetAsync(url);
            }
            catch (Exception ex)
            {
                return new SslCommerzValidationResponse
                {
                    status = "FAILED"
                };
            }

            string json;
            try
            {
                json = await res.Content.ReadAsStringAsync();
            }
            catch
            {
                return new SslCommerzValidationResponse
                {
                    status = "FAILED"
                };
            }

            if (!res.IsSuccessStatusCode)
            {
                return new SslCommerzValidationResponse { status = "FAILED" };
            }

            SslCommerzValidationResponse? data;
            try
            {
                data = JsonSerializer.Deserialize<SslCommerzValidationResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return new SslCommerzValidationResponse { status = "FAILED" };
            }

            return data ?? new SslCommerzValidationResponse { status = "FAILED" };
        }
    }
}

