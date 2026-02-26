using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Common.Models;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Entities.Leave;
using Employee.Domain.Entities.Payroll;
using Employee.Domain.Entities.Organization;
using Employee.Domain.Entities.ValueObjects;
using Employee.Domain.Entities.Attendance;
using Employee.Domain.Entities.Common;
using Employee.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Employee.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Employee.Infrastructure.data.Seeding
{
  public static class DbSeeder
  {
    public static async Task SeedUsersAndRolesAsync(IServiceProvider serviceProvider)
    {
      var env = serviceProvider.GetRequiredService<IHostEnvironment>();
      var config = serviceProvider.GetRequiredService<IConfiguration>();

      // 1. SERVICES
      var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
      var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
      var deptRepo = serviceProvider.GetRequiredService<IDepartmentRepository>();
      var posRepo = serviceProvider.GetRequiredService<IPositionRepository>();
      var empRepo = serviceProvider.GetRequiredService<IEmployeeRepository>();
      var contractRepo = serviceProvider.GetRequiredService<IContractRepository>();
      var leaveRepo = serviceProvider.GetRequiredService<ILeaveRequestRepository>();
      var payrollRepo = serviceProvider.GetRequiredService<IPayrollRepository>();
      var leaveTypeRepo = serviceProvider.GetRequiredService<ILeaveTypeRepository>();
      var attendanceRepo = serviceProvider.GetRequiredService<IAttendanceRepository>();
      var auditRepo = serviceProvider.GetRequiredService<IAuditLogRepository>();
      var leaveAllocRepo = serviceProvider.GetRequiredService<ILeaveAllocationRepository>();
      var shiftRepo = serviceProvider.GetRequiredService<IShiftRepository>();
      var rawAttendanceRepo = serviceProvider.GetRequiredService<IRawAttendanceLogRepository>();
      var settingRepo = serviceProvider.GetRequiredService<ISystemSettingRepository>();

      var jobRepo = serviceProvider.GetRequiredService<IJobVacancyRepository>();
      var candidateRepo = serviceProvider.GetRequiredService<ICandidateRepository>();
      var interviewRepo = serviceProvider.GetRequiredService<IInterviewRepository>();

      var defaultPassword = config["Seeding:DefaultPassword"] ?? "User@12345";

      // ── PRODUCTION: Only ensure roles & admin exist, never wipe data ──
      if (env.IsProduction())
      {
        Console.WriteLine("🔒 PRODUCTION SEEDER: Checking essential data...");

        // Always ensure roles exist
        var prodRoles = new[] { "Admin", "HR", "Manager", "Employee" };
        foreach (var role in prodRoles)
        {
          if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new ApplicationRole(role));
        }
        Console.WriteLine("✅ Roles verified.");

        // Check if data already exists — if so, skip seeding entirely
        var existingEmployees = await empRepo.GetAllAsync();
        if (existingEmployees.Any())
        {
          Console.WriteLine($"✅ Database already has {existingEmployees.Count} employees. Skipping seeder.");

          // Ensure admin account exists even if data is present
          var prodAdminEmail = "admin@hrm.com";
          var prodAdminUser = await userManager.FindByEmailAsync(prodAdminEmail);
          if (prodAdminUser == null)
          {
            prodAdminUser = new ApplicationUser
            {
              UserName = "admin",
              Email = prodAdminEmail,
              FullName = "Super Administrator",
              EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(prodAdminUser, defaultPassword);
            if (result.Succeeded)
            {
              await userManager.AddToRoleAsync(prodAdminUser, "Admin");
              await userManager.AddToRoleAsync(prodAdminUser, "HR");
              Console.WriteLine("✅ Created missing Admin User.");
            }
          }

          Console.WriteLine("🔒 PRODUCTION SEEDER FINISHED (no data modified).");
          return;
        }

        Console.WriteLine("⚠️ Database is empty in Production — running initial seed...");
        // Fall through to the full seeding logic below
      }
      else
      {
        Console.WriteLine($"🌱 STARTING DATABASE SEEDER (Development/Staging)...");

        // DEVELOPMENT ONLY: Wipe data for a fresh start
        Console.WriteLine("🧹 WIPING EXISTING DATA FOR OVERWRITE...");
        await auditRepo.ClearAllAsync();
        await attendanceRepo.ClearAllAsync();
        await payrollRepo.ClearAllAsync();
        await leaveRepo.ClearAllAsync();
        await contractRepo.ClearAllAsync();
        await empRepo.ClearAllAsync();
        await posRepo.ClearAllAsync();
        await deptRepo.ClearAllAsync();
        await leaveTypeRepo.ClearAllAsync();
        await leaveAllocRepo.ClearAllAsync();
        await shiftRepo.ClearAllAsync();
        await rawAttendanceRepo.ClearAllAsync();
        await jobRepo.ClearAllAsync();
        await candidateRepo.ClearAllAsync();
        await interviewRepo.ClearAllAsync();
        await settingRepo.ClearAllAsync();

        // Wipe Users (Except Roles)
        var allUsers = userManager.Users.ToList();
        foreach (var user in allUsers)
        {
          await userManager.DeleteAsync(user);
        }
        Console.WriteLine("✅ Database wiped successfully.");
      }

      // 2. ROLES
      string[] roles = { "Admin", "HR", "Manager", "Employee" };
      foreach (var role in roles)
      {
        if (!await roleManager.RoleExistsAsync(role))
          await roleManager.CreateAsync(new ApplicationRole(role));
      }

      var adminEmail = "admin@hrm.com";
      var adminUser = await userManager.FindByEmailAsync(adminEmail);
      if (adminUser == null)
      {
        adminUser = new ApplicationUser
        {
          UserName = "admin",
          Email = adminEmail,
          FullName = "Super Administrator",
          EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(adminUser, defaultPassword);
        if (result.Succeeded)
        {
          await userManager.AddToRoleAsync(adminUser, "Admin");
          await userManager.AddToRoleAsync(adminUser, "HR");
          Console.WriteLine("✅ Created Admin User.");
        }
      }

      // 3. DEPARTMENTS (HIERARCHICAL)
      var hq = new Department("Headquarters", "HQ");
      hq.UpdateInfo("Headquarters", "Main Office");
      await deptRepo.CreateAsync(hq);

      var it = new Department("Technology", "TECH"); it.SetParent(hq.Id);
      var hr = new Department("Human Resources", "HR"); hr.SetParent(hq.Id);
      var sales = new Department("Sales & Marketing", "SALES"); sales.SetParent(hq.Id);
      var finance = new Department("Finance", "FIN"); finance.SetParent(hq.Id);
      var prod = new Department("Product", "PROD"); prod.SetParent(hq.Id);
      var sup = new Department("Support", "SUP"); sup.SetParent(hq.Id);
      var log = new Department("Logistics", "LOG"); log.SetParent(hq.Id);

      await deptRepo.CreateAsync(it);
      await deptRepo.CreateAsync(hr);
      await deptRepo.CreateAsync(sales);
      await deptRepo.CreateAsync(finance);
      await deptRepo.CreateAsync(prod);
      await deptRepo.CreateAsync(sup);
      await deptRepo.CreateAsync(log);

      // Level 2 (Sub-departments)
      var soft = new Department("Software Development", "SOFT"); soft.SetParent(it.Id); await deptRepo.CreateAsync(soft);
      var infra = new Department("Infrastructure", "INFRA"); infra.SetParent(it.Id); await deptRepo.CreateAsync(infra);
      var qa = new Department("Quality Assurance", "QA"); qa.SetParent(it.Id); await deptRepo.CreateAsync(qa);

      var rec = new Department("Recruitment", "REC"); rec.SetParent(hr.Id); await deptRepo.CreateAsync(rec);
      var ops = new Department("Operations", "OPS"); ops.SetParent(hr.Id); await deptRepo.CreateAsync(ops);

      var b2b = new Department("B2B Sales", "B2B"); b2b.SetParent(sales.Id); await deptRepo.CreateAsync(b2b);
      var mkt = new Department("Digital Marketing", "MKT"); mkt.SetParent(sales.Id); await deptRepo.CreateAsync(mkt);

      var design = new Department("UI/UX Design", "DESIGN"); design.SetParent(prod.Id); await deptRepo.CreateAsync(design);
      var cs = new Department("Customer Service", "CS"); cs.SetParent(sup.Id); await deptRepo.CreateAsync(cs);
      var wh = new Department("Warehouse", "WH"); wh.SetParent(log.Id); await deptRepo.CreateAsync(wh);

      Console.WriteLine("✅ Created Hierarchical Departments.");
      var depts = await deptRepo.GetAllAsync();

      // 4. POSITIONS (HIERARCHICAL)
      // Helper to get Department IDs
      var hqId = depts.First(d => d.Code == "HQ").Id;
      var techId = depts.First(d => d.Code == "TECH").Id;
      var hrId = depts.First(d => d.Code == "HR").Id;
      var salesId = depts.First(d => d.Code == "SALES").Id;
      var finId = depts.First(d => d.Code == "FIN").Id;
      var softId = depts.First(d => d.Code == "SOFT").Id;
      var qaId = depts.First(d => d.Code == "QA").Id;
      var prodId = depts.First(d => d.Code == "PROD").Id;
      var designId = depts.First(d => d.Code == "DESIGN").Id;
      var supId = depts.First(d => d.Code == "SUP").Id;
      var csId = depts.First(d => d.Code == "CS").Id;
      var logId = depts.First(d => d.Code == "LOG").Id;
      var whId = depts.First(d => d.Code == "WH").Id;

      // C-Level
      var ceo = new Position("Chief Executive Officer", "CEO", hqId);
      ceo.UpdateSalaryRange(new SalaryRange { Min = 80000000, Max = 150000000 });
      await posRepo.CreateAsync(ceo);

      // Directors
      var cto = new Position("Chief Technology Officer", "CTO", techId);
      cto.SetParent(ceo.Id);
      cto.UpdateSalaryRange(new SalaryRange { Min = 60000000, Max = 100000000 });

      var cfo = new Position("Chief Financial Officer", "CFO", finId);
      cfo.SetParent(ceo.Id);
      cfo.UpdateSalaryRange(new SalaryRange { Min = 60000000, Max = 100000000 });

      var chro = new Position("Chief HR Officer", "CHRO", hrId);
      chro.SetParent(ceo.Id);
      chro.UpdateSalaryRange(new SalaryRange { Min = 50000000, Max = 90000000 });

      await posRepo.CreateAsync(cto);
      await posRepo.CreateAsync(cfo);
      await posRepo.CreateAsync(chro);

      // Managers
      var engMgr = new Position("Engineering Manager", "ENG-MGR", softId);
      engMgr.SetParent(cto.Id);
      engMgr.UpdateSalaryRange(new SalaryRange { Min = 40000000, Max = 70000000 });

      var hrMgr = new Position("HR Manager", "HR-MGR", hrId);
      hrMgr.SetParent(chro.Id);
      hrMgr.UpdateSalaryRange(new SalaryRange { Min = 30000000, Max = 50000000 });

      var salesMgr = new Position("Sales Manager", "SALE-MGR", salesId);
      salesMgr.SetParent(ceo.Id);
      salesMgr.UpdateSalaryRange(new SalaryRange { Min = 30000000, Max = 60000000 });

      var prodMgr = new Position("Product Manager", "PROD-MGR", prodId);
      prodMgr.SetParent(ceo.Id);
      prodMgr.UpdateSalaryRange(new SalaryRange { Min = 35000000, Max = 65000000 });

      var supMgr = new Position("Support Manager", "SUP-MGR", supId);
      supMgr.SetParent(ceo.Id);
      supMgr.UpdateSalaryRange(new SalaryRange { Min = 25000000, Max = 45000000 });

      var logMgr = new Position("Logistics Manager", "LOG-MGR", logId);
      logMgr.SetParent(ceo.Id);
      logMgr.UpdateSalaryRange(new SalaryRange { Min = 25000000, Max = 45000000 });

      await posRepo.CreateAsync(engMgr);
      await posRepo.CreateAsync(hrMgr);
      await posRepo.CreateAsync(salesMgr);
      await posRepo.CreateAsync(prodMgr);
      await posRepo.CreateAsync(supMgr);
      await posRepo.CreateAsync(logMgr);

      // Team Leads
      var techLead = new Position("Tech Lead", "TECH-LEAD", softId);
      techLead.SetParent(engMgr.Id);
      techLead.UpdateSalaryRange(new SalaryRange { Min = 30000000, Max = 50000000 });
      await posRepo.CreateAsync(techLead);

      var qaLead = new Position("QA Lead", "QA-LEAD", qaId);
      qaLead.SetParent(engMgr.Id);
      qaLead.UpdateSalaryRange(new SalaryRange { Min = 25000000, Max = 45000000 });
      await posRepo.CreateAsync(qaLead);

      // Staff - Tech
      var senDev = new Position("Senior Developer", "SEN-DEV", softId);
      senDev.SetParent(techLead.Id);
      senDev.UpdateSalaryRange(new SalaryRange { Min = 25000000, Max = 45000000 });
      await posRepo.CreateAsync(senDev);

      var junDev = new Position("Junior Developer", "JUN-DEV", softId);
      junDev.SetParent(techLead.Id);
      junDev.UpdateSalaryRange(new SalaryRange { Min = 10000000, Max = 20000000 });
      await posRepo.CreateAsync(junDev);

      var qaEng = new Position("QA Engineer", "QA-ENG", qaId);
      qaEng.SetParent(qaLead.Id);
      qaEng.UpdateSalaryRange(new SalaryRange { Min = 15000000, Max = 25000000 });
      await posRepo.CreateAsync(qaEng);

      // Staff - Other
      var hrSpec = new Position("HR Specialist", "HR-SPEC", hrId);
      hrSpec.SetParent(hrMgr.Id);
      hrSpec.UpdateSalaryRange(new SalaryRange { Min = 12000000, Max = 20000000 });
      await posRepo.CreateAsync(hrSpec);

      var saleExec = new Position("Sales Executive", "SALE-EXEC", salesId);
      saleExec.SetParent(salesMgr.Id);
      saleExec.UpdateSalaryRange(new SalaryRange { Min = 10000000, Max = 30000000 });
      await posRepo.CreateAsync(saleExec);

      var acc = new Position("Accountant", "ACC", finId);
      acc.SetParent(cfo.Id);
      acc.UpdateSalaryRange(new SalaryRange { Min = 15000000, Max = 25000000 });
      await posRepo.CreateAsync(acc);

      var uxDes = new Position("UI/UX Designer", "UX-DES", designId);
      uxDes.SetParent(prodMgr.Id);
      uxDes.UpdateSalaryRange(new SalaryRange { Min = 18000000, Max = 35000000 });
      await posRepo.CreateAsync(uxDes);

      var csStaff = new Position("Customer Service Agent", "CS-STAFF", csId);
      csStaff.SetParent(supMgr.Id);
      csStaff.UpdateSalaryRange(new SalaryRange { Min = 8000000, Max = 15000000 });
      await posRepo.CreateAsync(csStaff);

      var whWorker = new Position("Warehouse Worker", "WH-WORKER", whId);
      whWorker.SetParent(logMgr.Id);
      whWorker.UpdateSalaryRange(new SalaryRange { Min = 7000000, Max = 12000000 });
      await posRepo.CreateAsync(whWorker);

      Console.WriteLine("✅ Created Hierarchical Positions.");
      var positions = await posRepo.GetAllAsync();

      Console.WriteLine("🔍 Looking up specialized positions...");
      var ceoPos = positions.First(p => p.Code == "CEO");
      var ctoPos = positions.First(p => p.Code == "CTO");
      var engMgrPos = positions.First(p => p.Code == "ENG-MGR");
      var techLeadPos = positions.First(p => p.Code == "TECH-LEAD");
      var senDevPos = positions.First(p => p.Code == "SEN-DEV");
      var junDevPos = positions.First(p => p.Code == "JUN-DEV");

      // 5. EMPLOYEES (GENERATION)
      var random = new Random();
      var firstNames = new[] { "Nguyen", "Tran", "Le", "Pham", "Hoang", "Huynh", "Phan", "Vu", "Vo", "Dang", "Bui", "Do", "Ho", "Ngo", "Duong", "Ly" };
      var midNames = new[] { "Van", "Thi", "Minh", "Huu", "Duc", "Thanh", "Quoc", "Hoang", "Ngoc", "Quang", "Tuan", "Anh", "My", "Bao" };
      var lastNames = new[] { "An", "Binh", "Cuong", "Dung", "Em", "Giang", "Huy", "Hung", "Khanh", "Linh", "Minh", "Nam", "Ngan", "Phuc", "Quan", "Son", "Thao", "Thuy", "Tuan", "Uy", "Vinh", "Yen" };

      var generatedEmps = new List<EmployeeEntity>();

      Console.WriteLine("🚀 Generating CEO...");
      var ceoEmp = CreateEmployee("CEO001", "Chief Executive Officer", "ceo@hrm.com", depts.First(d => d.Code == "HQ").Id, ceoPos.Id, null, new DateTime(2020, 1, 1));
      await empRepo.CreateAsync(ceoEmp);
      generatedEmps.Add(ceoEmp);
      await CreateUserForEmployee(userManager, ceoEmp.Email, defaultPassword, "Admin", ceoEmp.Id);

      // Link the main admin account to CEO as well
      var adminUserObj = await userManager.FindByEmailAsync("admin@hrm.com");
      if (adminUserObj != null)
      {
        adminUserObj.EmployeeId = ceoEmp.Id;
        await userManager.UpdateAsync(adminUserObj);
      }

      // 5.2 Create CTO (Reports to CEO)
      var ctoEmp = CreateEmployee("CTO001", "Chief Technology Officer", "cto@hrm.com", depts.First(d => d.Code == "TECH").Id, ctoPos.Id, ceoEmp.Id, new DateTime(2020, 2, 1));
      await empRepo.CreateAsync(ctoEmp);
      generatedEmps.Add(ctoEmp);
      await CreateUserForEmployee(userManager, ctoEmp.Email, defaultPassword, "Manager", ctoEmp.Id);

      // 5.3 Create Engineering Manager (Reports to CTO)
      var engMgrEmp = CreateEmployee("MGR001", "Engineering Manager", "eng.mgr@hrm.com", depts.First(d => d.Code == "SOFT").Id, engMgrPos.Id, ctoEmp.Id, new DateTime(2021, 1, 15));
      await empRepo.CreateAsync(engMgrEmp);
      generatedEmps.Add(engMgrEmp);
      await CreateUserForEmployee(userManager, engMgrEmp.Email, defaultPassword, "Manager", engMgrEmp.Id);

      // 5.4 Create Tech Leads (Report to Eng Mgr)
      for (int i = 1; i <= 5; i++)
      {
        var lead = CreateEmployee($"LEAD{i:00}", $"{GetRandomName(firstNames, midNames, lastNames)}", $"lead{i}@hrm.com", depts.First(d => d.Code == "SOFT").Id, techLeadPos.Id, engMgrEmp.Id, DateTime.UtcNow.AddMonths(-random.Next(12, 36)));
        await empRepo.CreateAsync(lead);
        generatedEmps.Add(lead);
        await CreateUserForEmployee(userManager, lead.Email, defaultPassword, "Employee", lead.Id);

        // 5.5 Create Developers (Report to this Lead)
        int devCount = random.Next(5, 12);
        for (int j = 1; j <= devCount; j++)
        {
          var isSenior = random.NextDouble() > 0.6;
          var pos = isSenior ? senDevPos : junDevPos;
          var devCode = isSenior ? $"SEN{i}{j:00}" : $"JUN{i}{j:00}";
          var joinDate = DateTime.UtcNow.AddMonths(-random.Next(1, 24));

          var dev = CreateEmployee(devCode, GetRandomName(firstNames, midNames, lastNames), $"{devCode.ToLower()}@hrm.com", depts.First(d => d.Code == "SOFT").Id, pos.Id, lead.Id, joinDate);

          await empRepo.CreateAsync(dev);
          generatedEmps.Add(dev);
          await CreateUserForEmployee(userManager, dev.Email, defaultPassword, "Employee", dev.Id);
        }
      }

      var otherDepts = depts.Where(d => d.Code != "HQ" && d.Code != "TECH" && d.Code != "SOFT").ToList();
      var otherPositions = positions.Where(p => p.Code != "CEO" && p.Code != "CTO" && !p.Code.Contains("DEV")).ToList();

      for (int i = 0; i < 75; i++)
      {
        var dept = otherDepts[random.Next(otherDepts.Count)];
        var pos = otherPositions[random.Next(otherPositions.Count)];
        var code = $"STAFF{i:000}";
        var name = GetRandomName(firstNames, midNames, lastNames);

        var staff = CreateEmployee(code, name, $"staff{i}@hrm.com", dept.Id, pos.Id, ceoEmp.Id, DateTime.UtcNow.AddMonths(-random.Next(1, 48)));

        await empRepo.CreateAsync(staff);
        generatedEmps.Add(staff);
        await CreateUserForEmployee(userManager, staff.Email, defaultPassword, "Employee", staff.Id);
      }

      Console.WriteLine($"✅ Generated {generatedEmps.Count} Employees with Hierarchy.");
      var allEmps = await empRepo.GetAllAsync();

      // 6-12. SUBSYSTEM SEEDING
      try { await GenerateContracts(contractRepo, allEmps); } catch (Exception ex) { Console.WriteLine($"❌ Error seeding contracts: {ex.Message}"); }
      try { await GenerateLeaves(leaveTypeRepo, leaveRepo, allEmps); } catch (Exception ex) { Console.WriteLine($"❌ Error seeding leaves: {ex.Message}"); }
      try { await GenerateLeaveAllocations(leaveAllocRepo, leaveTypeRepo, allEmps); } catch (Exception ex) { Console.WriteLine($"❌ Error seeding leave allocations: {ex.Message}"); }
      try { await GenerateShifts(shiftRepo); } catch (Exception ex) { Console.WriteLine($"❌ Error seeding shifts: {ex.Message}"); }
      try { await GenerateAttendance(attendanceRepo, allEmps); } catch (Exception ex) { Console.WriteLine($"❌ Error seeding attendance: {ex.Message}"); }
      try { await GeneratePayroll(payrollRepo, allEmps); } catch (Exception ex) { Console.WriteLine($"❌ Error seeding payroll: {ex.Message}"); }
      try { await GenerateSystemSettings(settingRepo); } catch (Exception ex) { Console.WriteLine($"❌ Error seeding settings: {ex.Message}"); }
      try { await GenerateRecruitment(jobRepo, candidateRepo, interviewRepo, allEmps); } catch (Exception ex) { Console.WriteLine($"❌ Error seeding recruitment: {ex.Message}"); }

      // 10. AUDIT LOGS
      await auditRepo.CreateAsync(new AuditLog(adminUser.Id.ToString(), adminUser.FullName, "System Setup", "Internal", "SYS-001", null, "Initial database seeding completed successfully."));
      Console.WriteLine("✅ Created initial Audit Logs.");
      Console.WriteLine("🎉 DATABASE SEEDER FINISHED SUCCESSFULLY!");
    }

    // --- HELPER METHODS ---

    private static string GetRandomName(string[] first, string[] mid, string[] last)
    {
      var r = new Random();
      return $"{first[r.Next(first.Length)]} {mid[r.Next(mid.Length)]} {last[r.Next(last.Length)]}";
    }

    private static EmployeeEntity CreateEmployee(string code, string name, string email, string deptId, string posId, string? managerId, DateTime joinDate)
    {
      var r = new Random();
      var emp = new EmployeeEntity(code, name, email);

      emp.UpdatePersonalInfo(new PersonalInfo
      {
        Dob = new DateTime(r.Next(1980, 2000), r.Next(1, 13), r.Next(1, 28)),
        Gender = r.NextDouble() > 0.5 ? "Male" : "Female",
        Phone = $"090{r.Next(1000000, 9999999)}",
        Address = "TP.HCM",
        IdentityCard = r.Next(100000000, 999999999).ToString(),
        City = "Ho Chi Minh",
        Country = "Vietnam"
      });

      emp.UpdateJobDetails(new JobDetails
      {
        DepartmentId = deptId,
        PositionId = posId,
        JoinDate = joinDate,
        Status = EmployeeStatus.Active,
        ShiftId = string.Empty,
        ManagerId = managerId ?? string.Empty
      });

      emp.UpdateBankDetails(new BankDetails
      {
        BankName = "Vietcombank",
        AccountNumber = r.Next(100000000, 999999999).ToString(),
        AccountHolder = name
      });

      return emp;
    }

    private static async Task CreateUserForEmployee(UserManager<ApplicationUser> userManager, string email, string password, string role, string employeeId)
    {
      if (await userManager.FindByEmailAsync(email) == null)
      {
        var user = new ApplicationUser { UserName = email, Email = email, FullName = email, EmailConfirmed = true, EmployeeId = employeeId };
        var res = await userManager.CreateAsync(user, password);
        if (res.Succeeded) await userManager.AddToRoleAsync(user, role);
      }
    }

    private static async Task GenerateContracts(IContractRepository repo, List<EmployeeEntity> emps)
    {
      foreach (var emp in emps)
      {
        var contract = new ContractEntity(emp.Id, $"HD-{emp.EmployeeCode}", emp.JobDetails.JoinDate);
        contract.UpdateSalary(new SalaryComponents { BasicSalary = 15000000 + new Random().Next(0, 50) * 1000000 });
        contract.Activate();
        await repo.CreateAsync(contract);
      }
    }

    private static async Task GenerateLeaves(ILeaveTypeRepository typeRepo, ILeaveRequestRepository leaveRepo, List<EmployeeEntity> emps)
    {
      var annual = new LeaveType("Annual Leave", "Annual", 12);
      annual.UpdateSettings(true, 1, false, 0);
      await typeRepo.CreateAsync(annual);

      var sick = new LeaveType("Sick Leave", "Sick", 10);
      sick.UpdateSettings(false, 0, false, 0);
      await typeRepo.CreateAsync(sick);

      var r = new Random();
      foreach (var emp in emps)
      {
        if (r.NextDouble() > 0.7)
        {
          var fromDate = DateTime.UtcNow.AddDays(r.Next(1, 10));
          var toDate = fromDate.AddDays(r.Next(1, 5));
          var req = new LeaveRequest(emp.Id, r.NextDouble() > 0.3 ? LeaveTypeEnum.Annual : LeaveTypeEnum.Sick, fromDate, toDate, "Vacation");
          if (r.NextDouble() > 0.5) req.Approve("Seeder", "Auto-approved");
          await leaveRepo.CreateAsync(req);
        }
      }
    }

    private static async Task GenerateAttendance(IAttendanceRepository repo, List<EmployeeEntity> emps)
    {
      var currentMonth = DateTime.UtcNow.ToString("MM-yyyy");
      var r = new Random();
      foreach (var emp in emps)
      {
        var bucket = new AttendanceBucket(emp.Id, currentMonth);
        int today = DateTime.UtcNow.Day;
        for (int d = 1; d <= today; d++)
        {
          var date = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, d);
          if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) continue;
          var log = new DailyLog(date, r.NextDouble() > 0.05 ? AttendanceStatus.Present : AttendanceStatus.Absent);
          if (log.Status == AttendanceStatus.Present)
          {
            log.UpdateCheckTimes(date.AddHours(8), date.AddHours(17), "S01");
            log.UpdateCalculationResults(8, 0, 0, 0, AttendanceStatus.Present);
          }
          bucket.AddOrUpdateDailyLog(log);
        }
        await repo.CreateAsync(bucket);
      }
    }

    private static async Task GeneratePayroll(IPayrollRepository repo, List<EmployeeEntity> emps)
    {
      foreach (var emp in emps.Take(5))
      {
        var month = DateTime.UtcNow.AddMonths(-1).ToString("MM-yyyy");
        var payroll = new PayrollEntity(emp.Id, month);
        payroll.UpdateIncome(20000000, 2000000, 0, 0, 0);
        payroll.FinalizeCalculation(18000000, 500000);
        payroll.Approve();
        payroll.MarkAsPaid(DateTime.UtcNow.AddDays(-5));
        await repo.CreateAsync(payroll);
      }
    }

    private static async Task GenerateShifts(IShiftRepository repo)
    {
      var list = new List<Shift>
      {
        new Shift("Office Hours", "S01", new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0), new TimeSpan(12, 0, 0), new TimeSpan(13, 0, 0), 8),
        new Shift("Morning Shift", "S02", new TimeSpan(6, 0, 0), new TimeSpan(14, 0, 0), new TimeSpan(10, 0, 0), new TimeSpan(10, 30, 0), 7.5)
      };
      foreach (var s in list) await repo.CreateAsync(s);
    }

    private static async Task GenerateLeaveAllocations(ILeaveAllocationRepository allocRepo, ILeaveTypeRepository typeRepo, List<EmployeeEntity> emps)
    {
      var types = await typeRepo.GetAllAsync();
      var currentYear = DateTime.UtcNow.Year.ToString();
      foreach (var emp in emps)
      {
        foreach (var type in types)
        {
          var alloc = new LeaveAllocation(emp.Id, type.Id, currentYear, type.DefaultDaysPerYear);
          await allocRepo.CreateAsync(alloc);
        }
      }
    }

    private static async Task GenerateSystemSettings(ISystemSettingRepository repo)
    {
      var settings = new List<SystemSetting>
      {
          new("BHXH_RATE", "Payroll", "0.08", "Social Insurance"),
          new("PERSONAL_DEDUCTION", "Tax", "11000000", "Personal Deduction")
      };
      foreach (var s in settings) await repo.CreateAsync(s);
    }

    private static async Task GenerateRecruitment(IJobVacancyRepository jobRepo, ICandidateRepository candidateRepo, IInterviewRepository interviewRepo, List<EmployeeEntity> emps)
    {
      var v1 = new JobVacancy("Senior dev", 1, DateTime.UtcNow.AddDays(10));
      await jobRepo.CreateAsync(v1);
      var c1 = new Candidate("Candidate A", "a@test.com", "123", v1.Id);
      await candidateRepo.CreateAsync(c1);
    }
  }
}
