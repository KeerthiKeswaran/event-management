using System;
using System.IO;
using System.Threading.Tasks;
using QRCoder;
using Event.Contracts.IServices;

namespace Event.Business.Services
{
    public class QrCodeService : IQrCodeService
    {
        #region GenerateQrCodeAsync

        public Task<byte[]> GenerateQrCodeAsync(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentException("Text to encode cannot be null or empty.", nameof(text));
            }

            using (var qrGenerator = new QRCodeGenerator())
            {
                using (var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q))
                {
                    using (var qrCode = new PngByteQRCode(qrCodeData))
                    {
                        byte[] qrCodeBytes = qrCode.GetGraphic(20);
                        return Task.FromResult(qrCodeBytes);
                    }
                }
            }
        }

        #endregion
    }
}
