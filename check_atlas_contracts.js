const { MongoClient } = require('C:\\temp\\mongo_check\\node_modules\\mongodb');

async function main() {
    const uri = "mongodb://nguyenvanminh180220_db_user:Pass-180220@ac-nfe7wrl-shard-00-00.kfpckor.mongodb.net:27017,ac-nfe7wrl-shard-00-01.kfpckor.mongodb.net:27017,ac-nfe7wrl-shard-00-02.kfpckor.mongodb.net:27017/?ssl=true&authSource=admin&retryWrites=true&w=majority";
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
