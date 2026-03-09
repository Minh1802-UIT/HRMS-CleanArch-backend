from pymongo import MongoClient
from datetime import datetime, timezone, timedelta
from bson import ObjectId
import calendar

# Config
MONGO_URI = "mongodb+srv://nguyenvanminh180220_db_user:Pass-180220@cluster0.kfpckor.mongodb.net/"
DB_NAME   = "EmployeeCleanDB"

client = MongoClient(MONGO_URI)
db = client[DB_NAME]
now = datetime.now(timezone.utc)

def get_employee_by_code(code):
    return db["employees"].find_one({"EmployeeCode": code, "IsDeleted": False})

def generate_bucket_for_employee(employee_id, month_str="03-2026", start_date_str="2026-02-26", end_date_str="2026-03-25"):
    start_date = datetime.strptime(start_date_str, "%Y-%m-%d").replace(tzinfo=timezone.utc)
    end_date = datetime.strptime(end_date_str, "%Y-%m-%d").replace(tzinfo=timezone.utc)
    
    daily_logs = []
    current_date = start_date
    while current_date <= end_date:
        is_weekend = current_date.weekday() in [5, 6] # Sat, Sun
        
        log = {
            "Date": current_date,
            "ShiftCode": "S01",
            "WorkingHours": 0 if is_weekend else 8,
            "LateMinutes": 0,
            "EarlyLeaveMinutes": 0,
            "OvertimeHours": 0,
            "Status": "Absent" if is_weekend else "Present",
            "IsLate": False,
            "IsEarlyLeave": False,
            "IsMissingPunch": False,
            "IsMissingCheckIn": False,
            "Note": "Test data",
            "IsHoliday": False,
            "IsWeekend": is_weekend
        }
        
        if not is_weekend:
            log["CheckIn"] = current_date + timedelta(hours=8)
            log["CheckOut"] = current_date + timedelta(hours=17)
        else:
            log["CheckIn"] = None
            log["CheckOut"] = None
            
        daily_logs.append(log)
        current_date += timedelta(days=1)
        
    return {
        "EmployeeId": str(employee_id),
        "Month": month_str,
        "DailyLogs": daily_logs,
        "TotalPresent": sum(1 for log in daily_logs if log["Status"] == "Present"),
        "TotalLate": 0,
        "TotalOvertime": 0,
        "CreatedAt": now,
        "CreatedBy": "PayrollTestSeeder",
        "Version": 1,
        "IsDeleted": False
    }

def update_bucket_in_db(bucket):
    db["attendance_buckets"].update_one(
        {"EmployeeId": bucket["EmployeeId"], "Month": bucket["Month"]},
        {"$set": bucket},
        upsert=True
    )

