using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Controls;

namespace PVZManagerWPF
{
    public partial class ReportsControl : UserControl
    {
        private string connString;

        public ReportsControl()
        {
            InitializeComponent();
            connString = System.Configuration.ConfigurationManager.ConnectionStrings["WarehouseDB"].ConnectionString;
            LoadReports();
        }

        private void LoadReports()
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM IssuedOrders WHERE issue_date = CAST(GETDATE() AS DATE)", conn);
                int today = (int)cmd.ExecuteScalar();
                lblToday.Text = $"Выдано сегодня: {today}";

                DataTable dt = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter(
                    @"SELECT s.shop_name, COUNT(o.order_id) AS cnt
                      FROM Orders o JOIN Shops s ON o.shop_id = s.shop_id
                      WHERE o.status = 'выдан' AND o.received_date >= DATEADD(month, -1, GETDATE())
                      GROUP BY s.shop_name", conn);
                da.Fill(dt);
                dgvStats.ItemsSource = dt.DefaultView;
            }
        }
    }
}