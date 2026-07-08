using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Event.Contracts.IServices;

namespace Event.API.TestControllers
{
    [ApiController]
    [Route("api/payment-test")]
    public class PaymentTestController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentTestController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("charge")]
        public async Task<IActionResult> TestCharge([FromBody] TestChargeRequest request)
        {
            var result = await _paymentService.CreateChargeAsync(
                request.Amount,
                request.Currency,
                request.Token,
                "Test charge description"
            );

            if (result.Success)
            {
                return Ok(new { Success = true, TransactionReference = result.TransactionReference });
            }
            return BadRequest(new { Success = false, Error = result.ErrorMessage });
        }

        [HttpPost("refund")]
        public async Task<IActionResult> TestRefund([FromBody] TestRefundRequest request)
        {
            var result = await _paymentService.CreateRefundAsync(
                request.TransactionReference,
                request.Amount
            );

            if (result.Success)
            {
                return Ok(new { Success = true, RefundReference = result.RefundReference });
            }
            return BadRequest(new { Success = false, Error = result.ErrorMessage });
        }

        [HttpPost("payout")]
        public async Task<IActionResult> TestPayout([FromBody] TestPayoutRequest request)
        {
            var result = await _paymentService.CreatePayoutAsync(
                request.DestinationAccountId,
                request.Amount,
                request.Currency
            );

            if (result.Success)
            {
                return Ok(new { Success = true, TransferReference = result.TransferReference });
            }
            return BadRequest(new { Success = false, Error = result.ErrorMessage });
        }
    }

    public class TestChargeRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "inr";
        public string Token { get; set; } = string.Empty;
    }

    public class TestRefundRequest
    {
        public string TransactionReference { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class TestPayoutRequest
    {
        public string DestinationAccountId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "inr";
    }
}