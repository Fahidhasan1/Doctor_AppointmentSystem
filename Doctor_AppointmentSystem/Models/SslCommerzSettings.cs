namespace Doctor_AppointmentSystem.Models
{
    public class SslCommerzSettings
    {
        public string StoreId { get; set; } = "";
        public string StorePassword { get; set; } = "";
        public string BaseUrl { get; set; } = "https://sandbox.sslcommerz.com";
        public string CallbackBaseUrl { get; set; } = "http://localhost:5243";
    }
}