def seed_payroll_test_data():
    month_str = "03-2026"
    print(f"Seeding test data for {month_str}...")

    # 1. CTO001: Overtime on Mar 5 (2h), Late on Mar 6 (1h)
    cto = get_employee_by_code("CTO001")
    if cto:
        bucket = generate_bucket_for_employee(cto["_id"])
        logs = bucket["DailyLogs"]
        for log in logs:
            date_str = log["Date"].strftime("%Y-%m-%d")
            if date_str == "2026-03-05":
                log["CheckOut"] = log["CheckOut"] + timedelta(hours=2)
                log["OvertimeHours"] = 2
                log["WorkingHours"] = 10
                log["Note"] = "Overtime 2h"
                db["overtime_schedules"].insert_one({
                    "_id": ObjectId(), "EmployeeId": str(cto["_id"]), "Date": log["Date"], "Hours": 2,
                    "Status": "Approved", "Reason": "Urgent release", "CreatedAt": now, "CreatedBy": "System", "Version": 1, "IsDeleted": False
                })
            elif date_str == "2026-03-06":
                log["CheckIn"] = log["CheckIn"] + timedelta(hours=1)
                log["LateMinutes"] = 60
                log["IsLate"] = True
                log["WorkingHours"] = 7
                log["Note"] = "Late check-in"
        bucket["TotalLate"] = 1
        bucket["TotalOvertime"] = 2
        update_bucket_in_db(bucket)
        print("✅ CTO001: Added Overtime (Mar 5) and Late (Mar 6)")

    # 2. MGR001: 2 days Unpaid Leave (Mar 10, 11)
    mgr = get_employee_by_code("MGR001")
    if mgr:
        bucket = generate_bucket_for_employee(mgr["_id"])
        logs = bucket["DailyLogs"]
        for log in logs:
            date_str = log["Date"].strftime("%Y-%m-%d")
            if date_str in ["2026-03-10", "2026-03-11"]:
                log["CheckIn"] = None; log["CheckOut"] = None
                log["WorkingHours"] = 0; log["Status"] = "Absent"; log["Note"] = "Unpaid Leave"
                db["leave_requests"].insert_one({
                    "_id": ObjectId(), "EmployeeId": str(mgr["_id"]), "LeaveType": "Unpaid",
                    "FromDate": log["Date"], "ToDate": log["Date"], "Reason": "Personal matters",
                    "Status": "Approved", "ManagerComment": "Approved", "ApprovedBy": "System", "CreatedAt": now, "CreatedBy": "System", "Version": 1, "IsDeleted": False
                })
        bucket["TotalPresent"] = sum(1 for log in logs if log["WorkingHours"] > 0)
        update_bucket_in_db(bucket)
        print("✅ MGR001: Added 2 days Unpaid Leave (Mar 10, 11)")

    # 3. LEAD01: 1 day Paid Leave (Mar 12)
    lead = get_employee_by_code("LEAD01")
    if lead:
        bucket = generate_bucket_for_employee(lead["_id"])
        logs = bucket["DailyLogs"]
        for log in logs:
            date_str = log["Date"].strftime("%Y-%m-%d")
            if date_str == "2026-03-12":
                log["CheckIn"] = None; log["CheckOut"] = None
                log["WorkingHours"] = 0; log["Status"] = "Absent"; log["Note"] = "Annual Leave"
                db["leave_requests"].insert_one({
                    "_id": ObjectId(), "EmployeeId": str(lead["_id"]), "LeaveType": "Annual",
                    "FromDate": log["Date"], "ToDate": log["Date"], "Reason": "Vacation",
                    "Status": "Approved", "ManagerComment": "Enjoy!", "ApprovedBy": "System", "CreatedAt": now, "CreatedBy": "System", "Version": 1, "IsDeleted": False
                })
        bucket["TotalPresent"] = sum(1 for log in logs if log["WorkingHours"] > 0)
        update_bucket_in_db(bucket)
        print("✅ LEAD01: Added 1 day Paid Leave (Mar 12)")

    # 4. JUN101: 1 day Absent without permission (Mar 13)
    jun = get_employee_by_code("JUN101")
    if jun:
        bucket = generate_bucket_for_employee(jun["_id"])
        logs = bucket["DailyLogs"]
        for log in logs:
            date_str = log["Date"].strftime("%Y-%m-%d")
            if date_str == "2026-03-13":
                log["CheckIn"] = None; log["CheckOut"] = None
                log["WorkingHours"] = 0; log["Status"] = "Absent"; log["Note"] = "Missing Check-in, no leave"; log["IsMissingPunch"] = True
        bucket["TotalPresent"] = sum(1 for log in logs if log["WorkingHours"] > 0)
        update_bucket_in_db(bucket)
        print("✅ JUN101: Added 1 day Absent No Permission (Mar 13)")

    # 5. SEN102: Early Leave (2 hours) on Mar 16
    sen = get_employee_by_code("SEN102")
    if sen:
        bucket = generate_bucket_for_employee(sen["_id"])
        logs = bucket["DailyLogs"]
        for log in logs:
            date_str = log["Date"].strftime("%Y-%m-%d")
            if date_str == "2026-03-16":
                log["CheckOut"] = log["CheckOut"] - timedelta(hours=2)
                log["EarlyLeaveMinutes"] = 120
                log["IsEarlyLeave"] = True; log["WorkingHours"] = 6; log["Note"] = "Early leave"
        update_bucket_in_db(bucket)
        print("✅ SEN102: Added early leave by 2 hours (Mar 16)")

    # CEO001: Perfect
    ceo = get_employee_by_code("CEO001")
    if ceo:
        bucket = generate_bucket_for_employee(ceo["_id"])
        update_bucket_in_db(bucket)
        print("✅ CEO001: Added Perfect Attendance")

if __name__ == "__main__":
    seed_payroll_test_data()
    print("\nPayroll test data seeded successfully!")
    client.close()
