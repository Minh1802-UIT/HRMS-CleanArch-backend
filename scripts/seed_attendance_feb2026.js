// Seed attendance buckets for February 2026 - 128 employees
// Working days in Feb 2026: Feb 2-6, 9-13, 16-20, 23-27 = 20 weekdays
// Run: mongosh "mongodb://localhost:27017/EmployeeCleanDB" --file seed_attendance_feb2026.js

const db = db.getSiblingDB("EmployeeCleanDB");

const employeeIds = [
  "69a06a793f2a5758a4364a9f","69a06a7a3f2a5758a4364aa0","69a06acb3f2a5758a4364ae5",
  "69a06ad63f2a5758a4364af0","69a06ade3f2a5758a4364af8","69a06afb3f2a5758a4364b14",
  "69a06b003f2a5758a4364b19","69a06b043f2a5758a4364b1d","69a06add3f2a5758a4364af7",
  "69a06b033f2a5758a4364b1c","69a06acf3f2a5758a4364ae9","69a06ae03f2a5758a4364afa",
  "69a06af13f2a5758a4364b0b","69a06afa3f2a5758a4364b13","69a06abf3f2a5758a4364ada",
  "69a06ac73f2a5758a4364ae1","69a06ad23f2a5758a4364aec","69a06ada3f2a5758a4364af4",
  "69a06acd3f2a5758a4364ae7","69a06ad33f2a5758a4364aed","69a06ae83f2a5758a4364b02",
  "69a06aeb3f2a5758a4364b05","69a06af43f2a5758a4364b0e","69a06b023f2a5758a4364b1b",
  "69a06ac43f2a5758a4364ade","69a06ac63f2a5758a4364ae0","69a06ad03f2a5758a4364aea",
  "69a06ad43f2a5758a4364aee","69a06adf3f2a5758a4364af9","69a06af73f2a5758a4364b10",
  "69a06af93f2a5758a4364b12","69a06afc3f2a5758a4364b15","69a06a7b3f2a5758a4364aa1",
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
  "69a06ab73f2a5758a4364ad2","69a2b228ad10da558270f938","69a06abc3f2a5758a4364ad6",
  "69a06adc3f2a5758a4364af6","69a06ae43f2a5758a4364afe","69a06aea3f2a5758a4364b04",
  "69a06aef3f2a5758a4364b09","69a06aba3f2a5758a4364ad5","69a06ac53f2a5758a4364adf",
  "69a06ac83f2a5758a4364ae2","69a06ace3f2a5758a4364ae8","69a06ad13f2a5758a4364aeb",
  "69a06ae23f2a5758a4364afc","69a06ae33f2a5758a4364afd","69a06afd3f2a5758a4364b16",
  "69a06afe3f2a5758a4364b17","69a06ad83f2a5758a4364af2","69a06ae63f2a5758a4364b00",
  "69a06ae93f2a5758a4364b03","69a06af53f2a5758a4364b0f","69a06aff3f2a5758a4364b18",
  "69a06b013f2a5758a4364b1a","69a06af03f2a5758a4364b0a","69a06af23f2a5758a4364b0c",
  "69a06af83f2a5758a4364b11","69a06ad53f2a5758a4364aef","69a06aed3f2a5758a4364b07",
  "69a06ab93f2a5758a4364ad4","69a06acc3f2a5758a4364ae6","69a06ad93f2a5758a4364af3",
  "69a06adb3f2a5758a4364af5","69a06ac03f2a5758a4364adb","69a06ac23f2a5758a4364adc",
  "69a06ac33f2a5758a4364add","69a06ac93f2a5758a4364ae3","69a06ae73f2a5758a4364b01",
  "69a06aec3f2a5758a4364b06","69a06af33f2a5758a4364b0d","69a06ab83f2a5758a4364ad3",
  "69a06abe3f2a5758a4364ad8","69a06aca3f2a5758a4364ae4","69a06ad73f2a5758a4364af1",
  "69a06ae13f2a5758a4364afb","69a06abd3f2a5758a4364ad7","69a06abf3f2a5758a4364ad9",
  "69a06ae53f2a5758a4364aff","69a06aee3f2a5758a4364b08"
];

