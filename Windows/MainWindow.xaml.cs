using quick_sql.Command;
using quick_sql.Converter;
using quick_sql.Enum;
using quick_sql.Model;
using quick_sql.Service;
using quick_sql.Windows;
using System.Data;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace quick_sql
{
    public partial class MainWindow : Window
    {
        private List<CodeSnippet> CodeSnippetList = [];
        private CodeSnippet? CodeSnippetCurrentSelected;

        public MainWindow()
        {
            InitializeComponent();
            ShowAppVersionOnTitle();
            RecentLoadComboBoxes();
            RegisterComponentEvents();
            CodeSnippetLoadAll();
        }

        private void ShowAppVersionOnTitle() => Title = $"Quick SQL - {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "Unknown Version"}";

        private static void ShowWarningMessage(string message) => MessageBox.Show(message, "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);

        private static void ShowErrorMessage(Exception ex) => MessageBox.Show(ex.Message, "Attention", MessageBoxButton.OK, MessageBoxImage.Error);

        private CancellationTokenSource? CancellationTokenSource;

        private async void PerformSearch()
        {
            if (!CheckMandatoryFields())
            {
                return;
            }

            CancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = CancellationTokenSource.Token;
            var stopwatch = Stopwatch.StartNew();
            LoadingOverlay.Visibility = Visibility.Visible;
            RecentSaveValues();

            try
            {
                if (tabMain.SelectedItem == tabExpQueries) await ExpensiveQuerySearch(cancellationToken);
                if (tabMain.SelectedItem == tabObjectSearch) await ObjectSearchSearch(cancellationToken);
                if (tabMain.SelectedItem == tabIndexFragmentation) await IndexFragmentationSearch(cancellationToken);
                if (tabMain.SelectedItem == tabTableInformation) await TableInformationSearch(cancellationToken);
                if (tabMain.SelectedItem == tabJobMonitor) await JobMonitorSearch(cancellationToken);
                if (tabMain.SelectedItem == tabQuery) await QueryExecute(cancellationToken);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                if (!cancellationToken.IsCancellationRequested)
                {
                    TabFooterFill(stopwatch.Elapsed);
                }
                CancellationTokenSource = null;
            }
        }

        private void TabFooterFill(TimeSpan elapsed)
        {
            Label? lbl = tabMain.SelectedItem switch
            {
                TabItem item when item == tabExpQueries => lblExpQueriesFooter,
                TabItem item when item == tabObjectSearch => lblObjectSearchFooter,
                TabItem item when item == tabIndexFragmentation => lblIndexFragmentationFooter,
                TabItem item when item == tabTableInformation => lblTableInformationFooter,
                TabItem item when item == tabJobMonitor => lblJobMonitorFooter,
                TabItem item when item == tabQuery => lblQueryFooter,
                _ => null
            };

            if (lbl != null)
            {
                string content = "";
                content += $"{cmbFilterServer.Text}   |   ";
                content += $"{string.Format("{0:00}:{1:00}:{2:00}", elapsed.Hours, elapsed.Minutes, elapsed.Seconds)}   |   ";
                content += $"{TabFooterGetRowCount()} rows";

                lbl.Content = content;
            }
        }

        private int TabFooterGetRowCount()
        {
            DataGrid? grid = tabMain.SelectedItem switch
            {
                TabItem item when item == tabExpQueries => gridExpQueries,
                TabItem item when item == tabObjectSearch => gridObjectSearch,
                TabItem item when item == tabIndexFragmentation => gridIndexFragmentation,
                TabItem item when item == tabTableInformation => gridTableInformation,
                TabItem item when item == tabJobMonitor => gridJobMonitor,
                TabItem item when item == tabQuery => gridQuery,
                _ => null
            };

            return grid?.ItemsSource != null ? ((System.Collections.ICollection)grid.ItemsSource).Count : 0;
        }

        private bool CheckMandatoryFields()
        {
            if (string.IsNullOrWhiteSpace(cmbFilterServer.Text))
            {
                ShowWarningMessage("Please inform the Server.");
                cmbFilterServer.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(cmbFilterDatabase.Text) && (tabMain.SelectedItem == tabIndexFragmentation || tabMain.SelectedItem == tabTableInformation))
            {
                ShowWarningMessage("Please inform the Database.");
                cmbFilterDatabase.Focus();
                return false;
            }

            return true;
        }

        private void OpenViewSqlWindow(string server, string database, string sql, string highlight = "")
        {
            ViewSql viewSqlWindow = new(server, database, sql, highlight)
            {
                Width = Width * 0.85,
                Height = Height * 0.75,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            viewSqlWindow.ShowDialog();
        }

        private static (string ColumnName, string CellValue, bool AnySelected) GetSelectedCellInfo(DataGrid dataGrid)
        {
            var selectedCell = dataGrid.CurrentCell;
            if (selectedCell.Column != null && selectedCell.Item != null)
            {
                string columnName = selectedCell.Column.Header.ToString() ?? string.Empty;
                string cellValue = string.Empty;
                if (selectedCell.Column.GetCellContent(selectedCell.Item) is TextBlock textBlock)
                {
                    cellValue = textBlock.Text;
                }
                return (columnName, cellValue, true);
            }

            return (string.Empty, string.Empty, false);
        }

        #region CodeSnippet
        private void CodeSnippetLoadAll(string selectByName = "")
        {
            try
            {
                CodeSnippetClearSelected();
                CodeSnippetList = [.. CodeSnippetService.Get().OrderBy(x => x.Name)];
                lsvCodeSnippets.ItemsSource = CodeSnippetList;
                if (!string.IsNullOrWhiteSpace(selectByName))
                {
                    var itemToSelect = CodeSnippetList.FirstOrDefault(x => x.Name.Equals(selectByName, StringComparison.InvariantCultureIgnoreCase));
                    if (itemToSelect != null)
                    {
                        lsvCodeSnippets.SelectedItem = itemToSelect;
                        lsvCodeSnippets.ScrollIntoView(itemToSelect);
                        lsvCodeSnippets.Focus();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
        }

        private void CodeSnippetSaveAll()
        {
            try
            {
                CodeSnippetService.Save(CodeSnippetList);
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
        }

        private void CodeSnippetLiveSearch(string term)
        {
            CodeSnippetClearSelected();
            List<CodeSnippet> list =
                string.IsNullOrWhiteSpace(term) ?
                    CodeSnippetList :
                    [.. CodeSnippetList.Where(x => x.Name.Contains(term, StringComparison.InvariantCultureIgnoreCase) || x.Code.Contains(term, StringComparison.InvariantCultureIgnoreCase))];

            lsvCodeSnippets.ItemsSource = list;
        }

        private void CodeSnippetClearSelected()
        {
            CodeSnippetCurrentSelected = null;
            txtCodeSnippetName.Text = string.Empty;
            rtbCodeSnippetCode.Document.Blocks.Clear();
        }

        private string CodeSnippetGetCode()
        {
            return new TextRange(rtbCodeSnippetCode.Document.ContentStart, rtbCodeSnippetCode.Document.ContentEnd).Text.Trim();
        }

        private void CodeSnippetShow(CodeSnippet codeSnippet)
        {
            CodeSnippetCurrentSelected = codeSnippet;

            txtCodeSnippetName.Text = CodeSnippetCurrentSelected.Name;
            rtbCodeSnippetCode.Document.Blocks.Clear();
            rtbCodeSnippetCode.CaretPosition.InsertTextInRun(CodeSnippetCurrentSelected.Code);
            SqlFormatter.Format(CodeSnippetGetCode(), rtbCodeSnippetCode);
        }

        private void CodeSnippetShowSelectedFromListView()
        {
            if (lsvCodeSnippets.SelectedItem is CodeSnippet selectedSnippet)
            {
                CodeSnippetShow(selectedSnippet);
            }
        }

        private void lsvCodeSnippets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CodeSnippetShowSelectedFromListView();
        }

        private void lsvCodeSnippets_MouseUp(object sender, MouseButtonEventArgs e)
        {
            CodeSnippetShowSelectedFromListView();
        }

        private void txtCodeSnippetSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            CodeSnippetLiveSearch(txtCodeSnippetSearch.Text.Trim());
        }

        private void txtCodeSnippetSearch_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                e.Handled = true;
                lsvCodeSnippets.Focus();
            }
        }

        private void btnCodeSnippetCopy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(CodeSnippetGetCode());
        }

        private void btnCodeSnippetAdd_Click(object sender, RoutedEventArgs e)
        {
            CodeSnippetClearSelected();
            txtCodeSnippetName.Focus();
            lsvCodeSnippets.SelectedIndex = -1;
        }

        private void btnCodeSnippetSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCodeSnippetName.Text.Trim()))
            {
                ShowWarningMessage("Please inform the code snippet name.");
                txtCodeSnippetName.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(CodeSnippetGetCode()))
            {
                ShowWarningMessage("Please inform the code snippet code.");
                rtbCodeSnippetCode.Focus();
                return;
            }

            bool isNew = CodeSnippetCurrentSelected == null;
            CodeSnippetCurrentSelected ??= new CodeSnippet();
            CodeSnippetCurrentSelected.Name = txtCodeSnippetName.Text.Trim();
            CodeSnippetCurrentSelected.Code = CodeSnippetGetCode();

            CodeSnippet? existingCodeSnippet = CodeSnippetList.FirstOrDefault(c => c.Name.Equals(CodeSnippetCurrentSelected.Name, StringComparison.InvariantCultureIgnoreCase));
            if (existingCodeSnippet != null)
            {
                if (isNew)
                {
                    ShowWarningMessage("There is already a code snippet with this name.");
                    txtCodeSnippetName.Focus();
                    return;
                }

                CodeSnippetList.Remove(existingCodeSnippet);
            }

            CodeSnippetList.Add(CodeSnippetCurrentSelected);
            CodeSnippetSaveAll();
            CodeSnippetLoadAll(CodeSnippetCurrentSelected.Name);
        }

        private void btnCodeSnippetDelete_Click(object sender, RoutedEventArgs e)
        {
            if (CodeSnippetCurrentSelected == null)
            {
                return;
            }

            if (MessageBox.Show($"Are you sure you want to delete Code Snippet '{CodeSnippetCurrentSelected.Name}'?", "Attention", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                CodeSnippetList.Remove(CodeSnippetCurrentSelected);
                CodeSnippetSaveAll();
                CodeSnippetLoadAll();
            }
        }

        #endregion CodeSnippet

        #region ExpensiveQueries
        private async Task ExpensiveQuerySearch(CancellationToken cancellationToken)
        {
            var filter = new ExpensiveQueryFilter
            {
                Server = cmbFilterServer.Text,
                Database = cmbFilterDatabase.Text,
                Host = txtExpQueriesFilterHost.Text,
                Login = txtExpQueriesFilterLogin.Text,
                Program = txtExpQueriesFilterProgram.Text,
                BlockingOnly = chkExpQueriesFilterBlocking.IsChecked,
                Query = txtExpQueriesFilterQuery.Text
            };

            List<ExpensiveQuery> list = await ExpensiveQueryService.SearchAsync(filter, cancellationToken);
            if (!cancellationToken.IsCancellationRequested)
            {
                list.ForEach(item =>
                {
                    item.KillSessionCommand = new RelayCommand<ExpensiveQuery>(param => ExpQueriesKillSession(param));
                    item.GotoBlockerSessionCommand = new RelayCommand<ExpensiveQuery>(param => ExpQueriesGotoBlockerSession(param));
                    item.QueryViewCommand = new RelayCommand<ExpensiveQuery>(param => ExpQueriesQueryView(param));
                    item.QueryCopyCommand = new RelayCommand<ExpensiveQuery>(param => ExpQueriesQueryCopy(param));
                });

                gridExpQueries.ItemsSource = list;
            }
        }

        private void ExpQueriesKillSession(object parameter)
        {
            if (parameter is ExpensiveQuery item)
            {
                if (MessageBox.Show($"Are you sure you want to kill session {item.SPID}?", "Attention", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        Mouse.OverrideCursor = Cursors.Wait;
                        ExpensiveQueryService.KillSession(cmbFilterServer.Text, item.SPID);
                        MessageBox.Show("Command run successfully.", "Attention", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        ShowErrorMessage(ex);
                    }
                    finally
                    {
                        Mouse.OverrideCursor = null;
                    }
                }
            }
        }

        private void ExpQueriesGotoBlockerSession(object parameter)
        {
            if (parameter is ExpensiveQuery item)
            {
                if (gridExpQueries.ItemsSource is not IEnumerable<ExpensiveQuery> gridItems)
                    return;

                ExpensiveQuery? recordToSelect = gridItems.FirstOrDefault(x => x.SPID.ToString() == item.BlockedBy);
                if (recordToSelect != null)
                {
                    gridExpQueries.SelectedItem = recordToSelect;
                    gridExpQueries.Focus();
                    gridExpQueries.ScrollIntoView(recordToSelect);
                }
            }
        }

        private void ExpQueriesQueryView(object parameter)
        {
            if (parameter is ExpensiveQuery item)
            {
                OpenViewSqlWindow(cmbFilterServer.Text, item.Database, item.Query);
            }
        }

        private static void ExpQueriesQueryCopy(object parameter)
        {
            if (parameter is ExpensiveQuery item)
            {
                Clipboard.SetText(item.Query);
            }
        }

        private void btnExpQueriesSearch_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        private void gridExpQueries_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var (columnName, cellValue, anySelected) = GetSelectedCellInfo(gridExpQueries);
            if (anySelected)
            {
                bool mustRefresh = false;

                mustRefresh = mustRefresh || FillFilterAndQuery(columnName, "Database", cmbFilterDatabase, cellValue);
                mustRefresh = mustRefresh || FillFilterAndQuery(columnName, "Host", txtExpQueriesFilterHost, cellValue);
                mustRefresh = mustRefresh || FillFilterAndQuery(columnName, "Login", txtExpQueriesFilterLogin, cellValue);
                mustRefresh = mustRefresh || FillFilterAndQuery(columnName, "Program", txtExpQueriesFilterProgram, cellValue);

                if (columnName == "Query")
                {
                    ExpQueriesQueryView(gridExpQueries.SelectedItem);
                }

                if (mustRefresh)
                {
                    PerformSearch();
                }
            }

            static bool FillFilterAndQuery(string clickedColName, string targetColName, Control control, string cellValue)
            {
                if (clickedColName == targetColName && control is ComboBox comboBox)
                {
                    comboBox.Text = cellValue;
                    return true;
                }

                if (clickedColName == targetColName && control is TextBox textBox)
                {
                    textBox.Text = cellValue;
                    return true;
                }

                return false;
            }
        }

        #endregion ExpensiveQueries

        #region ObjectSearch
        private async Task ObjectSearchSearch(CancellationToken cancellationToken)
        {
            var filter = new ObjectSearchFilter
            {
                Server = cmbFilterServer.Text,
                Database = cmbFilterDatabase.Text,
                Term = txtObjectSearchFilterTerm.Text,
                SearchInName = chkObjectSearchFilterSearchInName.IsChecked == true,
                SearchInCode = chkObjectSearchFilterSearchInCode.IsChecked == true
            };

            List<ObjectSearch> list = await ObjectSearchService.SearchAsync(filter, cancellationToken);
            if (!cancellationToken.IsCancellationRequested)
            {
                list.ForEach(item =>
                {
                    item.CodeViewCommand = new RelayCommand<ObjectSearch>(param => ObjectSearchCodeView(param));
                    item.CodeCopyCommand = new RelayCommand<ObjectSearch>(param => ObjectSearchCodeCopy(param));
                });

                gridObjectSearch.ItemsSource = list;
            }
        }

        private void ObjectSearchCodeView(object parameter)
        {
            if (parameter is ObjectSearch item)
            {
                OpenViewSqlWindow(cmbFilterServer.Text, item.Database, item.Code, txtObjectSearchFilterTerm.Text);
            }
        }

        private static void ObjectSearchCodeCopy(object parameter)
        {
            if (parameter is ObjectSearch item)
            {
                Clipboard.SetText(item.Code);
            }
        }

        private void btnObjectSearchSearch_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        private void gridObjectSearch_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var (columnName, cellValue, anySelected) = GetSelectedCellInfo(gridObjectSearch);
            if (anySelected)
            {
                if (columnName == "Code")
                {
                    ObjectSearchCodeView(gridObjectSearch.SelectedItem);
                }

                if (columnName == "Database" && !string.IsNullOrWhiteSpace(cellValue))
                {
                    cmbFilterDatabase.Text = cellValue;
                    PerformSearch();
                }
            }
        }
        #endregion ObjectSearch

        #region IndexFragmentation
        private async Task IndexFragmentationSearch(CancellationToken cancellationToken)
        {
            var filter = new IndexFragmentationFilter
            {
                Server = cmbFilterServer.Text,
                Database = cmbFilterDatabase.Text,
                Table = txtIndexFragmentationFilterTable.Text
            };

            List<IndexFragmentation> list = await IndexFragmentationService.SearchAsync(filter, cancellationToken);
            if (!cancellationToken.IsCancellationRequested)
            {
                list.ForEach(item =>
                {
                    item.RebuildScriptViewCommand = new RelayCommand<IndexFragmentation>(param => IndexFragmentationViewCommand(param));
                    item.RebuildScriptExecCommand = new RelayCommand<IndexFragmentation>(param => IndexFragmentationExecCommand(param));
                    item.RebuildScriptCopyCommand = new RelayCommand<IndexFragmentation>(param => IndexFragmentationCopyCommand(param));
                });

                gridIndexFragmentation.ItemsSource = list;
            }
        }

        private void IndexFragmentationViewCommand(object parameter)
        {
            if (parameter is IndexFragmentation item)
            {
                OpenViewSqlWindow(cmbFilterServer.Text, cmbFilterDatabase.Text, item.RebuildScript);
            }
        }

        private void IndexFragmentationExecCommand(object parameter)
        {
            if (parameter is IndexFragmentation item)
            {
                if (MessageBox.Show($"Are you sure you want to execute the rebuild script for index '{item.Index}' on table '{item.Table}'?", "Attention", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        Mouse.OverrideCursor = Cursors.Wait;
                        IndexFragmentationService.ExecuteRebuildCommand(cmbFilterServer.Text, cmbFilterDatabase.Text, item.RebuildScript);
                        MessageBox.Show("Command run successfully.", "Attention", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        ShowErrorMessage(ex);
                    }
                    finally
                    {
                        Mouse.OverrideCursor = null;
                    }
                }
            }
        }

        private static void IndexFragmentationCopyCommand(object parameter)
        {
            if (parameter is IndexFragmentation item)
            {
                Clipboard.SetText(item.RebuildScript);
            }
        }

        private void btnIndexFragmentationSearch_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        private void gridIndexFragmentation_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var (columnName, cellValue, anySelected) = GetSelectedCellInfo(gridIndexFragmentation);
            if (anySelected)
            {
                if (columnName == "Name" && !string.IsNullOrWhiteSpace(cellValue))
                {
                    txtJobMonitorFilterName.Text = cellValue;
                    PerformSearch();
                }

                if (columnName == "Rebuild script")
                {
                    IndexFragmentationViewCommand(gridIndexFragmentation.SelectedItem);
                }

                if (columnName == "Table")
                {
                    txtIndexFragmentationFilterTable.Text = cellValue;
                    PerformSearch();
                }
            }
        }
        #endregion IndexFragmentation

        #region TableInformation
        private async Task TableInformationSearch(CancellationToken cancellationToken)
        {
            var filter = new TableInformationFilter
            {
                Server = cmbFilterServer.Text,
                Database = cmbFilterDatabase.Text,
                Table = txtTableInformationFilterTable.Text
            };

            List<TableInformation> list = await TableInformationService.SearchAsync(filter, cancellationToken);
            if (!cancellationToken.IsCancellationRequested)
            {
                gridTableInformation.ItemsSource = list;
            }
        }

        private void btnTableInformationSearch_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }
        #endregion TableInformation

        #region JobMonitor
        private async Task JobMonitorSearch(CancellationToken cancellationToken)
        {
            var filter = new JobMonitorFilter
            {
                Server = cmbFilterServer.Text,
                Database = cmbFilterDatabase.Text,
                Name = txtJobMonitorFilterName.Text,
                EnabledYes = chkJobMonitorFilterEnabledYes.IsChecked == true,
                EnabledNo = chkJobMonitorFilterEnabledNo.IsChecked == true
            };

            List<JobMonitor> list = await JobMonitorService.SearchAsync(filter, cancellationToken);
            if (!cancellationToken.IsCancellationRequested)
            {
                list.ForEach(item =>
                {
                    item.StartJobCommand = new RelayCommand<JobMonitor>(param => JobMonitorStartJob(param));
                    item.StopJobCommand = new RelayCommand<JobMonitor>(param => JobMonitorStopJob(param));
                });

                gridJobMonitor.ItemsSource = list;
            }
        }

        private void JobMonitorStartJob(object parameter)
        {
            if (parameter is JobMonitor item)
            {
                if (MessageBox.Show($"Are you sure you want to start the job '{item.Name}'?", "Attention", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        Mouse.OverrideCursor = Cursors.Wait;
                        JobMonitorService.StartJob(cmbFilterServer.Text, item.Id);
                        MessageBox.Show("Job started successfully.", "Attention", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        ShowErrorMessage(ex);
                    }
                    finally
                    {
                        Mouse.OverrideCursor = null;
                    }
                }
            }
        }

        private void JobMonitorStopJob(object parameter)
        {
            if (parameter is JobMonitor item)
            {
                if (MessageBox.Show($"Are you sure you want to stop the job '{item.Name}'?", "Attention", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        Mouse.OverrideCursor = Cursors.Wait;
                        JobMonitorService.StopJob(cmbFilterServer.Text, item.Id);
                        MessageBox.Show("Job stopped successfully.", "Attention", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        ShowErrorMessage(ex);
                    }
                    finally
                    {
                        Mouse.OverrideCursor = null;
                    }
                }
            }
        }

        private void btnJobMonitorSearch_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        private void gridJobMonitor_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var (columnName, cellValue, anySelected) = GetSelectedCellInfo(gridJobMonitor);
            if (anySelected)
            {
                if (columnName == "Name" && !string.IsNullOrWhiteSpace(cellValue))
                {
                    txtJobMonitorFilterName.Text = cellValue;
                    PerformSearch();
                }
            }
        }
        #endregion JobMonitor

        #region Query
        private async Task QueryExecute(CancellationToken cancellationToken)
        {
            try
            {
                QueryInitialize();

                string code = QueryGetCode();
                if (string.IsNullOrWhiteSpace(code))
                {
                    return;
                }

                var filter = new QueryFilter
                {
                    Server = cmbFilterServer.Text,
                    Database = cmbFilterDatabase.Text,
                    Query = code
                };

                Query query = await QueryService.Run(filter, cancellationToken);
                if (!cancellationToken.IsCancellationRequested)
                {
                    if (query.HasGridResult)
                    {
                        gridQuery.ItemsSource = query.Result?.DefaultView;
                        gridQuery.Visibility = Visibility.Visible;
                        tabQueryResultResults.Visibility = Visibility.Visible;
                        tabQueryResult.SelectedItem = tabQueryResultResults;
                    }

                    txtQueryResultMessages.Text = query.Messages;
                }
                else
                {
                    txtQueryResultMessages.Text = "Query was canceled by user.";
                }
            }
            catch (Exception ex)
            {
                txtQueryResultMessages.Foreground = Brushes.Red;
                txtQueryResultMessages.Text = ex.Message;
                tabQueryResultResults.Visibility = Visibility.Collapsed;
            }
        }

        private string QueryGetCode()
        {
            string code = rtbQuery.Selection.Text;
            if (string.IsNullOrWhiteSpace(code))
            {
                code = new TextRange(rtbQuery.Document.ContentStart, rtbQuery.Document.ContentEnd).Text.Trim();
            }

            return code;
        }

        private void QueryInitialize()
        {
            tabQueryResult.SelectedItem = tabQueryResultMessages;
            txtQueryResultMessages.Text = string.Empty;
            txtQueryResultMessages.Foreground = Brushes.Black;
            tabQueryResultResults.Visibility = Visibility.Collapsed;
            gridQuery.Visibility = Visibility.Collapsed;
            gridQuery.ItemsSource = null;
        }

        private void btnQueryExecute_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        private void rtbQuery_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Shift)
            {
                e.Handled = true;
                PerformSearch();
            }
        }
        #endregion Query

        #region Recent
        private void RecentSaveValues()
        {
            var recent = new Recent
            {
                Items = []
            };

            RecentAddSelectedItemIfNotPresent(cmbFilterServer);
            RecentAddSelectedItemIfNotPresent(cmbFilterDatabase);

            recent.Items.AddRange(RecentGetItemsFromCombobox(cmbFilterServer));
            recent.Items.AddRange(RecentGetItemsFromCombobox(cmbFilterDatabase));

            RecentService.Save(recent);
        }

        private void RecentLoadComboBoxes()
        {
            Recent recent = RecentService.Get();
            RecentLoadDropdown(cmbFilterServer, recent);
            RecentLoadDropdown(cmbFilterDatabase, recent);
        }

        private List<RecentItem> RecentGetItemsFromCombobox(ComboBox comboBox)
        {
            var recentList = new List<RecentItem>();
            foreach (var item in comboBox.Items.Cast<string>().ToList())
            {
                recentList.Add(new RecentItem
                {
                    Type = RecentGetTypeByCombobox(comboBox),
                    Value = item,
                    LastUsed = item.Equals(comboBox.Text, StringComparison.InvariantCultureIgnoreCase)
                });
            }

            return recentList;
        }

        private static void RecentAddSelectedItemIfNotPresent(ComboBox comboBox)
        {
            if (!string.IsNullOrWhiteSpace(comboBox.Text) && !comboBox.Items.Contains(comboBox.Text))
            {
                comboBox.Items.Add(comboBox.Text);
            }
        }

        private RecentTypeEnum RecentGetTypeByCombobox(ComboBox comboBox) =>
            comboBox switch
            {
                var cb when cb == cmbFilterServer => RecentTypeEnum.Server,
                var cb when cb == cmbFilterDatabase => RecentTypeEnum.Database,
                _ => RecentTypeEnum.None
            };

        private void RecentLoadDropdown(ComboBox comboBox, Recent recent)
        {
            List<RecentItem> recentItems = [.. recent.Items.Where(r => r.Type == RecentGetTypeByCombobox(comboBox))];
            comboBox.Items.Clear();
            recentItems.Select(r => r.Value).OrderBy(r => r).ToList().ForEach(item => comboBox.Items.Add(item));
            comboBox.SelectedItem = recentItems.Where(r => r.LastUsed == true).Select(r => r.Value).FirstOrDefault();
        }
        #endregion Recent

        #region Events
        private void RegisterComponentEvents()
        {
            // Key Down
            cmbFilterServer.KeyDown += Handle_KeyDownForSearch;
            cmbFilterDatabase.KeyDown += Handle_KeyDownForSearch;
            txtExpQueriesFilterHost.KeyDown += Handle_KeyDownForSearch;
            txtExpQueriesFilterLogin.KeyDown += Handle_KeyDownForSearch;
            txtExpQueriesFilterProgram.KeyDown += Handle_KeyDownForSearch;
            txtExpQueriesFilterQuery.KeyDown += Handle_KeyDownForSearch;
            txtObjectSearchFilterTerm.KeyDown += Handle_KeyDownForSearch;
            txtIndexFragmentationFilterTable.KeyDown += Handle_KeyDownForSearch;
            txtTableInformationFilterTable.KeyDown += Handle_KeyDownForSearch;
            txtJobMonitorFilterName.KeyDown += Handle_KeyDownForSearch;

            // Preview Key Down
            cmbFilterServer.PreviewKeyDown += Handle_PreviewKeyDown;
            cmbFilterDatabase.PreviewKeyDown += Handle_PreviewKeyDown;
            txtExpQueriesFilterHost.PreviewKeyDown += Handle_PreviewKeyDown;
            txtExpQueriesFilterLogin.PreviewKeyDown += Handle_PreviewKeyDown;
            txtExpQueriesFilterProgram.PreviewKeyDown += Handle_PreviewKeyDown;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                e.Handled = true;
                PerformSearch();
            }
        }

        private void Handle_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (sender is ComboBox cmb)
                {
                    cmb.Items.Remove(cmb.SelectedItem);
                    cmb.Text = string.Empty;
                    e.Handled = true;
                    RecentSaveValues();
                }
            }
        }

        private void Handle_KeyDownForSearch(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                PerformSearch();
            }
        }

        private void tabMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl tabControl)
            {
                if (tabControl.SelectedItem is TabItem selectedTab)
                {
                    bool isDatabaseFilterEnabled =
                        (selectedTab == tabExpQueries) ||
                        (selectedTab == tabObjectSearch) ||
                        (selectedTab == tabIndexFragmentation) ||
                        (selectedTab == tabTableInformation) ||
                        (selectedTab == tabQuery);

                    bool isDatabaseMandatory = (selectedTab == tabIndexFragmentation) || (selectedTab == tabTableInformation);

                    cmbFilterDatabase.IsEnabled = isDatabaseFilterEnabled;
                    lblFilterDatabase.Foreground = isDatabaseFilterEnabled ? Brushes.Black : Brushes.Gray;
                    lblFilterDatabase.Content = isDatabaseMandatory ? "Database*" : "Database";
                }
            }
        }
        private void lblLoadingCancel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            CancellationTokenSource?.Cancel();
        }
        #endregion Events
    }
}