namespace Employee.Domain.Enums
{
    public enum ContractStatus
    {
        Draft,
        Pending,      // Signed but StartDate is in the future — not yet effective
        Active,
        Expired,
        Terminated
    }
}
