const { MongoClient, ObjectId } = require('C:\\temp\\mongo_check\\node_modules\\mongodb');

async function main() {
    const uri = "mongodb://nguyenvanminh180220_db_user:Pass-180220@ac-nfe7wrl-shard-00-00.kfpckor.mongodb.net:27017,ac-nfe7wrl-shard-00-01.kfpckor.mongodb.net:27017,ac-nfe7wrl-shard-00-02.kfpckor.mongodb.net:27017/?ssl=true&authSource=admin&retryWrites=true&w=majority";
    const client = new MongoClient(uri);

    try {
        await client.connect();
        const db = client.db('EmployeeCleanDB');
        const jobsCol = db.collection('job_vacancies');
        const candidatesCol = db.collection('candidates');

        // Clear previous runs
        await jobsCol.deleteMany({ _id: { $in: [new ObjectId("6614451c8e31a0e1c2a12301"), new ObjectId("6614451c8e31a0e1c2a12302"), new ObjectId("6614451c8e31a0e1c2a12303"), "6614451c8e31a0e1c2a12301", "6614451c8e31a0e1c2a12302", "6614451c8e31a0e1c2a12303"] } });
        await candidatesCol.deleteMany({ _id: { $in: [new ObjectId("6614451c8e31a0e1c2a12401"), new ObjectId("6614451c8e31a0e1c2a12402"), new ObjectId("6614451c8e31a0e1c2a12403"), new ObjectId("6614451c8e31a0e1c2a12404"), new ObjectId("6614451c8e31a0e1c2a12405"), "6614451c8e31a0e1c2a12401", "6614451c8e31a0e1c2a12402", "6614451c8e31a0e1c2a12403", "6614451c8e31a0e1c2a12404", "6614451c8e31a0e1c2a12405"] } });

        // Insert fresh jobs with precise fields and ObjectId types
        const newJobs = [
            {
                _id: new ObjectId("6614451c8e31a0e1c2a12301"),
                IsDeleted: false,
                CreatedAt: new Date("2026-03-10T12:00:00Z"),
                CreatedBy: "System",
                UpdatedAt: null,
                UpdatedBy: null,
                Version: 1,
                Title: "Frontend Developer (Angular)",
                Description: "We are looking for a strong frontend engineer with Angular.",
                Vacancies: 3,
                ExpiredDate: new Date("2026-12-31T00:00:00Z"),
                Status: "Open"
            },
            {
                _id: new ObjectId("6614451c8e31a0e1c2a12302"),
                IsDeleted: false,
                CreatedAt: new Date("2026-03-10T12:00:00Z"),
                CreatedBy: "System",
                UpdatedAt: null,
                UpdatedBy: null,
                Version: 1,
                Title: "Backend Developer (.NET)",
                Description: "Looking for an experienced C# .NET Core engineer.",
                Vacancies: 2,
                ExpiredDate: new Date("2026-06-30T00:00:00Z"),
                Status: "Open"
            },
            {
                _id: new ObjectId("6614451c8e31a0e1c2a12303"),
                IsDeleted: false,
                CreatedAt: new Date("2026-03-10T12:00:00Z"),
                CreatedBy: "System",
                UpdatedAt: null,
                UpdatedBy: null,
                Version: 1,
                Title: "UX/UI Designer",
                Description: "Creative designer for web applications.",
                Vacancies: 1,
                ExpiredDate: new Date("2026-08-15T00:00:00Z"),
                Status: "Open"
            }
        ];

        await jobsCol.insertMany(newJobs);
        console.log(`Inserted ${newJobs.length} Job Vacancies with ObjectIds.`);

        // Candidates - using String for JobVacancyId because the class defines it as string
        // The original Candidate has JobVacancyId as string (e.g. "69a06b1c3f2a5758a4364d49")
        // But wait! Let me check the class mapping... C# might map string properties representing object IDs to actual string in MongoDB unless specified.
        // Actually earlier `query_atlas.js` printed candidate JobVacancyId as string, so we'll leave it as string.
        const newCandidates = [
            {
                _id: new ObjectId("6614451c8e31a0e1c2a12401"),
                IsDeleted: false,
                CreatedAt: new Date("2026-03-10T14:00:00Z"),
                CreatedBy: "System",
                UpdatedAt: null,
                UpdatedBy: null,
                Version: 1,
                FullName: "Alice Johnson",
                Email: "alice@example.com",
                Phone: "555-0101",
                JobVacancyId: "6614451c8e31a0e1c2a12301",
                Status: "Applied",
                ResumeUrl: "https://example.com/alice-cv.pdf",
                AppliedDate: new Date("2026-03-01T00:00:00Z")
            },
            {
                _id: new ObjectId("6614451c8e31a0e1c2a12402"),
                IsDeleted: false,
                CreatedAt: new Date("2026-03-10T14:00:00Z"),
                CreatedBy: "System",
                UpdatedAt: null,
                UpdatedBy: null,
                Version: 1,
                FullName: "Bob Smith",
                Email: "bob@example.com",
                Phone: "555-0102",
                JobVacancyId: "6614451c8e31a0e1c2a12301",
                Status: "Interviewing",
                ResumeUrl: "https://example.com/bob-cv.pdf",
                AppliedDate: new Date("2026-03-05T00:00:00Z")
            },
            {
                _id: new ObjectId("6614451c8e31a0e1c2a12403"),
                IsDeleted: false,
                CreatedAt: new Date("2026-03-10T14:00:00Z"),
                CreatedBy: "System",
                UpdatedAt: null,
                UpdatedBy: null,
                Version: 1,
                FullName: "Charlie Brown",
                Email: "charlie@example.com",
                Phone: "555-0103",
                JobVacancyId: "6614451c8e31a0e1c2a12302",
                Status: "Test",
                ResumeUrl: "https://example.com/charlie-cv.pdf",
                AppliedDate: new Date("2026-02-15T00:00:00Z")
            },
            {
                _id: new ObjectId("6614451c8e31a0e1c2a12404"),
                IsDeleted: false,
                CreatedAt: new Date("2026-03-10T14:00:00Z"),
                CreatedBy: "System",
                UpdatedAt: null,
                UpdatedBy: null,
                Version: 1,
                FullName: "Diana Prince",
                Email: "diana@example.com",
                Phone: "555-0104",
                JobVacancyId: "6614451c8e31a0e1c2a12303",
                Status: "Applied",
                ResumeUrl: "https://example.com/diana-cv.pdf",
                AppliedDate: new Date("2026-03-10T00:00:00Z")
            },
            {
                _id: new ObjectId("6614451c8e31a0e1c2a12405"),
                IsDeleted: false,
                CreatedAt: new Date("2026-03-10T14:00:00Z"),
                CreatedBy: "System",
                UpdatedAt: null,
                UpdatedBy: null,
                Version: 1,
                FullName: "Edward Elric",
                Email: "edward@example.com",
                Phone: "555-0105",
                JobVacancyId: "6614451c8e31a0e1c2a12301",
                Status: "Hired",
                ResumeUrl: "https://example.com/edward-cv.pdf",
                AppliedDate: new Date("2026-01-20T00:00:00Z")
            }
        ];

        await candidatesCol.insertMany(newCandidates);
        console.log(`Inserted ${newCandidates.length} Candidates with ObjectIds.`);

    } catch (e) {
        console.error(e);
    } finally {
        await client.close();
    }
}

main().catch(console.error);
