const { MongoClient } = require('C:/Users/asus/AppData/Local/Temp/mongo-test/node_modules/mongodb');

async function main() {
  const uri = "mongodb+srv://nguyenvanminh180220_db_user:Pass-180220@cluster0.kfpckor.mongodb.net/";
  const client = new MongoClient(uri);

  try {
    const db = client.db('EmployeeCleanDB');
    const doc2 = await db.collection('payrolls').findOne({ Month: "02-2026" });
    console.log(JSON.stringify(doc2, null, 2));

  } catch (e) {
    console.error("Error:", e);
  } finally {
    await client.close();
  }
}

main();
