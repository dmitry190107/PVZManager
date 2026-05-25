using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Controls;

namespace PVZManagerWPF
{
    public partial class SearchOrdersControl : UserControl
    {
        private int employeeId;
        private string role;
        private string connString;

        public SearchOrdersControl(int empId, string userRole)
        {
            InitializeComponent();
            employeeId = empId;
            role = userRole;
            connString = System.Configuration.ConfigurationManager.ConnectionStrings["WarehouseDB"].ConnectionString;
        }

        private void BtnSearch_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string search = txtSearch.Text.Trim();
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string sql = @"SELECT o.order_id, o.external_order_number, 
                                      c.full_name AS customer_name, c.phone AS customer_phone,
                                      o.status, o.total_amount
                               FROM Orders o
                               LEFT JOIN Clients c ON o.client_id = c.client_id
                               WHERE @SearchString IS NULL 
                                  OR o.external_order_number LIKE '%' + @SearchString + '%'
                                  OR c.phone LIKE '%' + @SearchString + '%'
                                  OR c.full_name LIKE '%' + @SearchString + '%'
                               ORDER BY o.received_date DESC";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@SearchString", (object)search ?? DBNull.Value);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
            }
            dgvOrders.ItemsSource = dt.DefaultView;
        }

        private void DgvOrders_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (dgvOrders.SelectedItem != null)
            {
                DataRowView row = (DataRowView)dgvOrders.SelectedItem;
                int orderId = Convert.ToInt32(row["order_id"]);
                var win = new System.Windows.Window
                {
                    Title = "Выдача заказа",
                    Content = new IssueOrderControl(orderId, employeeId),
                    Width = 600,
                    Height = 500,
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                    Owner = System.Windows.Application.Current.MainWindow
                };
                win.ShowDialog();
                BtnSearch_Click(null, null);
            }
        }
    }
}