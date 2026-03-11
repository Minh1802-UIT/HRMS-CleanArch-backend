using Employee.Application.Common.Interfaces;
using Employee.Application.Features.Recruitment.Dtos;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Employee.Infrastructure.Services
{
    public class CandidateAiService : IAiService
    {
        private readonly HttpClient _httpClient;
        private readonly AiSettings _settings;

        public CandidateAiService(HttpClient httpClient, IOptions<AiSettings> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        }

        public async Task<ParsedCvDto> ParseCvAsync(string cvContent)
        {
            var systemPrompt = "You are a highly capable HR Assistant parser. Extract the user's details from the provided CV text. " +
                               "You MUST respond ONLY with a raw JSON object string. Do not include Markdown blocks (like ```json), no intro, no outro, ONLY the JSON string. " +
                               "The JSON must have this exact structure: {\"FirstName\": \"string\", \"LastName\": \"string\", \"Email\": \"string\", \"PhoneNumber\": \"string\", \"ExtractedSkills\": \"string (comma separated list)\"}";

            var resultStr = await CallAiAsync(systemPrompt, $"CV TEXT:\n{cvContent}");
            resultStr = resultStr.Trim();
            if (resultStr.StartsWith("```json"))
            {
                resultStr = resultStr.Substring(7);
                resultStr = resultStr.Substring(0, resultStr.LastIndexOf("```"));
                resultStr = resultStr.Trim();
            }

            try
            {
                var dto = JsonSerializer.Deserialize<ParsedCvDto>(resultStr, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return dto ?? new ParsedCvDto();
            }
            catch
            {
                return new ParsedCvDto();
            }
        }

        public async Task<CandidateScoreDto> ScoreCvAgainstJdAsync(string cvContent, string jobDescription)
        {
            var systemPrompt = "You are a highly capable HR Interviewer scoring an candidate's CV against a Job Description. " +
                               "Score the candidate out of 100 based on how well their skills and experience match the JD. Provide a short matching summary and extracted matching skills. " +
                               "You MUST respond ONLY with a raw JSON object string. Do not include Markdown blocks (like ```json), no intro, no outro, ONLY the JSON string. " +
                               "The JSON must have this exact structure: {\"AiScore\": (integer 0-100), \"AiMatchingSummary\": \"string\", \"ExtractedSkills\": \"string (comma separated)\"}";

            var userPrompt = $"JOB DESCRIPTION:\n{jobDescription}\n\nCANDIDATE CV:\n{cvContent}";

            var resultStr = await CallAiAsync(systemPrompt, userPrompt);
            resultStr = resultStr.Trim();
            if (resultStr.StartsWith("```json"))
            {
                resultStr = resultStr.Substring(7);
                resultStr = resultStr.Substring(0, resultStr.LastIndexOf("```"));
                resultStr = resultStr.Trim();
            }

            try
            {
                var dto = JsonSerializer.Deserialize<CandidateScoreDto>(resultStr, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return dto ?? new CandidateScoreDto();
            }
            catch
            {
                return new CandidateScoreDto();
            }
        }

        private async Task<string> CallAiAsync(string systemPrompt, string userPrompt)
        {
            var requestBody = new
            {
                model = _settings.Model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.2
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_settings.BaseUrl, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(responseJson);
            
            return document.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? string.Empty;
        }
    }
}
