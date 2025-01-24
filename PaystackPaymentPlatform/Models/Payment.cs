namespace PaystackPaymentPlatform.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public decimal Amount { get; set; }
        public string Reference { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsSuccessful { get; set; }
    }
}
