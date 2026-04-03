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
