using quick_sql.Converter;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace quick_sql.Windows
{
    public partial class ViewSql : Window
    {
        public ViewSql(string server, string database, string sql, string highlight = "")
        {
            InitializeComponent();
            lblServer.Content = server.Replace("_", "__");
            lblDatabase.Content = database.Replace("_", "__");
            SqlFormatter.Format(sql, rtbSql, highlight);

            KeyDown += ViewSql_KeyDown;
        }

        private void ViewSql_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Close();
            }
        }

        private void btnCopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(new TextRange(rtbSql.Document.ContentStart, rtbSql.Document.ContentEnd).Text);
        }
    }
}
