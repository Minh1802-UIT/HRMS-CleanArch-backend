using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Entities.Organization;
using Employee.Domain.Entities.Leave;
using Employee.Domain.Entities.Payroll;
using Employee.Domain.Entities.Attendance;
using Employee.Domain.Entities.Common;
using Employee.Domain.Enums;
using Employee.Infrastructure.Identity.Models;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson;

namespace Employee.Infrastructure.Persistence
{
  public static class MongoMappingConfig
  {
    private static bool _isRegistered = false;

    public static void RegisterMappings()
    {
      if (_isRegistered) return;
      _isRegistered = true;

      // Global Enum Serializer
      BsonSerializer.RegisterSerializer(new EnumSerializer<AttendanceStatus>(BsonType.String));
      BsonSerializer.RegisterSerializer(new EnumSerializer<CandidateStatus>(BsonType.String));
      BsonSerializer.RegisterSerializer(new EnumSerializer<ContractStatus>(BsonType.String));
      BsonSerializer.RegisterSerializer(new EnumSerializer<EmployeeStatus>(BsonType.String));
      BsonSerializer.RegisterSerializer(new EnumSerializer<InterviewStatus>(BsonType.String));
      BsonSerializer.RegisterSerializer(new EnumSerializer<JobVacancyStatus>(BsonType.String));
      BsonSerializer.RegisterSerializer(new EnumSerializer<LeaveStatus>(BsonType.String));
      BsonSerializer.RegisterSerializer(new EnumSerializer<LeaveCategory>(BsonType.String));
      BsonSerializer.RegisterSerializer(new EnumSerializer<PayrollStatus>(BsonType.String));
      BsonSerializer.RegisterSerializer(new EnumSerializer<RawLogType>(BsonType.String));

      // BaseEntity mapping
      if (!BsonClassMap.IsClassMapRegistered(typeof(BaseEntity)))
      {
        BsonClassMap.RegisterClassMap<BaseEntity>(cm =>
        {
          cm.AutoMap();
          cm.MapIdMember(c => c.Id);
          cm.UnmapProperty(c => c.DomainEvents);
        });
      }

      // Employee mapping
      BsonClassMap.RegisterClassMap<EmployeeEntity>(cm =>
      {
        cm.AutoMap();
        cm.SetIgnoreExtraElements(true);
      });

      // Contract mapping
      BsonClassMap.RegisterClassMap<ContractEntity>(cm =>
      {
        cm.AutoMap();
        cm.SetIgnoreExtraElements(true);
        cm.MapCreator(c => (ContractEntity)System.Activator.CreateInstance(typeof(ContractEntity), true)!);
      });

      // Auth Mappings
      BsonClassMap.RegisterClassMap<ApplicationUser>(cm =>
      {
        cm.AutoMap();
        cm.SetIgnoreExtraElements(true);
      });

      BsonClassMap.RegisterClassMap<ApplicationRole>(cm =>
      {
        cm.AutoMap();
        cm.SetIgnoreExtraElements(true);
      });

      // Organization
      BsonClassMap.RegisterClassMap<Department>(cm => cm.AutoMap());
      BsonClassMap.RegisterClassMap<Position>(cm => cm.AutoMap());

      // Leave
      BsonClassMap.RegisterClassMap<LeaveType>(cm => cm.AutoMap());
      BsonClassMap.RegisterClassMap<LeaveAllocation>(cm => cm.AutoMap());
      BsonClassMap.RegisterClassMap<LeaveRequest>(cm => cm.AutoMap());

      // Payroll
      BsonClassMap.RegisterClassMap<PayrollEntity>(cm => cm.AutoMap());

      // Attendance
      BsonClassMap.RegisterClassMap<Shift>(cm => cm.AutoMap());
      BsonClassMap.RegisterClassMap<AttendanceBucket>(cm => cm.AutoMap());
      BsonClassMap.RegisterClassMap<RawAttendanceLog>(cm => cm.AutoMap());

      // Common
      BsonClassMap.RegisterClassMap<AuditLog>(cm => cm.AutoMap());
    }
  }
}
