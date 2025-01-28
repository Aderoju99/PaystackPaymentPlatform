using Microsoft.AspNetCore.Mvc;
using PaystackPaymentPlatform.DTOs;
using PaystackPaymentPlatform.Services;

namespace PaystackPaymentPlatform.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("initialize")]
        public async Task<IActionResult> InitializePayment([FromBody] PaymentRequestDto request)
        {
            try
            {
                var authorizationUrl = await _paymentService.InitializePaymentAsync(request.Email, request.Amount);
                return Ok(new { AuthorizationUrl = authorizationUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "An error occurred while initializing the payment", Error = ex.Message });
            }
        }


        [HttpGet("verify/{reference}")]
        public async Task<IActionResult> VerifyPayment(string reference)
        {
            try
            {
                var isSuccessful = await _paymentService.VerifyPaymentAsync(reference);
                if (isSuccessful)
                {
                    return Ok(new { Message = "Payment verified successfully" });
                }

                return BadRequest(new { Message = "Payment verification failed" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying payment: {ex.Message}");
                return BadRequest(new { message = "An error occurred while verifying the payment", error = ex.Message });
            }
        }
    }
}
