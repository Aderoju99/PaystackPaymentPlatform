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
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        [HttpPost("initialize")]
        public async Task<IActionResult> InitializePayment([FromBody] PaymentRequestDto request)
        {
            try
            {
                _logger.LogInformation($"Received payment initialization request for Email: {request.Email}, Amount: {request.Amount}");

                var authorizationUrl = await _paymentService.InitializePaymentAsync(request.Email, request.Amount);

                return Ok(new { AuthorizationUrl = authorizationUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while initializing payment");

                return StatusCode(500, new { Message = "An error occurred while initializing the payment", Error = ex.Message });
            }
        }

        [HttpGet("verify/{reference}")]
        public async Task<IActionResult> VerifyPayment(string reference)
        {
            try
            {
                _logger.LogInformation($"Received payment verification request for Reference: {reference}");

                var isSuccessful = await _paymentService.VerifyPaymentAsync(reference);

                if (isSuccessful)
                {
                    _logger.LogInformation($"Payment verification succeeded for Reference: {reference}");
                    return Ok(new { Message = "Payment verified successfully" });
                }

                _logger.LogWarning($"Payment verification failed for Reference: {reference}");
                return BadRequest(new { Message = "Payment verification failed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while verifying payment");

                return StatusCode(500, new { Message = "An error occurred while verifying the payment", Error = ex.Message });
            }
        }
    }
}
