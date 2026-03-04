"""Inspect Atlas cycles and public holidays raw data."""
from pymongo import MongoClient

db = MongoClient("mongodb+srv://nguyenvanminh180220_db_user:Pass-180220@cluster0.kfpckor.mongodb.net/")["EmployeeCleanDB"]

print(f"Total cycles: {db['payroll_cycles'].count_documents({})}")
for d in db["payroll_cycles"].find(
    {},
    {"MonthKey": 1, "StartDate": 1, "EndDate": 1, "StandardWorkingDays": 1,
     "PublicHolidaysExcluded": 1, "WeeklyDaysOffSnapshot": 1}
).sort("MonthKey", 1):
    sd = d.get("StartDate", "?")
    ed = d.get("EndDate", "?")
    sd_s = sd.strftime("%d/%m/%Y %H:%M:%SZ") if hasattr(sd, "strftime") else str(sd)
    ed_s = ed.strftime("%d/%m/%Y %H:%M:%SZ") if hasattr(ed, "strftime") else str(ed)
    mk = d.get("MonthKey", "?")
    wd = d.get("StandardWorkingDays", "?")
    hd = d.get("PublicHolidaysExcluded", "?")
    wo = d.get("WeeklyDaysOffSnapshot", "?")
    print(f"  {mk:10}  start={sd_s}  end={ed_s}  workDays={wd}  holidays={hd}  weeklyOff='{wo}'")

print()
print(f"Total public_holidays in Atlas: {db['public_holidays'].count_documents({})}")
ph_sample = db["public_holidays"].find_one()
if ph_sample:
    print(f"  Sample keys: {sorted(ph_sample.keys())}")
    dt = ph_sample.get("Date")
    print(f"  Sample Date: {dt} | tzinfo: {getattr(dt, 'tzinfo', 'n/a')}")
    print(f"  IsRecurringYearly: {ph_sample.get('IsRecurringYearly')}")
