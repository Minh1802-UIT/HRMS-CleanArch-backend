import json
from pymongo import MongoClient

MONGO_URI = "mongodb+srv://nguyenvanminh180220_db_user:Pass-180220@cluster0.kfpckor.mongodb.net/"
DB_NAME   = "EmployeeCleanDB"

client = MongoClient(MONGO_URI)
db = client[DB_NAME]

output = {"overtime": [], "leave": [], "buckets": [], "ceo": None}

for os in db["overtime_schedules"].find():
    if os.get("Reason") == "Urgent release":
        os["_id"] = str(os["_id"])
        os["Date"] = str(os["Date"])
        os["CreatedAt"] = str(os["CreatedAt"])
        output["overtime"].append(os)

for lr in db["leave_requests"].find({"Reason": "Personal matters"}):
    lr["_id"] = str(lr["_id"])
    lr["FromDate"] = str(lr["FromDate"])
    lr["ToDate"] = str(lr["ToDate"])
    lr["CreatedAt"] = str(lr["CreatedAt"])
    output["leave"].append(lr)

buckets = list(db["attendance_buckets"].find({"CreatedBy": "PayrollTestSeeder"}))
for b in buckets:
    b["_id"] = str(b["_id"])
    b["CreatedAt"] = str(b["CreatedAt"])
    b.pop("DailyLogs", None)
    output["buckets"].append(b)

ceo = db["employees"].find_one({"EmployeeCode": "CEO001", "IsDeleted": False})
if ceo:
    b = db["attendance_buckets"].find_one({"EmployeeId": str(ceo["_id"]), "Month": "03-2026"})
    if b:
        output["ceo"] = { "id": str(b["_id"]), "empId": b["EmployeeId"] }

with open("verify_out.json", "w") as f:
    json.dump(output, f, indent=2)

print("Done")
