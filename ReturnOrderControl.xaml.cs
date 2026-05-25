using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace PVZManagerWPF
{
    public partial class ReturnOrderControl : UserControl
    {
        private int employeeId;
        private string connString;

        public ReturnOrderControl(int empId)
        {
            InitializeComponent();
            employeeId = empId;
            connString = System.Configuration.ConfigurationManager.ConnectionStrings["WarehouseDB"].ConnectionString;
        }

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtOrderId.Text, out int orderId))
            {
                MessageBox.Show("Введите числовой ID заказа.");
                return;
            }

            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string sql = @"SELECT oi.item_id, oi.product_name, oi.price
                               FROM OrderItems oi
                               JOIN Orders o ON oi.order_id = o.order_id
                               WHERE o.order_id = @oid AND o.status = 'выдан'";
                SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                da.SelectCommand.Parameters.AddWithValue("@oid", orderId);
                da.Fill(dt);
            }
            if (dt.Rows.Count == 0)
            {
                MessageBox.Show("Нет выданных товаров для возврата.");
                return;
            }
            dgvItems.ItemsSource = dt.DefaultView;
        }

        private void BtnReturn_Click(object sender, RoutedEventArgs e)
        {
            if (dgvItems.SelectedItem == null)
            {
                MessageBox.Show("Выберите товар для возврата.");
                return;
            }
            DataRowView row = (DataRowView)dgvItems.SelectedItem;
            int itemId = Convert.ToInt32(row["item_id"]);
            int orderId = Convert.ToInt32(txtOrderId.Text);
            string reason = (cboReason.SelectedItem as ComboBoxItem)?.Content.ToString();
            string condition = (cboCondition.SelectedItem as ComboBoxItem)?.Content.ToString();

            string insert = @"INSERT INTO Returns (order_id, item_id, return_date, reason, condition, refund_amount, employee_id, status)
                              VALUES (@oid, @iid, GETDATE(), @reason, @cond, (SELECT price FROM OrderItems WHERE item_id = @iid), @eid, 'оформлен')";
            using (SqlConnection conn = new SqlConnection(connString))
            {
                SqlCommand cmd = new SqlCommand(insert, conn);
                cmd.Parameters.AddWithValue("@oid", orderId);
                cmd.Parameters.AddWithValue("@iid", itemId);
                cmd.Parameters.AddWithValue("@reason", reason);
                cmd.Parameters.AddWithValue("@cond", condition);
                cmd.Parameters.AddWithValue("@eid", employeeId);
                conn.Open();
                cmd.ExecuteNonQuery();
                MessageBox.Show("Возврат оформлен.");
                BtnLoad_Click(null, null);
            }
        }
    }
}