"""
Seed attendance_buckets for 02-2026 for all 128 employees.
Shift S01: 08:00-17:00 ICT (01:00-10:00 UTC), Break 12:00-13:00 ICT (05:00-06:00 UTC), 8h
Shift S02: 06:00-14:00 ICT (23:00-07:00 UTC), Break 10:00-10:30 ICT (03:00-03:30 UTC), 7.5h
"""

import random
from datetime import datetime, timedelta, timezone
from pymongo import MongoClient

CONN_STR = "mongodb+srv://nguyenvanminh180220_db_user:Pass-180220@cluster0.kfpckor.mongodb.net/"
DB_NAME = "EmployeeCleanDB"
COLLECTION = "attendance_buckets"

ALL_EMPLOYEE_IDS = [
    "69a06a793f2a5758a4364a9f","69a06a7a3f2a5758a4364aa0","69a06a7b3f2a5758a4364aa1",
    "69a06a7c3f2a5758a4364aa2","69a06a7d3f2a5758a4364aa3","69a06a7e3f2a5758a4364aa4",
    "69a06a7f3f2a5758a4364aa5","69a06a803f2a5758a4364aa6","69a06a813f2a5758a4364aa7",
    "69a06a823f2a5758a4364aa8","69a06a833f2a5758a4364aa9","69a06a843f2a5758a4364aaa",
    "69a06a863f2a5758a4364aab","69a06a873f2a5758a4364aac","69a06a883f2a5758a4364aad",
    "69a06a893f2a5758a4364aae","69a06a8a3f2a5758a4364aaf","69a06a8a3f2a5758a4364ab0",
    "69a06a8c3f2a5758a4364ab1","69a06a8d3f2a5758a4364ab2","69a06a8e3f2a5758a4364ab3",
    "69a06a8f3f2a5758a4364ab4","69a06a903f2a5758a4364ab5","69a06a923f2a5758a4364ab6",
    "69a06a933f2a5758a4364ab7","69a06a943f2a5758a4364ab8","69a06a953f2a5758a4364ab9",
    "69a06a963f2a5758a4364aba","69a06a973f2a5758a4364abb","69a06a983f2a5758a4364abc",
    "69a06a9a3f2a5758a4364abd","69a06a9b3f2a5758a4364abe","69a06a9d3f2a5758a4364abf",
    "69a06a9e3f2a5758a4364ac0","69a06a9f3f2a5758a4364ac1","69a06aa03f2a5758a4364ac2",
    "69a06aa23f2a5758a4364ac3","69a06aa33f2a5758a4364ac4","69a06aa53f2a5758a4364ac5",
    "69a06aa63f2a5758a4364ac6","69a06aa83f2a5758a4364ac7","69a06aa93f2a5758a4364ac8",
    "69a06aaa3f2a5758a4364ac9","69a06aac3f2a5758a4364aca","69a06aae3f2a5758a4364acb",
    "69a06ab03f2a5758a4364acc","69a06ab13f2a5758a4364acd","69a06ab23f2a5758a4364ace",
    "69a06ab33f2a5758a4364acf","69a06ab53f2a5758a4364ad0","69a06ab63f2a5758a4364ad1",
    "69a06ab73f2a5758a4364ad2","69a06ab83f2a5758a4364ad3","69a06ab93f2a5758a4364ad4",
    "69a06aba3f2a5758a4364ad5","69a06abc3f2a5758a4364ad6","69a06abd3f2a5758a4364ad7",
    "69a06abe3f2a5758a4364ad8","69a06abf3f2a5758a4364ad9","69a06abf3f2a5758a4364ada",
    "69a06ac03f2a5758a4364adb","69a06ac23f2a5758a4364adc","69a06ac33f2a5758a4364add",
    "69a06ac43f2a5758a4364ade","69a06ac53f2a5758a4364adf","69a06ac63f2a5758a4364ae0",
    "69a06ac73f2a5758a4364ae1","69a06ac83f2a5758a4364ae2","69a06ac93f2a5758a4364ae3",
    "69a06aca3f2a5758a4364ae4","69a06acb3f2a5758a4364ae5","69a06acc3f2a5758a4364ae6",
    "69a06acd3f2a5758a4364ae7","69a06ace3f2a5758a4364ae8","69a06acf3f2a5758a4364ae9",
    "69a06ad03f2a5758a4364aea","69a06ad13f2a5758a4364aeb","69a06ad23f2a5758a4364aec",
    "69a06ad33f2a5758a4364aed","69a06ad43f2a5758a4364aee","69a06ad53f2a5758a4364aef",
    "69a06ad63f2a5758a4364af0","69a06ad73f2a5758a4364af1","69a06ad83f2a5758a4364af2",
    "69a06ad93f2a5758a4364af3","69a06ada3f2a5758a4364af4","69a06adb3f2a5758a4364af5",
    "69a06adc3f2a5758a4364af6","69a06add3f2a5758a4364af7","69a06ade3f2a5758a4364af8",
    "69a06adf3f2a5758a4364af9","69a06ae03f2a5758a4364afa","69a06ae13f2a5758a4364afb",
    "69a06ae23f2a5758a4364afc","69a06ae33f2a5758a4364afd","69a06ae43f2a5758a4364afe",
    "69a06ae53f2a5758a4364aff","69a06ae63f2a5758a4364b00","69a06ae73f2a5758a4364b01",
    "69a06ae83f2a5758a4364b02","69a06ae93f2a5758a4364b03","69a06aea3f2a5758a4364b04",
    "69a06aeb3f2a5758a4364b05","69a06aec3f2a5758a4364b06","69a06aed3f2a5758a4364b07",
    "69a06aee3f2a5758a4364b08","69a06aef3f2a5758a4364b09","69a06af03f2a5758a4364b0a",
    "69a06af13f2a5758a4364b0b","69a06af23f2a5758a4364b0c","69a06af33f2a5758a4364b0d",
    "69a06af43f2a5758a4364b0e","69a06af53f2a5758a4364b0f","69a06af73f2a5758a4364b10",
    "69a06af83f2a5758a4364b11","69a06af93f2a5758a4364b12","69a06afa3f2a5758a4364b13",
    "69a06afb3f2a5758a4364b14","69a06afc3f2a5758a4364b15","69a06afd3f2a5758a4364b16",
    "69a06afe3f2a5758a4364b17","69a06aff3f2a5758a4364b18","69a06b003f2a5758a4364b19",
    "69a06b013f2a5758a4364b1a","69a06b023f2a5758a4364b1b","69a06b033f2a5758a4364b1c",
    "69a06b043f2a5758a4364b1d","69a2b228ad10da558270f938",
]

