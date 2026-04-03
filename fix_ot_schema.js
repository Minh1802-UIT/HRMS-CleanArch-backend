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
