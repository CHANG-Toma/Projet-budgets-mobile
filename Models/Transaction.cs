using System;

namespace Projet_Budget_M1.Models;

public class Transaction
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public double Amount { get; set; }
    public DateTime Date { get; set; }
    public string Category { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
}


