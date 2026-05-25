using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace PVZManagerWPF
{
    public partial class IssueOrderControl : UserControl
    {
        private int orderId;
        private int employeeId;
        private string connString;

        public IssueOrderControl(int ordId, int empId)
        {
            InitializeComponent();
            orderId = ordId;
            employeeId = empId;
            connString = System.Configuration.ConfigurationManager.ConnectionStrings["WarehouseDB"].ConnectionString;
            LoadOrderInfo();
        }

        private void LoadOrderInfo()
        {
            string sql = @"SELECT o.external_order_number, c.full_name, c.phone, o.status, 
                                  sc.rack_number, sc.shelf_number, sc.cell_number
                           FROM Orders o
                           LEFT JOIN Clients c ON o.client_id = c.client_id
                           LEFT JOIN StorageCells sc ON o.cell_id = sc.cell_id
                           WHERE o.order_id = @id";
            using (SqlConnection conn = new SqlConnection(connString))
            {
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", orderId);
                conn.Open();
                SqlDataReader r = cmd.ExecuteReader();
                if (r.Read())
                {
                    lblOrderNum.Text = r["external_order_number"].ToString();
                    lblCustomer.Text = r["full_name"].ToString();
                    lblPhone.Text = r["phone"].ToString();
                    lblStatus.Text = r["status"].ToString();
                    lblCell.Text = $"{r["rack_number"]}-{r["shelf_number"]}-{r["cell_number"]}";
                }
                r.Close();
            }

            string itemsSql = "SELECT product_name, quantity_ordered, price FROM OrderItems WHERE order_id = @id";
            using (SqlConnection conn = new SqlConnection(connString))
            {
                SqlDataAdapter da = new SqlDataAdapter(itemsSql, conn);
                da.SelectCommand.Parameters.AddWithValue("@id", orderId);
                DataTable dt = new DataTable();
                da.Fill(dt);
                dgvItems.ItemsSource = dt.DefaultView;
            }
        }

        private void BtnIssue_Click(object sender, RoutedEventArgs e)
        {
            string method = (cboVerification.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (string.IsNullOrEmpty(method))
            {
                MessageBox.Show("Выберите способ подтверждения.");
                return;
            }

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    // Проверка статуса
                    SqlCommand cmdCheck = new SqlCommand("SELECT status FROM Orders WHERE order_id = @id", conn, transaction);
                    cmdCheck.Parameters.AddWithValue("@id", orderId);
                    string status = cmdCheck.ExecuteScalar()?.ToString();
                    if (status != "готов_к_выдаче")
                    {
                        MessageBox.Show("Заказ не готов к выдаче.");
                        transaction.Rollback();
                        return;
                    }

                    // Вставка в IssuedOrders
                    SqlCommand cmdInsert = new SqlCommand(
                        @"INSERT INTO IssuedOrders (order_id, employee_id, issue_date, issue_time, verification_method)
                          VALUES (@oid, @eid, CAST(GETDATE() AS DATE), CAST(GETDATE() AS TIME), @method)", conn, transaction);
                    cmdInsert.Parameters.AddWithValue("@oid", orderId);
                    cmdInsert.Parameters.AddWithValue("@eid", employeeId);
                    cmdInsert.Parameters.AddWithValue("@method", method);
                    cmdInsert.ExecuteNonQuery();

                    // Обновить статус заказа, освободить ячейку
                    SqlCommand cmdUpdate = new SqlCommand(
                        "UPDATE Orders SET status = 'выдан', cell_id = NULL WHERE order_id = @id", conn, transaction);
                    cmdUpdate.Parameters.AddWithValue("@id", orderId);
                    cmdUpdate.ExecuteNonQuery();

                    // Освободить ячейку в StorageCells
                    SqlCommand cmdFreeCell = new SqlCommand(
                        "UPDATE StorageCells SET status = 'свободна' WHERE cell_id IN (SELECT cell_id FROM Orders WHERE order_id = @id)", conn, transaction);
                    cmdFreeCell.Parameters.AddWithValue("@id", orderId);
                    cmdFreeCell.ExecuteNonQuery();

                    transaction.Commit();
                    MessageBox.Show("Заказ успешно выдан!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    Window.GetWindow(this).Close();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show("Ошибка: " + ex.Message);
                }
            }
        }
    }
}