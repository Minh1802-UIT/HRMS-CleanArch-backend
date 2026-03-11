const { MongoClient, ObjectId } = require('C:\\temp\\mongo_check\\node_modules\\mongodb');

async function main() {
    // Connect to the local MongoDB where the backend points to
    const uri = "mongodb://localhost:27017";
    const client = new MongoClient(uri);

    try {
        await client.connect();
        const db = client.db('EmployeeCleanDB');
        const jobsCol = db.collection('JobVacancies');
        const candidatesCol = db.collection('Candidates');

        // Create some Jobs
        const newJobs = [
            {
                _id: "6614451c8e31a0e1c2a12301",
                Title: "Frontend Developer (Angular)",
                Description: "We are looking for a strong frontend engineer with Angular.",
                Vacancies: 3,
                ExpiredDate: new Date("2026-12-31"),
                Status: 0, // Open 
                Requirements: ["3+ years Angular", "TypeScript expert", "RxJS"],
                CreatedAt: new Date(),
                Version: 1
            },
            {
                _id: "6614451c8e31a0e1c2a12302",
                Title: "Backend Developer (.NET)",
                Description: "Looking for an experienced C# .NET Core engineer.",
                Vacancies: 2,
                ExpiredDate: new Date("2026-06-30"),
                Status: 0, // Open
                Requirements: [".NET 8", "Clean Architecture", "MongoDB"],
                CreatedAt: new Date(),
                Version: 1
            },
            {
                _id: "6614451c8e31a0e1c2a12303",
                Title: "UX/UI Designer",
                Description: "Creative designer for web applications.",
                Vacancies: 1,
                ExpiredDate: new Date("2026-08-15"),
                Status: 0, // Open
                Requirements: ["Figma", "UI/UX principles", "Portfolio"],
                CreatedAt: new Date(),
                Version: 1
            }
        ];

        // Ensure we don't duplicate on multiple runs for safe measure
        for (const job of newJobs) {
            await jobsCol.updateOne({ _id: job._id }, { $set: job }, { upsert: true });
        }
        console.log(`Inserted/Updated ${newJobs.length} Job Vacancies.`);

        // Candidates
        const newCandidates = [
            {
                _id: "6614451c8e31a0e1c2a12401",
                FullName: "Alice Johnson",
                Email: "alice@example.com",
                Phone: "555-0101",
                JobVacancyId: "6614451c8e31a0e1c2a12301",
                Status: 0, // Applied
                ResumeUrl: "https://example.com/alice-cv.pdf",
                AppliedDate: new Date("2026-03-01"),
                CreatedAt: new Date(),
                Version: 1
            },
            {
                _id: "6614451c8e31a0e1c2a12402",
                FullName: "Bob Smith",
                Email: "bob@example.com",
                Phone: "555-0102",
                JobVacancyId: "6614451c8e31a0e1c2a12301",
                Status: 1, // Interviewing
                ResumeUrl: "https://example.com/bob-cv.pdf",
                AppliedDate: new Date("2026-03-05"),
                CreatedAt: new Date(),
                Version: 1
            },
            {
                _id: "6614451c8e31a0e1c2a12403",
                FullName: "Charlie Brown",
                Email: "charlie@example.com",
                Phone: "555-0103",
                JobVacancyId: "6614451c8e31a0e1c2a12302",
                Status: 2, // Test
                ResumeUrl: "https://example.com/charlie-cv.pdf",
                AppliedDate: new Date("2026-02-15"),
                CreatedAt: new Date(),
                Version: 1
            },
            {
                _id: "6614451c8e31a0e1c2a12404",
                FullName: "Diana Prince",
                Email: "diana@example.com",
                Phone: "555-0104",
                JobVacancyId: "6614451c8e31a0e1c2a12303",
                Status: 0, // Applied
                ResumeUrl: "https://example.com/diana-cv.pdf",
                AppliedDate: new Date("2026-03-10"),
                CreatedAt: new Date(),
                Version: 1
            },
            {
                _id: "6614451c8e31a0e1c2a12405",
                FullName: "Edward Elric",
                Email: "edward@example.com",
                Phone: "555-0105",
                JobVacancyId: "6614451c8e31a0e1c2a12301",
                Status: 3, // Hired
                ResumeUrl: "https://example.com/edward-cv.pdf",
                AppliedDate: new Date("2026-01-20"),
                CreatedAt: new Date(),
                Version: 1
            }
        ];

        for (const candidate of newCandidates) {
            await candidatesCol.updateOne({ _id: candidate._id }, { $set: candidate }, { upsert: true });
        }
        console.log(`Inserted/Updated ${newCandidates.length} Candidates.`);

    } catch (e) {
        console.error(e);
    } finally {
        await client.close();
    }
}

main().catch(console.error);
