namespace Employee.Application.Common.Interfaces
{
    public interface IPdfExtractorService
    {
        /// <summary>
        /// Reads purely the text from a PDF file stream or byte array.
        /// </summary>
        string ExtractTextFromPdf(byte[] pdfBytes);
    }
}
