const { MongoClient, ObjectId } = require('C:\\temp\\mongo_check\\node_modules\\mongodb');

async function main() {
    const uri = "mongodb://nguyenvanminh180220_db_user:Pass-180220@ac-nfe7wrl-shard-00-00.kfpckor.mongodb.net:27017,ac-nfe7wrl-shard-00-01.kfpckor.mongodb.net:27017,ac-nfe7wrl-shard-00-02.kfpckor.mongodb.net:27017/?ssl=true&authSource=admin&retryWrites=true&w=majority";
    const client = new MongoClient(uri);

    try {
        await client.connect();
        const db = client.db('EmployeeCleanDB');
        console.log("Connected to MongoDB Atlas - EmployeeCleanDB");

        // 1. Process Payrolls - Fill out Snapshot accurately based on actual foreign records
        const payrollCollection = db.collection('payrolls');
        const employeeCollection = db.collection('employees');
        const departmentCollection = db.collection('departments');
        const positionCollection = db.collection('positions');

        const payrolls = await payrollCollection.find({}).toArray();
        let payrollUpdates = 0;

        for (const pr of payrolls) {
            if (pr.EmployeeId) {
                const emp = await employeeCollection.findOne({ _id: new ObjectId(pr.EmployeeId) }); // ObjectId? The IDs in Seeder are sometimes string sometimes ObjectId. Wait, Entity Framework uses strings for IDs in this implementation? Let's check first. Let's assume they are strings first based on the output of check_mongo.js: "_id": "69a06b1c3f2a5758a4364d43"
                // Actually, let's just find by string ID
                let empData = await employeeCollection.findOne({ _id: pr.EmployeeId });
                // If not string, maybe ObjectId
                if (!empData) {
                    try { empData = await employeeCollection.findOne({ _id: new ObjectId(pr.EmployeeId) }); } catch (e) { }
                }

                if (empData) {
                    let deptName = "";
                    let posName = "";

                    if (empData.JobDetails && empData.JobDetails.DepartmentId) {
                        let dept = await departmentCollection.findOne({ _id: empData.JobDetails.DepartmentId });
                        if (!dept) {
                            try { dept = await departmentCollection.findOne({ _id: new ObjectId(empData.JobDetails.DepartmentId) }); } catch (e) { }
                        }
                        if (dept) deptName = dept.Name;
                    }

                    if (empData.JobDetails && empData.JobDetails.PositionId) {
                        let pos = await positionCollection.findOne({ _id: empData.JobDetails.PositionId });
                        if (!pos) {
                            try { pos = await positionCollection.findOne({ _id: new ObjectId(empData.JobDetails.PositionId) }); } catch (e) { }
                        }
                        if (pos) posName = pos.Name;
                    }

                    await payrollCollection.updateOne(
                        { _id: pr._id },
                        {
                            $set: {
                                "Snapshot.EmployeeName": empData.FullName || "",
                                "Snapshot.EmployeeCode": empData.EmployeeCode || "",
                                "Snapshot.DepartmentName": deptName,
                                "Snapshot.PositionTitle": posName
                            }
                        }
                    );
                    payrollUpdates++;
                }
            }
        }
        console.log(`Updated ${payrollUpdates} payroll snapshot records.`);

        // 2. Process job_vacancies
        const jobResult = await db.collection('job_vacancies').updateMany(
            { Title: "Senior dev" },
            {
                $set: {
                    Title: "Tuyển dụng Lập Trình Viên Đạt Chuẩn (Senior .NET/Clean Architecture)",
                    Description: "Mô tả công việc: Tham gia phát triển hệ thống lõi HRMS sử dụng C# .NET 8, Clean Architecture. Yêu cầu ít nhất 3 năm kinh nghiệm thực chiến."
                }
            }
        );
        console.log(`Updated ${jobResult.modifiedCount} job_vacancies.`);

        // 3. Process candidates
        const candidateResult = await db.collection('candidates').updateMany(
            { FullName: "Candidate A" },
            {
                $set: {
                    FullName: "Trần Đại Trí",
                    Email: "trandaitri.dev@gmail.com",
                    Phone: "0901235555",
                    ResumeUrl: "https://hrms-assets.com/resumes/trandaitri_cv.pdf"
                }
            }
        );
        console.log(`Updated ${candidateResult.modifiedCount} candidates.`);

        // 4. Process leave requests
        const leaveCollection = db.collection('leave_requests');
        const leaves = await leaveCollection.find({}).toArray();
        let leaveUpdates = 0;
        const reasons = [
            "Nghỉ phép thường niên cùng gia đình",
            "Nghỉ ốm theo chỉ định bác sĩ",
            "Giải quyết việc gia đình",
            "Du lịch cá nhân"
        ];

        for (const leave of leaves) {
            const randomReason = reasons[Math.floor(Math.random() * reasons.length)];
            const newManagerComment = leave.ManagerComment === "Auto-approved" ? "Đã duyệt/Đồng ý" : (leave.ManagerComment || "Chờ xử lý");

            await leaveCollection.updateOne(
                { _id: leave._id },
                {
                    $set: {
                        Reason: randomReason,
                        ManagerComment: newManagerComment
                    }
                }
            );
            leaveUpdates++;
        }
        console.log(`Updated ${leaveUpdates} leave_requests.`);

        // 5. Process overtime_schedules
        const otResult = await db.collection('overtime_schedules').updateMany(
            { Reason: "Urgent release" },
            {
                $set: {
                    Reason: "Làm thêm giờ để deploy dự án mới đúng tiến độ theo yêu cầu"
                }
            }
        );
        console.log(`Updated ${otResult.modifiedCount} overtime_schedules.`);

    } catch (e) {
        console.error("Error connecting to MongoDB:", e);
    } finally {
        await client.close();
    }
}

main().catch(console.error);