assert len(ALL_EMPLOYEE_IDS) == 128, f"Expected 128 employees, got {len(ALL_EMPLOYEE_IDS)}"

# Feb 2026: weekends are Feb 1(Sun),7(Sat),8(Sun),14(Sat),15(Sun),21(Sat),22(Sun),28(Sat)
WEEKEND_DAYS = {1, 7, 8, 14, 15, 21, 22, 28}

# Shifts: S01 = first 90 employees, S02 = next 38
# (80/20 split approximately)
def get_shift_for_index(i):
    return "S01" if (i % 5) != 0 else "S02"  # ~80% S01, ~20% S02

# Shift configs (ICT = UTC+7)
SHIFTS = {
    "S01": {
        "start_utc_h": 1, "start_utc_m": 0,   # 08:00 ICT
        "end_utc_h": 10, "end_utc_m": 0,        # 17:00 ICT
        "break_start_utc_h": 5, "break_start_utc_m": 0,  # 12:00 ICT
        "break_end_utc_h": 6, "break_end_utc_m": 0,       # 13:00 ICT
        "std_hours": 8.0,
        "grace_min": 15,
    },
    "S02": {
        # 06:00 ICT = 23:00 UTC previous night, but for same-date storage we store relative to midnight
        # Let's treat S02 check-in as previous day evening, but for simplicity
        # store all on the calendar date. 06:00 ICT = 23:00 UTC day-1
        # We'll add the prev_day offset for S02 check-in
        "start_utc_h": 23, "start_utc_m": 0,   # 06:00 ICT = prev day 23:00 UTC
        "end_utc_h": 7, "end_utc_m": 0,          # 14:00 ICT
        "break_start_utc_h": 3, "break_start_utc_m": 0,   # 10:00 ICT
        "break_end_utc_h": 3, "break_end_utc_m": 30,       # 10:30 ICT
        "std_hours": 7.5,
        "grace_min": 15,
        "prev_day_checkin": True,
    },
}

