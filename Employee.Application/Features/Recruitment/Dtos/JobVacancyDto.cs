namespace Employee.Application.Features.Recruitment.Dtos
{
  // Create/Update DTO
  public class JobVacancyDto
  {
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Vacancies { get; set; }
    public DateTime ExpiredDate { get; set; }
    public List<string> Requirements { get; set; } = new();
  }

  // Response DTO
  public class JobVacancyResponseDto
  {
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Vacancies { get; set; }
    public DateTime ExpiredDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> Requirements { get; set; } = new();
    public DateTime CreatedAt { get; set; }
  }
}
