using RestSharp;
using Microsoft.Extensions.Configuration;
using PaystackPaymentPlatform.Models;
using PaystackPaymentPlatform.Data;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;

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

            // Send the request to Paystack API
            var response = await _client.ExecuteAsync<PaystackResponse>(request);

            // Check if the request was successful
            if (response.IsSuccessful)
            {
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
                else
                {
                    Console.WriteLine($"Paystack API Error: {response.Data?.Message}");
                    throw new Exception($"Paystack API Error: {response.Data?.Message}");
                }
            }
            else
            {
                Console.WriteLine($"Error initializing payment: {response.StatusCode} - {response.Content}");
                Console.WriteLine($"Response Details: {response.ErrorMessage}");
                throw new Exception($"Failed to initialize payment. Response: {response.Content}");
            }
        }

        public async Task<bool> VerifyPaymentAsync(string reference)
        {
            var request = new RestRequest($"transaction/verify/{reference}", Method.Get);

            var secretKey = _configuration["Paystack:SecretKey"];
            if (string.IsNullOrWhiteSpace(secretKey))
                throw new Exception("Paystack SecretKey is missing in configuration.");

            request.AddHeader("Authorization", $"Bearer {secretKey}");

            var response = await _client.ExecuteAsync<PaystackResponse>(request);

            // Check if the verification was successful
            if (response.IsSuccessful)
            {
                if (response.Data?.Status == true && response.Data.Data.Status == "success")
                {
                    // If successful, update the payment status in the database
                    var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Reference == reference);
                    if (payment != null)
                    {
                        payment.IsSuccessful = true;
                        await _context.SaveChangesAsync();
                    }

                    return true;
                }
                else
                {
                    Console.WriteLine($"Paystack Verification Error: {response.Data?.Message}");
                    return false;
                }
            }
            else
            {
                Console.WriteLine($"Error verifying payment: {response.StatusCode} - {response.Content}");
                throw new Exception($"Failed to verify payment. Response: {response.Content}");
            }
        }
    }

    public class PaystackResponse
    {
        public bool Status { get; set; } // Indicates whether the request was successful
        public PaystackData Data { get; set; } // Contains the transaction data
        public string Message { get; set; } // Error message if something went wrong
    }

    public class PaystackData
    {
        public string Reference { get; set; } // Transaction reference ID
        public string AuthorizationUrl { get; set; } // URL for the user to authorize the payment
        public string Status { get; set; } // Transaction status (e.g., "success")
    }
}
