namespace Employee.Application.Features.Recruitment.Dtos
{
    public class ParsedCvDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string ExtractedSkills { get; set; } = string.Empty;
    }

    public class CandidateScoreDto
    {
        public int AiScore { get; set; }
        public string AiMatchingSummary { get; set; } = string.Empty;
        public string ExtractedSkills { get; set; } = string.Empty;
    }
}
