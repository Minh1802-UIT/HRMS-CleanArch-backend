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
