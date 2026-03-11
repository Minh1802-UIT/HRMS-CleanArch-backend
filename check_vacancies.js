const { MongoClient } = require('mongodb');

async function main() {
    const uri = "mongodb://localhost:27017";
    const client = new MongoClient(uri);

    try {
        await client.connect();
        const database = client.db('EmployeeCleanDB');
        const collection = database.collection('job_vacancies');
        
        const count = await collection.countDocuments();
        console.log(`Total job vacancies: ${count}`);
        
        const documents = await collection.find({}).limit(5).toArray();
        console.log("Sample documents:");
        console.log(JSON.stringify(documents, null, 2));
    } finally {
        await client.close();
    }
}

main().catch(console.error);
