using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Projet_Budget_M1.Models;

public class CategorieWrapper : INotifyPropertyChanged
{
    private bool _isSelected;

    public Categorie Categorie { get; set; } = null!;
    
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
