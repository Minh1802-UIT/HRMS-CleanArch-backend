using Employee.Application.Common.Dtos;
using Employee.Application.Common.Interfaces;
using Employee.Application.Features.Recruitment.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Employee.Application.Features.Recruitment.Commands.Candidate.ParseCv
{
    public record ParseCvCommand(byte[] FileBytes) : IRequest<Result<ParsedCvDto>>;

    public class ParseCvCommandHandler : IRequestHandler<ParseCvCommand, Result<ParsedCvDto>>
    {
        private readonly IPdfExtractorService _pdfExtractor;
        private readonly IAiService _aiService;
        private readonly ILogger<ParseCvCommandHandler> _logger;

        public ParseCvCommandHandler(
            IPdfExtractorService pdfExtractor,
            IAiService aiService,
            ILogger<ParseCvCommandHandler> logger)
        {
            _pdfExtractor = pdfExtractor;
            _aiService = aiService;
            _logger = logger;
        }

        public async Task<Result<ParsedCvDto>> Handle(ParseCvCommand request, CancellationToken cancellationToken)
        {
            if (request.FileBytes == null || request.FileBytes.Length == 0)
                return Result<ParsedCvDto>.Failure("File is empty.");

            try
            {
                // 1. Extract raw text from PDF
                var cvText = _pdfExtractor.ExtractTextFromPdf(request.FileBytes);

                if (string.IsNullOrWhiteSpace(cvText))
                    return Result<ParsedCvDto>.Failure("Failed to extract text from the provided PDF.");

                // 2. Call AI to parse structured data
                var parsedDto = await _aiService.ParseCvAsync(cvText);

                return Result<ParsedCvDto>.Success(parsedDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while parsing CV.");
                return Result<ParsedCvDto>.Failure("An error occurred while parsing CV. Please try again.");
            }
        }
    }
}
