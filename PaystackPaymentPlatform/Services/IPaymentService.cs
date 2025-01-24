namespace PaystackPaymentPlatform.Services
{
    public interface IPaymentService
    {
        Task<string> InitializePaymentAsync(string email, decimal amount);
        Task<bool> VerifyPaymentAsync(string reference);
    }
}
