using System;

namespace RavenHub.Models
{
    public class Employee
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; }
        public int PositionId { get; set; }
        public string Position { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string SocialLink { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserLogin { get; set; }
    }
}