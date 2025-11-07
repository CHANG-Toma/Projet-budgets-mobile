using System.Collections.ObjectModel;

namespace Projet_Budget_M1.Models
{
    public class TransactionGroup : ObservableCollection<Transaction>
    {
        public string GroupName { get; set; } = string.Empty;
        public DateTime GroupDate { get; set; }
        
        public TransactionGroup(string groupName, DateTime groupDate)
        {
            GroupName = groupName;
            GroupDate = groupDate;
        }
    }
}

