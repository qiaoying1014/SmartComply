using QRCoder;

namespace SmartComply.Helper
{
  public static class QrHelper
  {
    /// <summary>
    /// Generate a PNG byte array for the given URL.
    /// </summary>
    public static byte[] GeneratePngQr(string url, int pixelsPerModule = 20)
    {
      using var qrGen = new QRCodeGenerator();
      using var qrData = qrGen.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
      using var qrCode = new PngByteQRCode(qrData);
      return qrCode.GetGraphic(pixelsPerModule);
    }
  }
}
