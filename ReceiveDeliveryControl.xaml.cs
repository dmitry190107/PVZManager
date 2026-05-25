using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace PVZManagerWPF
{
    public partial class ReceiveDeliveryControl : UserControl
    {
        private int employeeId;
        private string connString;

        public ReceiveDeliveryControl(int empId)
        {
            InitializeComponent();
            employeeId = empId;
            connString = System.Configuration.ConfigurationManager.ConnectionStrings["WarehouseDB"].ConnectionString;
            LoadDeliveries();
        }

        private void LoadDeliveries()
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(connString))
            {
                SqlDataAdapter da = new SqlDataAdapter("SELECT delivery_id, waybill_number FROM Deliveries WHERE status = 'обрабатывается'", conn);
                da.Fill(dt);
            }
            cboDelivery.ItemsSource = dt.DefaultView;
            cboDelivery.DisplayMemberPath = "waybill_number";
            cboDelivery.SelectedValuePath = "delivery_id";
        }

        private void BtnAccept_Click(object sender, RoutedEventArgs e)
        {
            if (cboDelivery.SelectedValue == null)
            {
                MessageBox.Show("Выберите поставку.");
                return;
            }
            int deliveryId = Convert.ToInt32(cboDelivery.SelectedValue);

            // Здесь нужно получить список заказов. Для примера создаём DataTable вручную.
            // В реальном приложении вы можете загружать данные из CSV/Excel или вводить через форму.
            DataTable orders = new DataTable();
            orders.Columns.Add("external_order_number", typeof(string));
            orders.Columns.Add("shop_id", typeof(int));
            orders.Columns.Add("customer_name", typeof(string));
            orders.Columns.Add("customer_phone", typeof(string));
            orders.Columns.Add("customer_email", typeof(string));
            orders.Columns.Add("payment_status", typeof(string));
            orders.Columns.Add("total_amount", typeof(decimal));

            // Добавим тестовые заказы
            orders.Rows.Add("NEW-001", 1, "Тестовый клиент", "79990000000", "", "оплачен", 1500);
            orders.Rows.Add("NEW-002", 2, "Другой клиент", "79991111111", "", "не_оплачен", 3200);

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    foreach (DataRow row in orders.Rows)
                    {
                        // Найти свободную ячейку
                        SqlCommand cmdCell = new SqlCommand(
                            "SELECT TOP 1 cell_id FROM StorageCells WHERE status = 'свободна' ORDER BY cell_id", conn, transaction);
                        object cellObj = cmdCell.ExecuteScalar();
                        if (cellObj == null) throw new Exception("Нет свободных ячеек");
                        int cellId = Convert.ToInt32(cellObj);

                        // Вставить заказ
                        SqlCommand cmdOrder = new SqlCommand(
                            @"INSERT INTO Orders (external_order_number, shop_id, delivery_id, client_id, received_date, available_date, 
                                                  payment_status, total_amount, status, cell_id)
                              VALUES (@ext, @shop, @delivery, NULL, GETDATE(), DATEADD(day,1,GETDATE()), @pay, @total, 'на_складе', @cell)",
                            conn, transaction);
                        cmdOrder.Parameters.AddWithValue("@ext", row["external_order_number"]);
                        cmdOrder.Parameters.AddWithValue("@shop", row["shop_id"]);
                        cmdOrder.Parameters.AddWithValue("@delivery", deliveryId);
                        cmdOrder.Parameters.AddWithValue("@pay", row["payment_status"]);
                        cmdOrder.Parameters.AddWithValue("@total", row["total_amount"]);
                        cmdOrder.Parameters.AddWithValue("@cell", cellId);
                        cmdOrder.ExecuteNonQuery();

                        // Занять ячейку
                        SqlCommand cmdOccupy = new SqlCommand(
                            "UPDATE StorageCells SET status = 'занята' WHERE cell_id = @cell", conn, transaction);
                        cmdOccupy.Parameters.AddWithValue("@cell", cellId);
                        cmdOccupy.ExecuteNonQuery();
                    }

                    // Обновить статус поставки
                    SqlCommand cmdDelivery = new SqlCommand(
                        "UPDATE Deliveries SET status = 'завершена' WHERE delivery_id = @id", conn, transaction);
                    cmdDelivery.Parameters.AddWithValue("@id", deliveryId);
                    cmdDelivery.ExecuteNonQuery();

                    transaction.Commit();
                    MessageBox.Show("Поставка принята.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadDeliveries();
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