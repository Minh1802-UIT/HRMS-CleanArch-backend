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
