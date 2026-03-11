const { MongoClient } = require('C:\\temp\\mongo_check\\node_modules\\mongodb');

async function main() {
    const uri = "mongodb://nguyenvanminh180220_db_user:Pass-180220@ac-nfe7wrl-shard-00-00.kfpckor.mongodb.net:27017,ac-nfe7wrl-shard-00-01.kfpckor.mongodb.net:27017,ac-nfe7wrl-shard-00-02.kfpckor.mongodb.net:27017/?ssl=true&authSource=admin&retryWrites=true&w=majority";
    const client = new MongoClient(uri);

    try {
        await client.connect();
        const db = client.db('EmployeeCleanDB');
        const col = db.collection('contracts');
        
        // 1. Fix Integer Statuses
        const statusMap = {
            0: 'Draft',
            1: 'Pending',
            2: 'Active',
            3: 'Expired',
            4: 'Terminated'
        };
        for (const [key, value] of Object.entries(statusMap)) {
            const res = await col.updateMany(
                { Status: parseInt(key) },
                { $set: { Status: value } }
            );
            if (res.modifiedCount > 0)
                console.log(`Updated ${res.modifiedCount} contracts from Status ${key} to ${value}`);
        }

        // 2. Map Permanent to Indefinite
        let res = await col.updateMany(
            { Type: 'Permanent' },
            { $set: { Type: 'Indefinite' } }
        );
        if (res.modifiedCount > 0)
            console.log(`Updated ${res.modifiedCount} 'Permanent' Types to 'Indefinite'`);

        // 3. Migrate flat legacy schema
        const flatContracts = await col.find({ BasicSalary: { $exists: true } }).toArray();
        for (const doc of flatContracts) {
             const updates = {
                 $set: {
                     Salary: {
                         BasicSalary: doc.BasicSalary || 0,
                         TransportAllowance: doc.TransportAllowance || 0,
                         LunchAllowance: doc.LunchAllowance || 0,
                         OtherAllowance: doc.OtherAllowance || 0
                     }
                 },
                 $unset: {
                     BasicSalary: "",
                     TransportAllowance: "",
                     LunchAllowance: "",
                     OtherAllowance: "",
                     InsuranceSalary: "",
                     ContractType: ""
                 }
             };

             if (doc.ContractType && !doc.Type) {
                 updates.$set.Type = (doc.ContractType === 'Permanent') ? 'Indefinite' : doc.ContractType;
             }

             await col.updateOne({ _id: doc._id }, updates);
        }
        if (flatContracts.length > 0) {
            console.log(`Migrated ${flatContracts.length} flat salary contracts to nested Salary schema.`);
        }

        // Just to be safe, get distinct values again
        const distinctTypes = await col.distinct('Type');
        console.log("Distinct Types after migration:", distinctTypes);

    } catch (e) {
        console.error(e);
    } finally {
        await client.close();
    }
}

main().catch(console.error);
