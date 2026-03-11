const { MongoClient, ObjectId } = require('mongodb');

const uri = "mongodb://nguyenvanminh180220_db_user:Pass-180220@ac-nfe7wrl-shard-00-00.kfpckor.mongodb.net:27017,ac-nfe7wrl-shard-00-01.kfpckor.mongodb.net:27017,ac-nfe7wrl-shard-00-02.kfpckor.mongodb.net:27017/?ssl=true&authSource=admin&retryWrites=true&w=majority";

async function seed() {
  const client = new MongoClient(uri);
  try {
    await client.connect();
    const db = client.db("EmployeeCleanDB");

    // Clean up old bad seed data (camelCase fields)
    const oldBadVacancy = ObjectId.createFromHexString("69b141539c3c3db374aa606c");
    const oldBadCandidate = ObjectId.createFromHexString("69b141539c3c3db374aa606d");
    await db.collection("job_vacancies").deleteOne({ _id: oldBadVacancy });
    await db.collection("candidates").deleteOne({ _id: oldBadCandidate });
    console.log("Cleaned up old bad seed data");

    // 1. Job Vacancy with full JD (PascalCase to match C# driver convention)
    const vacancyId = new ObjectId();
    await db.collection("job_vacancies").insertOne({
      _id: vacancyId,
      Title: "Senior Full-Stack Developer (.NET + Angular)",
      Description: "We are looking for a Senior Full-Stack Developer to join our product engineering team. The ideal candidate will design, develop, and maintain high-performance web applications using .NET 8 on the backend and Angular 17+ on the frontend. You will work closely with the UI/UX team and DevOps engineers to deliver scalable, production-ready features. Responsibilities include building RESTful APIs with C# and ASP.NET Core, architecting MongoDB collections and indexes, writing unit and integration tests, participating in code reviews, and mentoring junior developers. The role requires strong problem-solving skills, experience with cloud deployments (Azure or AWS), and a deep understanding of Clean Architecture and Domain-Driven Design patterns.",
      Vacancies: 2,
      ExpiredDate: new Date("2026-06-30T00:00:00Z"),
      Status: "Open",
      _requirements: [
        "3+ years of experience with C# and .NET (ASP.NET Core)",
        "2+ years of experience with Angular (v14+)",
        "Strong knowledge of MongoDB or other NoSQL databases",
        "Experience with RESTful API design and best practices",
        "Understanding of Clean Architecture and SOLID principles",
        "Experience with Git, CI/CD pipelines, and Docker",
        "Familiarity with cloud services (AWS, Azure, or GCP)",
        "Excellent English communication skills"
      ],
      IsDeleted: false,
      CreatedAt: new Date("2026-03-01T08:00:00Z"),
      CreatedBy: "System",
      UpdatedAt: null,
      UpdatedBy: null,
      Version: 1
    });
    console.log("Job Vacancy inserted:", vacancyId.toString());

    // 2. Candidate linked to the vacancy (PascalCase)
    const candidateId = new ObjectId();
    await db.collection("candidates").insertOne({
      _id: candidateId,
      FullName: "Nguyen Van Test",
      Email: "nguyenvantest@example.com",
      Phone: "0901234567",
      JobVacancyId: vacancyId.toString(),
      Status: "Applied",
      ResumeUrl: "https://www.w3.org/WAI/WCAG21/Techniques/pdf/img/table-word.pdf",
      AppliedDate: new Date("2026-03-10T10:00:00Z"),
      AiScore: null,
      AiMatchingSummary: null,
      ExtractedSkills: null,
      IsDeleted: false,
      CreatedAt: new Date("2026-03-10T10:00:00Z"),
      CreatedBy: "System",
      UpdatedAt: null,
      UpdatedBy: null,
      Version: 1
    });
    console.log("Candidate inserted:", candidateId.toString());
    console.log("\nVacancy ID:", vacancyId.toString());
    console.log("Candidate ID:", candidateId.toString());
  } finally {
    await client.close();
  }
}

seed().catch(console.error);
