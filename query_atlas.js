const { MongoClient } = require('C:\\temp\\mongo_check\\node_modules\\mongodb');

async function main() {
    const uri = "mongodb://nguyenvanminh180220_db_user:Pass-180220@ac-nfe7wrl-shard-00-00.kfpckor.mongodb.net:27017,ac-nfe7wrl-shard-00-01.kfpckor.mongodb.net:27017,ac-nfe7wrl-shard-00-02.kfpckor.mongodb.net:27017/?ssl=true&authSource=admin&retryWrites=true&w=majority";
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
