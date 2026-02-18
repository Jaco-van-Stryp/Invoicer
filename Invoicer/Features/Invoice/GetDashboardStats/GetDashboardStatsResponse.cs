namespace Invoicer.Features.Invoice.GetDashboardStats
{
    public record struct GetDashboardStatsResponse(
        List<MonthlyIncomeItem> MonthlyIncome,
        List<ClientIncomeItem> IncomeByClient,
        InvoiceStatusSummary StatusSummary
    );

    public record struct MonthlyIncomeItem(int Year, int Month, decimal TotalPaid);

    public record struct ClientIncomeItem(Guid ClientId, string ClientName, decimal TotalPaid);

    public record struct InvoiceStatusSummary(int PaidCount, int PartialCount, int UnpaidCount);
}
