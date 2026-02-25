using AspNetCore.Identity.MongoDbCore.Models;
using System;

namespace Employee.Infrastructure.Identity.Models
{
    public class ApplicationRole : MongoIdentityRole<Guid>
    {
        public ApplicationRole() : base() { }
        public ApplicationRole(string roleName) : base(roleName) { }
    }
}
