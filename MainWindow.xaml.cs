using System.Windows;

namespace PVZManagerWPF
{
    public partial class MainWindow : Window
    {
        private int employeeId;
        private string fullName;
        private string role;

        public MainWindow(int empId, string name, string userRole)
        {
            InitializeComponent();
            employeeId = empId;
            fullName = name;
            role = userRole;
            this.Title = $"PVZManager - {fullName} [{role}]";
            ConfigureMenuByRole();
        }

        private void ConfigureMenuByRole()
        {
            bool isAdmin = (role == "администратор");
            bool isOperator = (role == "оператор");
            bool isStorekeeper = (role == "кладовщик");

            btnReceive.Visibility = (isAdmin || isStorekeeper) ? Visibility.Visible : Visibility.Collapsed;
            btnReturn.Visibility = (isAdmin || isOperator) ? Visibility.Visible : Visibility.Collapsed;
            btnCells.Visibility = (isAdmin || isStorekeeper) ? Visibility.Visible : Visibility.Collapsed;
            btnReports.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            btnIssue.Visibility = (isOperator || isAdmin) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnIssue_Click(object sender, RoutedEventArgs e) => ContentFrame.Navigate(new SearchOrdersControl(employeeId, role));
        private void BtnReceive_Click(object sender, RoutedEventArgs e) => ContentFrame.Navigate(new ReceiveDeliveryControl(employeeId));
        private void BtnReturn_Click(object sender, RoutedEventArgs e) => ContentFrame.Navigate(new ReturnOrderControl(employeeId));
        private void BtnCells_Click(object sender, RoutedEventArgs e) => ContentFrame.Navigate(new StorageCellsControl());
        private void BtnReports_Click(object sender, RoutedEventArgs e) => ContentFrame.Navigate(new ReportsControl());
        private void BtnLogout_Click(object sender, RoutedEventArgs e) { new LoginWindow().Show(); this.Close(); }
    }
}