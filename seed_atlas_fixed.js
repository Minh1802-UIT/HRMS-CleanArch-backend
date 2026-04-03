const { MongoClient, ObjectId } = require('mongodb');

const uri = process.env.MONGO_URI || process.env.MONGODB_URI;
if (!uri) {
    throw new Error('MONGO_URI is required. Set it before running this script.');
}

async function main() {
    const client = new MongoClient(uri);

    try {
        await client.connect();
        const db = client.db('EmployeeCleanDB');
        const jobsCol = db.collection('job_vacancies');
        const candidatesCol = db.collection('candidates');

        // Create some Jobs
        const newJobs = [
            {
                _id: "6614451c8e31a0e1c2a12301",
                IsDeleted: false,
                CreatedAt: new Date(),
                CreatedBy: "System",
                Version: 1,
                Title: "Frontend Developer (Angular)",
                Description: "We are looking for a strong frontend engineer with Angular.",
                Vacancies: 3,
                ExpiredDate: new Date("2026-12-31"),
                Status: "Open", 
                _requirements: ["3+ years Angular", "TypeScript expert", "RxJS"]
            },
            {
                _id: "6614451c8e31a0e1c2a12302",
                IsDeleted: false,
                CreatedAt: new Date(),
                CreatedBy: "System",
                Version: 1,
                Title: "Backend Developer (.NET)",
                Description: "Looking for an experienced C# .NET Core engineer.",
                Vacancies: 2,
                ExpiredDate: new Date("2026-06-30"),
                Status: "Open",
                _requirements: [".NET 8", "Clean Architecture", "MongoDB"]
            },
            {
                _id: "6614451c8e31a0e1c2a12303",
                IsDeleted: false,
                CreatedAt: new Date(),
                CreatedBy: "System",
                Version: 1,
                Title: "UX/UI Designer",
                Description: "Creative designer for web applications.",
                Vacancies: 1,
                ExpiredDate: new Date("2026-08-15"),
                Status: "Open",
                _requirements: ["Figma", "UI/UX principles", "Portfolio"]
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
                IsDeleted: false,
                CreatedAt: new Date(),
                CreatedBy: "System",
                Version: 1,
                FullName: "Alice Johnson",
                Email: "alice@example.com",
                Phone: "555-0101",
                JobVacancyId: "6614451c8e31a0e1c2a12301",
                Status: "Applied",
                ResumeUrl: "https://example.com/alice-cv.pdf",
                AppliedDate: new Date("2026-03-01")
            },
            {
                _id: "6614451c8e31a0e1c2a12402",
                IsDeleted: false,
                CreatedAt: new Date(),
                CreatedBy: "System",
                Version: 1,
                FullName: "Bob Smith",
                Email: "bob@example.com",
                Phone: "555-0102",
                JobVacancyId: "6614451c8e31a0e1c2a12301",
                Status: "Interviewing",
                ResumeUrl: "https://example.com/bob-cv.pdf",
                AppliedDate: new Date("2026-03-05")
            },
            {
                _id: "6614451c8e31a0e1c2a12403",
                IsDeleted: false,
                CreatedAt: new Date(),
                CreatedBy: "System",
                Version: 1,
                FullName: "Charlie Brown",
                Email: "charlie@example.com",
                Phone: "555-0103",
                JobVacancyId: "6614451c8e31a0e1c2a12302",
                Status: "Test",
                ResumeUrl: "https://example.com/charlie-cv.pdf",
                AppliedDate: new Date("2026-02-15")
            },
            {
                _id: "6614451c8e31a0e1c2a12404",
                IsDeleted: false,
                CreatedAt: new Date(),
                CreatedBy: "System",
                Version: 1,
                FullName: "Diana Prince",
                Email: "diana@example.com",
                Phone: "555-0104",
                JobVacancyId: "6614451c8e31a0e1c2a12303",
                Status: "Applied",
                ResumeUrl: "https://example.com/diana-cv.pdf",
                AppliedDate: new Date("2026-03-10")
            },
            {
                _id: "6614451c8e31a0e1c2a12405",
                IsDeleted: false,
                CreatedAt: new Date(),
                CreatedBy: "System",
                Version: 1,
                FullName: "Edward Elric",
                Email: "edward@example.com",
                Phone: "555-0105",
                JobVacancyId: "6614451c8e31a0e1c2a12301",
                Status: "Hired",
                ResumeUrl: "https://example.com/edward-cv.pdf",
                AppliedDate: new Date("2026-01-20")
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
