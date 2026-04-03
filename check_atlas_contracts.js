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
        const col = db.collection('contracts');
        
        // Aggregate to find all unique 'Type' values
        const types = await col.distinct('Type');
        console.log("Distinct Types in DB:", types);

        // Find documents with unexpected Types
        const badContracts = await col.find({ Type: { $nin: ["FixedTerm", "Indefinite", "Probation", "Internship", "PartTime", "Freelance"] } }).limit(5).toArray();
        console.log("Contracts with invalid Types:", JSON.stringify(badContracts, null, 2));

        // Let's also check 'Status' values just in case
        const statuses = await col.distinct('Status');
        console.log("Distinct Statuses in DB:", statuses);

    } catch (e) {
        console.error(e);
    } finally {
        await client.close();
    }
}

main().catch(console.error);
