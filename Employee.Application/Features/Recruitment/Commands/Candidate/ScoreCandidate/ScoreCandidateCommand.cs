using Employee.Application.Common.Dtos;
using Employee.Application.Common.Interfaces;
using Employee.Domain.Interfaces.Repositories;
using MediatR;
using System.Net.Http;

namespace Employee.Application.Features.Recruitment.Commands.Candidate.ScoreCandidate
{
    public record ScoreCandidateCommand(string CandidateId) : IRequest<Result<bool>>;

    public class ScoreCandidateCommandHandler : IRequestHandler<ScoreCandidateCommand, Result<bool>>
    {
        private readonly ICandidateRepository _candidateRepository;
        private readonly IJobVacancyRepository _vacancyRepository;
        private readonly IPdfExtractorService _pdfExtractor;
        private readonly IAiService _aiService;

        public ScoreCandidateCommandHandler(
            ICandidateRepository candidateRepository, 
            IJobVacancyRepository vacancyRepository,
            IPdfExtractorService pdfExtractor,
            IAiService aiService)
        {
            _candidateRepository = candidateRepository;
            _vacancyRepository = vacancyRepository;
            _pdfExtractor = pdfExtractor;
            _aiService = aiService;
        }

        public async Task<Result<bool>> Handle(ScoreCandidateCommand request, CancellationToken cancellationToken)
        {
            // 1. Retrieve Candidate
            var candidate = await _candidateRepository.GetByIdAsync(request.CandidateId);
            if (candidate == null)
                return Result<bool>.Failure("Candidate not found.");

            if (string.IsNullOrWhiteSpace(candidate.ResumeUrl))
                return Result<bool>.Failure("Candidate does not have a resume to score.");

            // 2. Retrieve Job Vacancy
            var vacancy = await _vacancyRepository.GetByIdAsync(candidate.JobVacancyId);
            if (vacancy == null)
                return Result<bool>.Failure("Job Vacancy not found.");

            try
            {
                // 3. Download the resume from the URL
                byte[] resumeBytes;
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync(candidate.ResumeUrl, cancellationToken);
                    if (!response.IsSuccessStatusCode)
                        return Result<bool>.Failure($"Could not download resume. The file at '{candidate.ResumeUrl}' returned HTTP {(int)response.StatusCode}. The file may have been deleted from storage.");

                    resumeBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                }

                // 4. Extract Text
                var cvText = _pdfExtractor.ExtractTextFromPdf(resumeBytes);
                if (string.IsNullOrWhiteSpace(cvText))
                    return Result<bool>.Failure("Could not extract text from the resume PDF. The file may be empty or image-based.");

                // 5. Call AI
                var jdText = $"Title: {vacancy.Title}\nDescription: {vacancy.Description}\nRequirements: {vacancy.Requirements}";
                var scoreResult = await _aiService.ScoreCvAgainstJdAsync(cvText, jdText);

                // 6. Update Entity & Database
                candidate.UpdateAiScore(scoreResult.AiScore, scoreResult.AiMatchingSummary, scoreResult.ExtractedSkills);

                await _candidateRepository.UpdateAsync(candidate.Id, candidate, cancellationToken);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to score candidate: {ex.Message}");
            }
        }
    }
}
