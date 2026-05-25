using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace PVZManagerWPF
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password;
            string role = (cboRole.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password) || role == null)
            {
                txtError.Text = "Заполните все поля.";
                return;
            }

            string connString = System.Configuration.ConfigurationManager.ConnectionStrings["WarehouseDB"].ConnectionString;
            string query = "SELECT employee_id, full_name FROM Employees WHERE login = @login AND position = @role AND status = 'активен'";

            using (SqlConnection conn = new SqlConnection(connString))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@login", login);
                cmd.Parameters.AddWithValue("@role", role);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    int empId = reader.GetInt32(0);
                    string fullName = reader.GetString(1);
                    reader.Close();

                    MainWindow main = new MainWindow(empId, fullName, role);
                    main.Show();
                    this.Close();
                }
                else
                {
                    txtError.Text = "Неверный логин, пароль или роль.";
                }
            }
        }
    }
}