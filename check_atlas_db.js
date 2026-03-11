const { MongoClient } = require('C:\\temp\\mongo_check\\node_modules\\mongodb');

async function main() {
    const uri = "mongodb+srv://nguyenvanminh180220_db_user:Pass-180220@cluster0.kfpckor.mongodb.net/HRMS";
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