def compute_working_hours(check_in: datetime, check_out: datetime, shift: dict) -> float:
    """Compute working hours, deducting break time."""
    total_minutes = (check_out - check_in).total_seconds() / 60
    # Determine break duration that falls within check_in - check_out
    # Break window: [break_start_utc, break_end_utc] on the check_out date
    date_base = check_out.replace(hour=0, minute=0, second=0, microsecond=0)
    break_start = date_base.replace(hour=shift["break_start_utc_h"], minute=shift["break_start_utc_m"])
    break_end = date_base.replace(hour=shift["break_end_utc_h"], minute=shift["break_end_utc_m"])
    # overlap
    overlap_start = max(check_in, break_start)
    overlap_end = min(check_out, break_end)
    break_minutes = max(0, (overlap_end - overlap_start).total_seconds() / 60)
    working_minutes = total_minutes - break_minutes
    return round(working_minutes / 60, 4)


def generate_daily_log(emp_idx: int, day: int, shift_code: str, rng: random.Random) -> dict:
    """Generate a DailyLog entry for a given day (1-28) of Feb 2026."""
    is_weekend = day in WEEKEND_DAYS
    date_utc = datetime(2026, 2, day, 0, 0, 0, tzinfo=timezone.utc)
    shift = SHIFTS[shift_code]

    if is_weekend:
        return {
            "Date": date_utc,
            "CheckIn": None,
            "CheckOut": None,
            "ShiftCode": shift_code,
            "WorkingHours": 0.0,
            "LateMinutes": 0,
            "EarlyLeaveMinutes": 0,
            "OvertimeHours": 0.0,
            "Status": "Weekend",
            "Note": "",
            "IsHoliday": False,
            "IsWeekend": True,
        }

    # Working day
    is_absent = rng.random() < 0.08  # 8% absence rate
    if is_absent:
        return {
            "Date": date_utc,
            "CheckIn": None,
            "CheckOut": None,
            "ShiftCode": shift_code,
            "WorkingHours": 0.0,
            "LateMinutes": 0,
            "EarlyLeaveMinutes": 0,
            "OvertimeHours": 0.0,
            "Status": "Absent",
            "Note": "",
            "IsHoliday": False,
            "IsWeekend": False,
        }

    is_late = rng.random() < 0.15   # 15% late
    leaves_early = rng.random() < 0.10  # 10% early leave
    works_overtime = rng.random() < 0.08  # 8% overtime (mutually exclusive with early leave)
    if leaves_early:
        works_overtime = False

    # Determine shift start base datetime
    if shift_code == "S02":
        # Start at 23:00 UTC on previous day
        shift_start = datetime(2026, 2, day - 1 if day > 1 else 1, shift["start_utc_h"],
                               shift["start_utc_m"], 0, tzinfo=timezone.utc)
        if day == 1:
            shift_start = datetime(2026, 1, 31, shift["start_utc_h"], shift["start_utc_m"], 0,
                                   tzinfo=timezone.utc)
        shift_end = datetime(2026, 2, day, shift["end_utc_h"], shift["end_utc_m"], 0, tzinfo=timezone.utc)
    else:  # S01
        shift_start = datetime(2026, 2, day, shift["start_utc_h"], shift["start_utc_m"], 0, tzinfo=timezone.utc)
        shift_end = datetime(2026, 2, day, shift["end_utc_h"], shift["end_utc_m"], 0, tzinfo=timezone.utc)

    # Generate check-in time
    grace = shift["grace_min"]
    if is_late:
        late_minutes = rng.randint(grace + 1, 90)
        check_in = shift_start + timedelta(minutes=late_minutes)
        check_in_sec = rng.randint(0, 59)
        check_in = check_in.replace(second=check_in_sec)
    else:
        # On time: -10 to +grace minutes from start
        on_time_offset = rng.randint(-10, grace - 1)
        check_in = shift_start + timedelta(minutes=on_time_offset)
        check_in_sec = rng.randint(0, 59)
        check_in = check_in.replace(second=check_in_sec)

    # Generate check-out time
    if leaves_early:
        early_leave_minutes = rng.randint(30, 120)
        check_out = shift_end - timedelta(minutes=early_leave_minutes)
        check_out_sec = rng.randint(0, 59)
        check_out = check_out.replace(second=check_out_sec)
    elif works_overtime:
        overtime_minutes = rng.randint(30, 120)
        check_out = shift_end + timedelta(minutes=overtime_minutes)
        check_out_sec = rng.randint(0, 59)
        check_out = check_out.replace(second=check_out_sec)
    else:
        # Normal checkout: 0 to 30 min after shift end
        late_out = rng.randint(0, 30)
        check_out = shift_end + timedelta(minutes=late_out)
        check_out_sec = rng.randint(0, 59)
        check_out = check_out.replace(second=check_out_sec)

    # Ensure check_out > check_in
    if check_out <= check_in:
        check_out = check_in + timedelta(hours=shift["std_hours"])

    working_hours = compute_working_hours(check_in, check_out, shift)
    if working_hours < 0:
        working_hours = 0.0

    # Calculate late minutes
    late_minutes_actual = max(0, int((check_in - shift_start).total_seconds() / 60))
    # Calculate early leave minutes
    early_leave_minutes_actual = max(0, int((shift_end - check_out).total_seconds() / 60))
    # Overtime hours
    overtime_hours = max(0.0, round((check_out - shift_end).total_seconds() / 3600, 4)) if not leaves_early else 0.0

    # Status
    if is_late and leaves_early:
        status = "Late"
    elif is_late:
        status = "Late"
    elif leaves_early:
        status = "EarlyLeave"
    else:
        status = "Present"

    return {
        "Date": date_utc,
        "CheckIn": check_in,
        "CheckOut": check_out,
        "ShiftCode": shift_code,
        "WorkingHours": working_hours,
        "LateMinutes": late_minutes_actual,
        "EarlyLeaveMinutes": early_leave_minutes_actual,
        "OvertimeHours": overtime_hours,
        "Status": status,
        "Note": "",
        "IsHoliday": False,
        "IsWeekend": False,
    }