// Feb 2026 working days (Mon-Fri): Feb 2-6, 9-13, 16-20, 23-27
const workingDays = [2,3,4,5,6, 9,10,11,12,13, 16,17,18,19,20, 23,24,25,26,27];

function makeCheckIn(dayOfMonth, lateMinutes) {
  // 8:00 ICT = 01:00 UTC, add lateMinutes
  const base = new Date(`2026-02-${String(dayOfMonth).padStart(2,'0')}T01:00:00.000Z`);
  base.setMinutes(base.getMinutes() + lateMinutes);
  return base;
}

function makeCheckOut(dayOfMonth, earlyLeaveMinutes, overtimeHours) {
  // 17:00 ICT = 10:00 UTC, minus earlyLeave, plus overtime
  const base = new Date(`2026-02-${String(dayOfMonth).padStart(2,'0')}T10:00:00.000Z`);
  base.setMinutes(base.getMinutes() - earlyLeaveMinutes + overtimeHours * 60);
  return base;
}

// Delete old Feb 2026 attendance buckets (if re-running)
db.attendance_buckets.deleteMany({ Month: "02-2026" });
print("Deleted old 02-2026 attendance buckets");

const docs = [];

employeeIds.forEach((empId, idx) => {
  // Vary presence: 80% employees have 20 days, 15% have 18-19, 5% have 14-17
  const seed = idx;
  let presentDays = workingDays.slice(); // copy all 20 days

  // Some employees miss 1-2 days (simulate sick leave / annual leave)
  if (seed % 7 === 0) presentDays = presentDays.filter((_, i) => i !== 2 && i !== 15); // 18 days
  else if (seed % 11 === 0) presentDays = presentDays.filter((_, i) => i !== 5);       // 19 days
  else if (seed % 17 === 0) presentDays = presentDays.slice(0, 17);                     // 17 days

  let totalLate = 0;
  let totalOvertime = 0;

  const dailyLogs = presentDays.map((day, i) => {
    const late     = (i === 1 && seed % 3 === 0) ? 15 : 0;   // some late on day 2
    const early    = 0;
    const overtime = (i >= 18 && seed % 4 === 0) ? 1 : 0;     // some overtime last days
    const wh       = 8 + overtime - late / 60;

    if (late > 0)     totalLate++;
    if (overtime > 0) totalOvertime += overtime;

    return {
      Date:              new Date(`2026-02-${String(day).padStart(2,'0')}T00:00:00.000Z`),
      CheckIn:           makeCheckIn(day, late),
      CheckOut:          makeCheckOut(day, early, overtime),
      ShiftCode:         "S01",
      WorkingHours:      parseFloat(wh.toFixed(2)),
      LateMinutes:       late,
      EarlyLeaveMinutes: early,
      OvertimeHours:     overtime,
      Status:            late > 0 ? "Late" : "Present",
      Note:              "",
      IsHoliday:         false,
      IsWeekend:         false
    };
  });

  docs.push({
    IsDeleted:   false,
    CreatedAt:   new Date("0001-01-01T00:00:00.000Z"),
    CreatedBy:   "System",
    UpdatedAt:   new Date("2026-02-28T17:00:00.000Z"),
    UpdatedBy:   null,
    Version:     1,
    EmployeeId:  empId,
    Month:       "02-2026",
    DailyLogs:   dailyLogs,
    TotalPresent:  presentDays.length,
    TotalLate:     totalLate,
    TotalOvertime: totalOvertime
  });
});

const result = db.attendance_buckets.insertMany(docs);
print(`Inserted ${result.insertedIds ? Object.keys(result.insertedIds).length : 0} attendance buckets for 02-2026`);
