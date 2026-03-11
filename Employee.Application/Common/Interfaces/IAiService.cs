using Employee.Application.Features.Recruitment.Dtos;

namespace Employee.Application.Common.Interfaces
{
    public interface IAiService
    {
        /// <summary>
        /// Parses the raw text extracted from a CV into structured standard candidate fields.
        /// </summary>
        /// <param name="cvContent">The raw text from the CV PDF/Word document.</param>
        /// <returns>A structured ParsedCvDto.</returns>
        Task<ParsedCvDto> ParseCvAsync(string cvContent);

        /// <summary>
        /// Scores a candidate's CV against a provided Job Description.
        /// </summary>
        /// <param name="cvContent">The extracted text from the candidate's CV.</param>
        /// <param name="jobDescription">The required skills and description from the Job Vacancy.</param>
        /// <returns>A score (0-100) and a brief matching summary.</returns>
        Task<CandidateScoreDto> ScoreCvAgainstJdAsync(string cvContent, string jobDescription);
    }
}
