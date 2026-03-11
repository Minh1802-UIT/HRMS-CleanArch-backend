const { MongoClient } = require('C:\\temp\\mongo_check\\node_modules\\mongodb');

async function main() {
    const uri = "mongodb://nguyenvanminh180220_db_user:Pass-180220@ac-nfe7wrl-shard-00-00.kfpckor.mongodb.net:27017,ac-nfe7wrl-shard-00-01.kfpckor.mongodb.net:27017,ac-nfe7wrl-shard-00-02.kfpckor.mongodb.net:27017/?ssl=true&authSource=admin&retryWrites=true&w=majority";
    const client = new MongoClient(uri);

    try {
        await client.connect();
        const db = client.db('EmployeeCleanDB');
        const count = await db.collection('contracts').countDocuments();
        console.log(`Total contracts: ${count}`);
        const contracts = await db.collection('contracts').find().limit(2).toArray();
        console.log(JSON.stringify(contracts, null, 2));
    } catch (e) {
        console.error(e);
    } finally {
        await client.close();
    }
}

main().catch(console.error);
