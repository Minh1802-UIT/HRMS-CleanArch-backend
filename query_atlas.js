const { MongoClient } = require('mongodb');

const uri = process.env.MONGO_URI || process.env.MONGODB_URI;
if (!uri) {
    throw new Error('MONGO_URI is required. Set it before running this script.');
}

async function main() {
    const client = new MongoClient(uri);

    try {
        await client.connect();
        const db = client.db('EmployeeCleanDB');
        const collections = await db.listCollections().toArray();

        console.log("=== Collections in EmployeeCleanDB ===");
        collections.forEach(c => console.log(`- ${c.name}`));
        
        console.log("\n=== Checking job_vacancies ===");
        const jobs = await db.collection("job_vacancies").find({}).toArray();
        console.log(`JobVacancies count: ${jobs.length}`);
        console.log(JSON.stringify(jobs.slice(0, 2), null, 2));

        console.log("\n=== Checking candidates ===");
        const candidates = await db.collection("candidates").find({}).toArray();
        console.log(`Candidates count: ${candidates.length}`);
        console.log(JSON.stringify(candidates.slice(0, 2), null, 2));

    } catch (e) {
        console.error(e);
    } finally {
        await client.close();
    }
}

main().catch(console.error);
