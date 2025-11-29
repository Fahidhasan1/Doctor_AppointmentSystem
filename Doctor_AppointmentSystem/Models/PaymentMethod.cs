namespace Doctor_AppointmentSystem.Models
{
    public enum PaymentMethod
    {
        Unknown = 0,
        Cash = 1,
        Card = 2,
        MobileBanking = 3,   // bKash, Nagad, etc.
        OnlineGateway = 4
    }
}
