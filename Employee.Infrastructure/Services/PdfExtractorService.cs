using Employee.Application.Common.Interfaces;
using System.Text;
using UglyToad.PdfPig;

namespace Employee.Infrastructure.Services
{
    public class PdfExtractorService : IPdfExtractorService
    {
        public string ExtractTextFromPdf(byte[] pdfBytes)
        {
            if (pdfBytes == null || pdfBytes.Length == 0)
                return string.Empty;

            var result = new StringBuilder();

            using (var document = PdfDocument.Open(pdfBytes))
            {
                foreach (var page in document.GetPages())
                {
                    result.AppendLine(page.Text);
                }
            }

            return result.ToString().Trim();
        }
    }
}
