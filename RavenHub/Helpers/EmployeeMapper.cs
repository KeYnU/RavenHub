using System;
using System.Data;
using RavenHub.Models;

namespace RavenHub.Helpers
{
    public static class EmployeeMapper
    {
        public static Employee DataRowToEmployee(DataRow row)
        {
            return new Employee
            {
                EmployeeId = row["EmployeeId"] != DBNull.Value ? (int)row["EmployeeId"] : 0,
                FullName = row["FullName"]?.ToString() ?? "",
                Position = row["Position"]?.ToString() ?? "",
                PhoneNumber = row["PhoneNumber"]?.ToString() ?? "",
                Email = row["Email"]?.ToString() ?? "",
                SocialLink = row["SocialLink"]?.ToString() ?? "",
                PositionId = row["PositionId"] != DBNull.Value ? (int)row["PositionId"] : 0
            };
        }
    }
}