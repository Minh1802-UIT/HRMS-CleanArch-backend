const { MongoClient } = require('C:\\temp\\mongo_check\\node_modules\\mongodb');

async function main() {
    const uri = "mongodb://nguyenvanminh180220_db_user:Pass-180220@ac-nfe7wrl-shard-00-00.kfpckor.mongodb.net:27017,ac-nfe7wrl-shard-00-01.kfpckor.mongodb.net:27017,ac-nfe7wrl-shard-00-02.kfpckor.mongodb.net:27017/?ssl=true&authSource=admin&retryWrites=true&w=majority";
    const client = new MongoClient(uri);

    try {
        await client.connect();
        const db = client.db('EmployeeCleanDB');
        console.log("Connected to MongoDB Atlas.");

        const result = await db.collection('overtime_schedules').updateMany(
            {},
            {
                $rename: { "Reason": "Note" },
                $unset: { "Hours": "", "Status": "" }
            }
        );

        console.log(`Successfully fixed ${result.modifiedCount} overtime records.`);
    } catch (e) {
        console.error("Error connecting to MongoDB:", e);
    } finally {
        await client.close();
    }
}

main().catch(console.error);
