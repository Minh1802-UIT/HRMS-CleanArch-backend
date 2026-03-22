using Employee.Application.Common.Interfaces;
using Employee.Application.Features.Recruitment.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Employee.Infrastructure.Services
{
    public class CandidateAiService : IAiService
    {
        private readonly HttpClient _httpClient;
        private readonly AiSettings _settings;
        private readonly ILogger<CandidateAiService> _logger;

        public CandidateAiService(
            HttpClient httpClient,
            IOptions<AiSettings> options,
            ILogger<CandidateAiService> logger)
        {
            _httpClient = httpClient;
            _settings = options.Value;
            _logger = logger;
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        }

        public async Task<ParsedCvDto> ParseCvAsync(string cvContent)
        {
            var systemPrompt =
                "You are a highly capable HR Assistant parser. " +
                "Extract the user's details from the provided CV text. " +
                "You MUST respond ONLY with a raw JSON object string. " +
                "Do not include Markdown blocks (like ```json), no intro, no outro, ONLY the JSON string. " +
                "The JSON must have this exact structure: " +
                "{\"FirstName\": \"string\", \"LastName\": \"string\", \"Email\": \"string\", \"PhoneNumber\": \"string\", \"ExtractedSkills\": \"string (comma separated list)\"}";

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
                var dto = JsonSerializer.Deserialize<ParsedCvDto>(resultStr,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return dto ?? new ParsedCvDto
                {
                    ParseError = "AI response was empty. Please try again."
                };
            }
            catch (JsonException jsonEx)
            {
                _logger.LogWarning(jsonEx,
                    "Failed to parse AI CV response as JSON. Response snippet: {ResponseSnippet}",
                    resultStr.Length > 200 ? resultStr.Substring(0, 200) : resultStr);
                return new ParsedCvDto
                {
                    ParseError = "Could not parse the AI response. Please try again or enter details manually."
                };
            }
        }

        public async Task<CandidateScoreDto> ScoreCvAgainstJdAsync(string cvContent, string jobDescription)
        {
            var systemPrompt =
                "You are a highly capable HR Interviewer scoring a candidate's CV against a Job Description. " +
                "Score the candidate out of 100 based on how well their skills and experience match the JD. " +
                "Provide a short matching summary and extracted matching skills. " +
                "You MUST respond ONLY with a raw JSON object string. " +
                "Do not include Markdown blocks (like ```json), no intro, no outro, ONLY the JSON string. " +
                "The JSON must have this exact structure: " +
                "{\"AiScore\": (integer 0-100), \"AiMatchingSummary\": \"string\", \"ExtractedSkills\": \"string (comma separated)\"}";

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
                var dto = JsonSerializer.Deserialize<CandidateScoreDto>(resultStr,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return dto ?? new CandidateScoreDto();
            }
            catch (JsonException jsonEx)
            {
                _logger.LogWarning(jsonEx,
                    "Failed to parse AI scoring response as JSON. Response snippet: {ResponseSnippet}",
                    resultStr.Length > 200 ? resultStr.Substring(0, 200) : resultStr);
                return new CandidateScoreDto
                {
                    AiMatchingSummary = "Could not parse AI response. Please review manually."
                };
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

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_settings.BaseUrl, content);

            // Read response body before throwing so error messages include the actual content
            var responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Groq API returned HTTP {StatusCode}. Response: {ResponseBody}",
                    (int)response.StatusCode, responseBody);
                throw new HttpRequestException(
                    $"Groq API returned {(int)response.StatusCode}. " +
                    $"Message: {responseBody}. The AI service may be temporarily unavailable.");
            }

            using var document = JsonDocument.Parse(responseBody);
            return document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;
        }
    }
}
