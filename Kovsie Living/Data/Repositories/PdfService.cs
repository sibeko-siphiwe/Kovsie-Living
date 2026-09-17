using Kovsie_Living.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;

namespace Kovsie_Living.Data.Repositories
{
    public class PdfService
    {
        private readonly string SerifFont = Fonts.Calibri;

        public byte[] GenerateReportPdf(Report report)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor("#212529"));

                    page.Header().Element(c => ComposeHeader(c, report));
                    page.Content().Element(c => ComposeContent(c, report));
                    page.Footer().Element(c => ComposeFooter(c, report));
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeHeader(IContainer container, Report report)
        {
            container.Column(column =>
            {
                column.Item().PaddingBottom(8).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Kovsie Living")
                            .FontSize(14)
                            .Bold()
                            .FontFamily(SerifFont)
                            .FontColor("#2c3e50");

                        col.Item().Text("Estate Management Report")
                            .FontSize(9)
                            .FontColor("#868e96");
                    });

                    row.ConstantItem(120).AlignRight().Column(col =>
                    {
                        col.Item().Text(report.GeneratedDate.ToString("dd MMM yyyy"))
                            .FontSize(9)
                            .FontColor("#868e96");

                        col.Item().Text(report.GeneratedDate.ToString("HH:mm"))
                            .FontSize(9)
                            .FontColor("#adb5bd");
                    });
                });

                column.Item().PaddingTop(4).PaddingBottom(12).Column(col =>
                {
                    col.Item().Text(report.Title)
                        .FontSize(20)
                        .Bold()
                        .FontFamily(SerifFont)
                        .FontColor("#212529");

                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.AutoItem().Text($"{report.ReportType}  ·  ")
                            .FontSize(9)
                            .FontColor("#868e96");

                        row.AutoItem().Text($"{report.StartDate:dd MMM yyyy} – {report.EndDate:dd MMM yyyy}")
                            .FontSize(9)
                            .FontColor("#868e96");
                    });
                });

                column.Item().LineHorizontal(1).LineColor("#eef1f3");
            });
        }

        private void ComposeContent(IContainer container, Report report)
        {
            container.PaddingVertical(15).Column(column =>
            {
                column.Spacing(15);

                if (!string.IsNullOrEmpty(report.ReportDataJson))
                {
                    switch (report.ReportType)
                    {
                        case "Customers":
                            var customerData = JsonSerializer.Deserialize<CustomerReportData>(report.ReportDataJson);
                            if (customerData != null)
                                ComposeCustomerReport(column, customerData);
                            break;
                        case "Payments":
                            var paymentData = JsonSerializer.Deserialize<PaymentReportData>(report.ReportDataJson);
                            if (paymentData != null)
                                ComposePaymentReport(column, paymentData);
                            break;
                        case "Revenue":
                            var revenueData = JsonSerializer.Deserialize<RevenueReportData>(report.ReportDataJson);
                            if (revenueData != null)
                                ComposeRevenueReport(column, revenueData);
                            break;
                    }
                }
            });
        }

        private void ComposeCustomerReport(ColumnDescriptor column, CustomerReportData data)
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Element(c => CreateSummaryCard(c, "Total Customers", data.TotalCustomers.ToString(), "#3b82f6", "#eff6ff"));
                row.RelativeItem().PaddingLeft(8).Element(c => CreateSummaryCard(c, "Approved", data.ApprovedCustomers.ToString(), "#22c55e", "#f0fdf4"));
                row.RelativeItem().PaddingLeft(8).Element(c => CreateSummaryCard(c, "Pending", data.PendingCustomers.ToString(), "#f59e0b", "#fffbeb"));
                row.RelativeItem().PaddingLeft(8).Element(c => CreateSummaryCard(c, "Total Collected", $"R {data.TotalCollected:N2}", "#22c55e", "#f0fdf4"));
            });

            column.Item().Text("Customer Details")
                .FontSize(12)
                .Bold()
                .FontFamily(SerifFont)
                .FontColor("#212529");

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).Text("Name").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("Type").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("Number").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("Unit").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("Status").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("Registered").Bold();
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Total Paid").Bold();
                });

                foreach (var customer in data.Customers)
                {
                    table.Cell().Element(CellStyle).Text(customer.FullName);
                    table.Cell().Element(CellStyle).Text(customer.UserType);
                    table.Cell().Element(CellStyle).Text(customer.UserNumber);
                    table.Cell().Element(CellStyle).Text(customer.UnitNumber);

                    table.Cell().Element(CellStyle).Element(c =>
                        ComposeStatusBadge(c, customer.Status));

                    table.Cell().Element(CellStyle).Text(customer.RegistrationDate.ToString("dd MMM yyyy"));
                    table.Cell().Element(CellStyle).AlignRight().Text($"R {customer.TotalPaid:N2}");
                }
            });
        }

        private void ComposePaymentReport(ColumnDescriptor column, PaymentReportData data)
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Element(c => CreateSummaryCard(c, "Total Payments", data.TotalPayments.ToString(), "#3b82f6", "#eff6ff"));
                row.RelativeItem().PaddingLeft(8).Element(c => CreateSummaryCard(c, "Approved", data.ApprovedPayments.ToString(), "#22c55e", "#f0fdf4"));
                row.RelativeItem().PaddingLeft(8).Element(c => CreateSummaryCard(c, "Pending", data.PendingPayments.ToString(), "#f59e0b", "#fffbeb"));
                row.RelativeItem().PaddingLeft(8).Element(c => CreateSummaryCard(c, "Total Amount", $"R {data.TotalAmount:N2}", "#22c55e", "#f0fdf4"));
            });

            column.Item().Text("Payment Details")
                .FontSize(12)
                .Bold()
                .FontFamily(SerifFont)
                .FontColor("#212529");

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).Text("Customer").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("Service").Bold();
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Amount").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("Date").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("Status").Bold();
                    header.Cell().Element(HeaderCellStyle).Text("Reference").Bold();
                });

                foreach (var payment in data.Payments)
                {
                    table.Cell().Element(CellStyle).Text(payment.CustomerName);
                    table.Cell().Element(CellStyle).Text(payment.TariffName);
                    table.Cell().Element(CellStyle).AlignRight().Text($"R {payment.Amount:N2}");
                    table.Cell().Element(CellStyle).Text(payment.PaymentDate.ToString("dd MMM yyyy"));

                    table.Cell().Element(CellStyle).Element(c =>
                        ComposeStatusBadge(c, payment.Status));

                    table.Cell().Element(CellStyle).Text(payment.Reference);
                }
            });
        }

        private void ComposeRevenueReport(ColumnDescriptor column, RevenueReportData data)
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Element(c => CreateSummaryCard(c, "Total Revenue", $"R {data.TotalRevenue:N2}", "#22c55e", "#f0fdf4"));
                row.RelativeItem().PaddingLeft(8).Element(c => CreateSummaryCard(c, "Pending Revenue", $"R {data.PendingRevenue:N2}", "#f59e0b", "#fffbeb"));
                row.RelativeItem().PaddingLeft(8).Element(c => CreateSummaryCard(c, "Transactions", data.TransactionCount.ToString(), "#3b82f6", "#eff6ff"));
            });

            column.Item().Text("Revenue by Tariff")
                .FontSize(12)
                .Bold()
                .FontFamily(SerifFont)
                .FontColor("#212529");

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).Text("Service").Bold();
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Amount").Bold();
                });

                foreach (var item in data.RevenueByTariff)
                {
                    table.Cell().Element(CellStyle).Text(item.Key);
                    table.Cell().Element(CellStyle).AlignRight().Text($"R {item.Value:N2}");
                }
            });

            column.Item().PaddingTop(8).Text("Monthly Breakdown")
                .FontSize(12)
                .Bold()
                .FontFamily(SerifFont)
                .FontColor("#212529");

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).Text("Month").Bold();
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Amount").Bold();
                });

                foreach (var item in data.MonthlyBreakdown)
                {
                    table.Cell().Element(CellStyle).Text(item.Key);
                    table.Cell().Element(CellStyle).AlignRight().Text($"R {item.Value:N2}");
                }
            });
        }

        private void ComposeFooter(IContainer container, Report report)
        {
            container.Column(col =>
            {
                col.Item().PaddingTop(6).LineHorizontal(1).LineColor("#eef1f3");

                col.Item().PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().Text($"Generated by Kovsie Living · {report.GeneratedBy?.FullName ?? "System"}")
                        .FontSize(8)
                        .FontColor("#adb5bd");

                    row.ConstantItem(80).AlignRight().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(8).FontColor("#adb5bd"));
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            });
        }

        private void ComposeStatusBadge(IContainer container, string status)
        {
            var (bg, fg) = status switch
            {
                "Approved" or "Paid" or "Active" or "Generated" => ("#d1fae5", "#065f46"),
                "Pending" => ("#fef3c7", "#92400e"),
                "Inactive" or "Rejected" or "Cancelled" => ("#fee2e2", "#991b1b"),
                _ => ("#f1f3f5", "#495057")
            };

            container
                .Background(bg)
                .PaddingVertical(2)
                .PaddingHorizontal(6)
                .Text(status)
                .FontSize(8)
                .FontColor(fg)
                .Bold();
        }

        private void CreateSummaryCard(IContainer container, string label, string value, string valueColor, string bgColor)
        {
            container
                .Background(bgColor)
                .Padding(10)
                .Column(column =>
                {
                    column.Item().Text(label.ToUpperInvariant())
                        .FontSize(7)
                        .FontColor("#868e96")
                        .Bold();

                    column.Item().PaddingTop(3).Text(value)
                        .FontSize(13)
                        .Bold()
                        .FontFamily(SerifFont)
                        .FontColor(valueColor);
                });
        }

        private IContainer HeaderCellStyle(IContainer container)
        {
            return container
                .Background("#f8f9fa")
                .BorderBottom(1)
                .BorderColor("#eef1f3")
                .PaddingVertical(6)
                .PaddingHorizontal(5)
                .DefaultTextStyle(x => x.FontSize(8).FontColor("#495057").Bold());
        }

        private IContainer CellStyle(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor("#f1f3f5")
                .PaddingVertical(6)
                .PaddingHorizontal(5)
                .DefaultTextStyle(x => x.FontSize(9).FontColor("#212529"));
        }
    }
}