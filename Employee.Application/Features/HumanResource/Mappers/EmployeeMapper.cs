using Employee.Domain.Common.Models;
using Employee.Application.Features.HumanResource.Dtos;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Entities.ValueObjects;
using Employee.Domain.Enums;
using System;

namespace Employee.Application.Features.HumanResource.Mappers
{
  public static class EmployeeMapper
  {
    // 1. DTO -> ENTITY (Create)
    public static EmployeeEntity ToEntity(this CreateEmployeeDto dto)
    {
      var entity = new EmployeeEntity(dto.EmployeeCode, dto.FullName, dto.Email);
      entity.UpdateAvatar(dto.AvatarUrl);

      var personalInfo = new PersonalInfo
      {
        Dob = dto.PersonalInfo.DateOfBirth,
        Gender = dto.PersonalInfo.Gender,
        Phone = dto.PersonalInfo.PhoneNumber,
        Address = dto.PersonalInfo.Address,
        IdentityCard = dto.PersonalInfo.IdentityCard,
        MaritalStatus = dto.PersonalInfo.MaritalStatus,
        Nationality = dto.PersonalInfo.Nationality,
        Hometown = dto.PersonalInfo.Hometown,
        Country = dto.PersonalInfo.Country,
        City = dto.PersonalInfo.City,
        PostalCode = dto.PersonalInfo.PostalCode
            };
      entity.UpdatePersonalInfo(personalInfo);

      Enum.TryParse<EmployeeStatus>(dto.JobDetails.Status, true, out var status);

      var jobDetails = new JobDetails
      {
        DepartmentId = dto.JobDetails.DepartmentId,
        PositionId = dto.JobDetails.PositionId,
        JoinDate = dto.JobDetails.JoinDate,
              Status = status,
              ShiftId = dto.JobDetails.ShiftId ?? string.Empty,
              ResumeUrl = dto.JobDetails.ResumeUrl,
              ContractUrl = dto.JobDetails.ContractUrl,
              ManagerId = dto.JobDetails.ManagerId ?? string.Empty,
              ProbationEndDate = dto.JobDetails.ProbationEndDate
            };
      entity.UpdateJobDetails(jobDetails);

      var bankDetails = new BankDetails
      {
        BankName = dto.BankDetails.BankName,
        AccountNumber = dto.BankDetails.AccountNumber,
              AccountHolder = dto.BankDetails.AccountHolder ?? dto.FullName,
              InsuranceCode = dto.BankDetails.InsuranceCode,
              TaxCode = dto.BankDetails.TaxCode
            };
      entity.UpdateBankDetails(bankDetails);

      return entity;
    }

    // 2. ENTITY -> DTO (View)
    public static EmployeeDto ToDto(this EmployeeEntity entity)
    {
      return new EmployeeDto
      {
        Id = entity.Id,
        EmployeeCode = entity.EmployeeCode,
        FullName = entity.FullName,
        Email = entity.Email,
        AvatarUrl = entity.AvatarUrl,
        Version = entity.Version,

              PersonalInfo = new PersonalInfoDto
              {
                DateOfBirth = entity.PersonalInfo.Dob,
                Gender = entity.PersonalInfo.Gender,
                PhoneNumber = entity.PersonalInfo.Phone,
                Address = entity.PersonalInfo.Address,
                IdentityCard = entity.PersonalInfo.IdentityCard,
                MaritalStatus = entity.PersonalInfo.MaritalStatus,
                Nationality = entity.PersonalInfo.Nationality,
                Hometown = entity.PersonalInfo.Hometown,
                Country = entity.PersonalInfo.Country,
                City = entity.PersonalInfo.City,
                PostalCode = entity.PersonalInfo.PostalCode
              },

              JobDetails = new JobDetailsDto
              {
                DepartmentId = entity.JobDetails.DepartmentId,
                PositionId = entity.JobDetails.PositionId,
                JoinDate = entity.JobDetails.JoinDate,
                  Status = entity.JobDetails.Status.ToString(),
                  ShiftId = entity.JobDetails.ShiftId,
                  ResumeUrl = entity.JobDetails.ResumeUrl,
                  ContractUrl = entity.JobDetails.ContractUrl,
                  ManagerId = entity.JobDetails.ManagerId,
                  ProbationEndDate = entity.JobDetails.ProbationEndDate
                },

              BankDetails = new BankDetailsDto
              {
                BankName = entity.BankDetails.BankName,
                AccountNumber = entity.BankDetails.AccountNumber,
                AccountHolder = entity.BankDetails.AccountHolder,
                InsuranceCode = entity.BankDetails.InsuranceCode,
                TaxCode = entity.BankDetails.TaxCode
              }
            };
    }

    // 3. UPDATE ENTITY
    public static void UpdateFromDto(this EmployeeEntity entity, UpdateEmployeeDto dto)
    {
      entity.UpdateBasicInfo(dto.FullName, dto.Email);
      entity.UpdateAvatar(dto.AvatarUrl);

      var personalInfo = new PersonalInfo
      {
        Dob = dto.PersonalInfo.DateOfBirth,
        Gender = dto.PersonalInfo.Gender,
        Phone = dto.PersonalInfo.PhoneNumber,
        Address = dto.PersonalInfo.Address,
        IdentityCard = dto.PersonalInfo.IdentityCard,
        MaritalStatus = dto.PersonalInfo.MaritalStatus,
        Nationality = dto.PersonalInfo.Nationality,
        Hometown = dto.PersonalInfo.Hometown,
        Country = dto.PersonalInfo.Country,
        City = dto.PersonalInfo.City,
        PostalCode = dto.PersonalInfo.PostalCode
      };
      entity.UpdatePersonalInfo(personalInfo);

      Enum.TryParse<EmployeeStatus>(dto.JobDetails.Status, true, out var status);

      var jobDetails = new JobDetails
      {
        DepartmentId = dto.JobDetails.DepartmentId,
        PositionId = dto.JobDetails.PositionId,
        JoinDate = dto.JobDetails.JoinDate,
              Status = status,
              ShiftId = dto.JobDetails.ShiftId ?? string.Empty,
              ResumeUrl = dto.JobDetails.ResumeUrl,
              ContractUrl = dto.JobDetails.ContractUrl,
              ManagerId = dto.JobDetails.ManagerId ?? string.Empty,
              ProbationEndDate = dto.JobDetails.ProbationEndDate
            };
      entity.UpdateJobDetails(jobDetails);

      var bankDetails = new BankDetails
      {
        BankName = dto.BankDetails.BankName,
        AccountNumber = dto.BankDetails.AccountNumber,
        AccountHolder = dto.BankDetails.AccountHolder ?? dto.FullName,
        InsuranceCode = dto.BankDetails.InsuranceCode,
        TaxCode = dto.BankDetails.TaxCode
      };
      entity.UpdateBankDetails(bankDetails);
    }
  }
}
