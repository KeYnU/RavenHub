using System;
using System.Data;

namespace RavenHub.Pages
{
    public static class TaskFilter
    {
        public static void ApplyFilter(DataView dataView, string searchText, string statusFilter)
        {
            if (dataView == null) return;

            string filter = BuildFilterExpression(searchText, statusFilter);
            dataView.RowFilter = filter;
        }

        private static string BuildFilterExpression(string searchText, string statusFilter)
        {
            string filter = "";

            // Поиск по тексту
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                filter = $"(Title LIKE '%{EscapeLikeValue(searchText)}%' OR Description LIKE '%{EscapeLikeValue(searchText)}%')";
            }

            // Фильтр по статусу
            string statusFilterExpr = GetStatusFilterExpression(statusFilter);
            if (!string.IsNullOrEmpty(statusFilterExpr))
            {
                filter = string.IsNullOrEmpty(filter)
                    ? statusFilterExpr
                    : $"{filter} AND {statusFilterExpr}";
            }

            return filter;
        }

        private static string GetStatusFilterExpression(string statusFilter)
        {
            DateTime now = DateTime.Now;
            string nowString = now.ToString("yyyy-MM-dd HH:mm:ss");

            switch (statusFilter)
            {
                case "Активные":
                    return $"(IsCompleted = 0 AND Deadline >= #{nowString}#)";
                case "Просроченные":
                    return $"(IsCompleted = 0 AND Deadline < #{nowString}#)";
                case "Завершенные":
                    return "(IsCompleted = 1)";
                default:
                    return "";
            }
        }

        private static string EscapeLikeValue(string value)
        {
            return value.Replace("[", "[[]")
                       .Replace("%", "[%]")
                       .Replace("_", "[_]");
        }
    }
}