const { MongoClient } = require('C:\\temp\\mongo_check\\node_modules\\mongodb');

async function main() {
    const uri = "mongodb://nguyenvanminh180220_db_user:Pass-180220@ac-nfe7wrl-shard-00-00.kfpckor.mongodb.net:27017,ac-nfe7wrl-shard-00-01.kfpckor.mongodb.net:27017,ac-nfe7wrl-shard-00-02.kfpckor.mongodb.net:27017/?ssl=true&authSource=admin&retryWrites=true&w=majority";
    const client = new MongoClient(uri);

    try {
        await client.connect();
        const db = client.db('EmployeeCleanDB');
        console.log("Connected to MongoDB Atlas - EmployeeCleanDB");

        const attendanceCollection = db.collection('attendance_buckets');
        const buckets = await attendanceCollection.find({}).toArray();
        let bucketUpdates = 0;
        let logUpdates = 0;

        for (const bucket of buckets) {
            let modified = false;

            for (let i = 0; i < bucket.DailyLogs.length; i++) {
                const log = bucket.DailyLogs[i];
                if (log.Status === "Present" && log.ShiftCode === "S02" && log.WorkingHours === 7.5) {
                    bucket.DailyLogs[i].WorkingHours = 8;
                    modified = true;
                    logUpdates++;
                }
            }

            if (modified) {
                await attendanceCollection.updateOne(
                    { _id: bucket._id },
                    { $set: { DailyLogs: bucket.DailyLogs } }
                );
                bucketUpdates++;
            }
        }

        console.log(`Updated ${bucketUpdates} attendance buckets, comprising ${logUpdates} total daily logs adjusted to GMT+7 local timezone alignment.`);

    } catch (e) {
        console.error("Error connecting to MongoDB:", e);
    } finally {
        await client.close();
    }
}

main().catch(console.error);
