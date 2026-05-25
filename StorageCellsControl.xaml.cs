using System.Data;
using System.Data.SqlClient;
using System.Windows.Controls;

namespace PVZManagerWPF
{
    public partial class StorageCellsControl : UserControl
    {
        private string connString;

        public StorageCellsControl()
        {
            InitializeComponent();
            connString = System.Configuration.ConfigurationManager.ConnectionStrings["WarehouseDB"].ConnectionString;
            LoadCells();
        }

        private void LoadCells()
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(connString))
            {
                SqlDataAdapter da = new SqlDataAdapter(
                    "SELECT cell_id, rack_number, shelf_number, cell_number, zone, status FROM StorageCells ORDER BY rack_number, shelf_number, cell_number", conn);
                da.Fill(dt);
            }
            dgvCells.ItemsSource = dt.DefaultView;
        }
    }
}