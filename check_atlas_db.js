const { MongoClient } = require('mongodb');

const uri = process.env.MONGO_URI || process.env.MONGODB_URI;
if (!uri) {
    throw new Error('MONGO_URI is required. Set it before running this script.');
}

async function main() {
    const client = new MongoClient(uri);

    try {
        await client.connect();
        const adminDb = client.db().admin();
        const dbList = await adminDb.listDatabases();
        console.log("Databases:");
        dbList.databases.forEach(db => console.log(` - ${db.name}`));

        // Try to access HRMS or EmployeeDB depending on what exists
        const targetDbName = dbList.databases.find(d => d.name === 'EmployeeDB') ? 'EmployeeDB' : 'HRMS';
        console.log(`\nConnecting to: ${targetDbName}`);
        
        const db = client.db(targetDbName);
        const collections = await db.listCollections().toArray();
        console.log("\nCollections:");
        collections.forEach(col => console.log(` - ${col.name}`));

        if (collections.find(c => c.name === 'contracts')) {
            const count = await db.collection('contracts').countDocuments();
            console.log(`\nTotal contracts in ${targetDbName}: ${count}`);
        } else {
             console.log(`\nNo contracts collection in ${targetDbName}`);
        }

    } catch (e) {
        console.error(e);
    } finally {
        await client.close();
    }
}

main().catch(console.error);
