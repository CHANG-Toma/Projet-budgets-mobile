using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Maui.Storage;
using Projet_Budget_M1.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDFInfrastructure = QuestPDF.Infrastructure;
using QuestPDFColors = QuestPDF.Helpers.Colors;

namespace Projet_Budget_M1.Services
{
    public class PdfService
    {
        public static Task<string> GenerateTransactionsPdfAsync(
            List<Transaction> transactions, 
            string exportType = "all",
            string? filterValue = null)
        {
            return Task.Run(() =>
            {
                QuestPDF.Settings.License = QuestPDFInfrastructure.LicenseType.Community;

                var fileName = GenerateFileName(exportType, filterValue);
                var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, QuestPDFInfrastructure.Unit.Centimetre);
                        page.PageColor(QuestPDFColors.White);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header()
                            .Element(ComposeHeader);

                        page.Content()
                            .Element(container => ComposeContent(container, transactions, exportType, filterValue));

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.CurrentPageNumber();
                                x.Span(" / ");
                                x.TotalPages();
                            });
                    });
                });

                document.GeneratePdf(filePath);

                return filePath;
            });
        }

        private static void ComposeHeader(QuestPDFInfrastructure.IContainer container)
        {
            container
                .Row(row =>
                {
                    row.RelativeColumn().Column(column =>
                    {
                        column.Item().Text("Rapport des Transactions")
                            .FontSize(20)
                            .Bold()
                            .FontColor(QuestPDFColors.Blue.Darken3);
                    });

                    row.ConstantColumn(100).AlignRight().Text(DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                        .FontSize(10)
                        .FontColor(QuestPDFColors.Grey.Medium);
                });
        }

        private static void ComposeContent(QuestPDFInfrastructure.IContainer container, List<Transaction> transactions, string exportType, string? filterValue)
        {
            container.Column(column =>
            {
                // Titre du filtre
                if (!string.IsNullOrEmpty(filterValue))
                {
                    column.Item().PaddingBottom(10).Text($"Filtre: {filterValue}")
                        .FontSize(14)
                        .Bold()
                        .FontColor(QuestPDFColors.Blue.Darken2);
                }

                // Statistiques
                var totalIncome = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
                var totalExpenses = transactions.Where(t => t.Amount < 0).Sum(t => Math.Abs(t.Amount));
                var balance = totalIncome - totalExpenses;

                column.Item().PaddingBottom(15).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Revenus").Bold();
                        header.Cell().Element(CellStyle).Text("Dépenses").Bold();
                        header.Cell().Element(CellStyle).Text("Solde").Bold();
                    });

                    table.Cell().Element(CellStyle).Text($"{totalIncome:F2} €").FontColor(QuestPDFColors.Green.Darken2);
                    table.Cell().Element(CellStyle).Text($"{totalExpenses:F2} €").FontColor(QuestPDFColors.Red.Darken2);
                    table.Cell().Element(CellStyle).Text($"{balance:F2} €")
                        .FontColor(balance >= 0 ? QuestPDFColors.Green.Darken2 : QuestPDFColors.Red.Darken2);
                });

                // Grouper les transactions selon le type d'export
                if (exportType == "month")
                {
                    var groupedByMonth = transactions
                        .GroupBy(t => new { t.Date.Year, t.Date.Month })
                        .OrderByDescending(g => g.Key.Year)
                        .ThenByDescending(g => g.Key.Month);

                    foreach (var group in groupedByMonth)
                    {
                        var groupTitle = $"{new DateTime(group.Key.Year, group.Key.Month, 1):MMMM yyyy}";
                        RenderTransactionGroup(column, groupTitle, group.OrderByDescending(t => t.Date).ToList());
                    }
                }
                else if (exportType == "category")
                {
                    var groupedByCategory = transactions
                        .GroupBy(t => t.Category)
                        .OrderBy(g => g.Key);

                    foreach (var group in groupedByCategory)
                    {
                        RenderTransactionGroup(column, group.Key, group.OrderByDescending(t => t.Date).ToList());
                    }
                }
                else
                {
                    // Toutes les transactions
                    RenderTransactionGroup(column, "Toutes les transactions", transactions.OrderByDescending(t => t.Date).ToList());
                }
            });
        }

        private static void RenderTransactionGroup(ColumnDescriptor column, string groupTitle, List<Transaction> transactions)
        {
            column.Item().PaddingTop(15).Text(groupTitle)
                .FontSize(16)
                .Bold()
                .FontColor(QuestPDFColors.Blue.Darken3);

            // Table des transactions
            column.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Titre").Bold();
                    header.Cell().Element(CellStyle).Text("Catégorie").Bold();
                    header.Cell().Element(CellStyle).Text("Date").Bold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Montant").Bold();
                });

                foreach (var transaction in transactions)
                {
                    table.Cell().Element(CellStyle).Text(transaction.Title);
                    table.Cell().Element(CellStyle).Text(transaction.Category);
                    table.Cell().Element(CellStyle).Text(transaction.Date.ToString("dd/MM/yyyy"));
                    table.Cell().Element(CellStyle).AlignRight()
                        .Text($"{transaction.Amount:+0.00;-0.00} €")
                        .FontColor(transaction.Amount >= 0 ? QuestPDFColors.Green.Darken2 : QuestPDFColors.Red.Darken2);
                }
            });
        }

        private static QuestPDFInfrastructure.IContainer CellStyle(QuestPDFInfrastructure.IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(QuestPDFColors.Grey.Lighten2)
                .PaddingVertical(5)
                .PaddingHorizontal(5);
        }

        private static string GenerateFileName(string exportType, string? filterValue)
        {
            var baseName = "Transactions";
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            
            var fileName = exportType switch
            {
                "month" => $"{baseName}_Mois_{filterValue?.Replace(" ", "_") ?? timestamp}_{timestamp}.pdf",
                "category" => $"{baseName}_Categorie_{filterValue?.Replace(" ", "_") ?? "Toutes"}_{timestamp}.pdf",
                _ => $"{baseName}_{timestamp}.pdf"
            };

            return fileName;
        }
    }
}

