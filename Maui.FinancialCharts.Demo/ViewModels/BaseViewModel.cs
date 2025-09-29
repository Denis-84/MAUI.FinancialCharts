using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Maui.FinancialCharts.Demo.ViewModels;

public abstract class BaseViewModel :
	INotifyPropertyChanged {

	public event PropertyChangedEventHandler? PropertyChanged;

	protected void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