def generate_bucket(emp_id: str, emp_idx: int) -> dict:
    shift_code = get_shift_for_index(emp_idx)
    rng = random.Random(emp_idx * 100 + 202602)

    daily_logs = []
    for day in range(1, 29):  # Feb 1-28
        log = generate_daily_log(emp_idx, day, shift_code, rng)
        daily_logs.append(log)

    total_present = sum(
        1 for l in daily_logs
        if l["Status"] in ("Present", "Late", "EarlyLeave") and not l["IsWeekend"]
    )
    total_late = sum(1 for l in daily_logs if l["Status"] == "Late")
    total_overtime = sum(1 for l in daily_logs if l["OvertimeHours"] > 0)

    now_utc = datetime(2026, 3, 1, 0, 0, 0, tzinfo=timezone.utc)
    epoch_min = datetime(1, 1, 1, tzinfo=timezone.utc)  # matches the sample's -62135596800000 ms epoch

    return {
        "IsDeleted": False,
        "CreatedAt": epoch_min,
        "CreatedBy": "System",
        "UpdatedAt": now_utc,
        "UpdatedBy": None,
        "Version": 1,
        "EmployeeId": emp_id,
        "Month": "02-2026",
        "DailyLogs": daily_logs,
        "TotalPresent": total_present,
        "TotalLate": total_late,
        "TotalOvertime": total_overtime,
    }


def main():
    client = MongoClient(CONN_STR)
    db = client[DB_NAME]
    col = db[COLLECTION]

    # Check if any 02-2026 docs already exist
    existing = col.count_documents({"Month": "02-2026"})
    if existing > 0:
        print(f"WARNING: {existing} documents for 02-2026 already exist. Skipping insert.")
        return

    documents = []
    for idx, emp_id in enumerate(ALL_EMPLOYEE_IDS):
        doc = generate_bucket(emp_id, idx)
        documents.append(doc)

    print(f"Generated {len(documents)} attendance bucket documents for 02-2026")

    # Insert in batches of 32 to avoid payload limits
    batch_size = 32
    total_inserted = 0
    for i in range(0, len(documents), batch_size):
        batch = documents[i:i + batch_size]
        result = col.insert_many(batch)
        total_inserted += len(result.inserted_ids)
        print(f"Batch {i//batch_size + 1}: inserted {len(result.inserted_ids)} documents")

    print(f"\nTotal inserted: {total_inserted} attendance buckets for 02-2026")
    client.close()


if __name__ == "__main__":
    main()
