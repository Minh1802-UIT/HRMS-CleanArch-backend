namespace Employee.Application.Features.Recruitment.Dtos;

public class RecruitmentOptionsDto
{
  public List<string> Offices { get; set; } = new()
  {
    "New York",
    "London",
    "Remote",
    "San Francisco",
    "Singapore",
    "Tokyo"
  };

  public List<string> EmploymentTypes { get; set; } = new()
  {
    "Full time",
    "Part time",
    "Contract",
    "Internship",
    "Freelance"
  };
}
