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
        console.log("\n=== Data Preview ===");

        for (let col of collections) {
            console.log(`\n--- Collection: ${col.name} ---`);
            const docs = await db.collection(col.name).find({}).limit(5).toArray();
            console.log(JSON.stringify(docs, null, 2));
            const count = await db.collection(col.name).countDocuments();
            console.log(`Total documents: ${count}`);
        }

    } catch (e) {
        console.error(e);
    } finally {
        await client.close();
    }
}

main().catch(console.error);
