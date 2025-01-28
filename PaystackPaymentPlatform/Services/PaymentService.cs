using Microsoft.EntityFrameworkCore;
using PaystackPaymentPlatform.Data;
using PaystackPaymentPlatform.Models;
using RestSharp;

namespace PaystackPaymentPlatform.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly RestClient _client;

        public PaymentService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _client = new RestClient("https://api.paystack.co");
        }

        public async Task<string> InitializePaymentAsync(string email, decimal amount)
        {
            var request = new RestRequest("transaction/initialize", Method.Post);

            var secretKey = _configuration["Paystack:SecretKey"];
            if (string.IsNullOrWhiteSpace(secretKey))
                throw new Exception("Paystack SecretKey is missing in configuration.");

            request.AddHeader("Authorization", $"Bearer {secretKey}");

            request.AddJsonBody(new { email, amount = amount * 100 });

            Console.WriteLine($"Initializing payment for Email: {email}, Amount: {amount * 100}");

            var response = await _client.ExecuteAsync<PaystackResponse>(request);

            if (response.Data?.Status == true)
            {
                var payment = new Payment
                {
                    Email = email,
                    Amount = amount,
                    Reference = response.Data.Data.Reference,
                    CreatedAt = DateTime.UtcNow,
                    IsSuccessful = false
                };
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                return response.Data.Data.AuthorizationUrl;
            }

            Console.WriteLine($"Paystack Initialization Error: {response.Content}");
            throw new Exception("Failed to initialize payment");
        }

        public async Task<bool> VerifyPaymentAsync(string reference)
        {
            var request = new RestRequest($"transaction/verify/{reference}", Method.Get);

            var secretKey = _configuration["Paystack:SecretKey"];
            if (string.IsNullOrWhiteSpace(secretKey))
                throw new Exception("Paystack SecretKey is missing in configuration.");

            request.AddHeader("Authorization", $"Bearer {secretKey}");

            Console.WriteLine($"Verifying payment with Reference: {reference}");

            var response = await _client.ExecuteAsync<PaystackResponse>(request);

            if (response.Data?.Status == true && response.Data.Data.Status == "success")
            {
                var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Reference == reference);
                if (payment != null)
                {
                    payment.IsSuccessful = true;
                    await _context.SaveChangesAsync();
                }
                return true;
            }

            Console.WriteLine($"Paystack Verification Error: {response.Content}");
            return false;
        }
    }

    public class PaystackResponse
    {
        public bool Status { get; set; }
        public PaystackData Data { get; set; }
    }

    public class PaystackData
    {
        public string Reference { get; set; }
        public string AuthorizationUrl { get; set; }
        public string Status { get; set; }
    }
}
