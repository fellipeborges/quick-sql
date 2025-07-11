﻿using quick_sql.Command;
using quick_sql.Converter;
using quick_sql.Enum;
using quick_sql.Model;
using quick_sql.Service;
using quick_sql.Windows;
using System.Data;
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

        private async void PerformSearch()
        {
            if (!CheckMandatoryFields())
            {
                return;
            }
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                LoadingOverlay.Visibility = Visibility.Visible;
                RecentSaveValues();

                await Task.Delay(50); // To allow the UI to update before running the search
                await Task.Run(() =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (tabMain.SelectedItem == tabExpQueries) ExpensiveQuerySearch();
                        if (tabMain.SelectedItem == tabObjectSearch) ObjectSearchSearch();
                        if (tabMain.SelectedItem == tabIndexFragmentation) IndexFragmentationSearch();
                        if (tabMain.SelectedItem == tabTableInformation) TableInformationSearch();
                        if (tabMain.SelectedItem == tabJobMonitor) JobMonitorSearch();
                        if (tabMain.SelectedItem == tabQuery) QueryExecute();
                    });
                });

            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
            finally
            {
                Mouse.OverrideCursor = null;
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
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
        private void ExpensiveQuerySearch()
        {
            List<ExpensiveQuery> list = ExpensiveQueryService.Search(new ExpensiveQueryFilter
            {
                Server = cmbFilterServer.Text,
                Database = cmbFilterDatabase.Text,
                Host = cmbExpQueriesFilterHost.Text,
                Login = cmbExpQueriesFilterLogin.Text,
                Program = cmbExpQueriesFilterProgram.Text,
                BlockingOnly = chkExpQueriesFilterBlocking.IsChecked,
                Query = txtExpQueriesFilterQuery.Text
            });

            list.ForEach(item =>
            {
                item.KillSessionCommand = new RelayCommand<ExpensiveQuery>(param => ExpQueriesKillSession(param));
                item.GotoBlockerSessionCommand = new RelayCommand<ExpensiveQuery>(param => ExpQueriesGotoBlockerSession(param));
                item.QueryViewCommand = new RelayCommand<ExpensiveQuery>(param => ExpQueriesQueryView(param));
                item.QueryCopyCommand = new RelayCommand<ExpensiveQuery>(param => ExpQueriesQueryCopy(param));
            });

            gridExpQueries.ItemsSource = list;
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
                mustRefresh = mustRefresh || FillFilterAndQuery(columnName, "Host", cmbExpQueriesFilterHost, cellValue);
                mustRefresh = mustRefresh || FillFilterAndQuery(columnName, "Login", cmbExpQueriesFilterLogin, cellValue);
                mustRefresh = mustRefresh || FillFilterAndQuery(columnName, "Program", cmbExpQueriesFilterProgram, cellValue);

                if (columnName == "Query")
                {
                    ExpQueriesQueryView(gridExpQueries.SelectedItem);
                }

                if (mustRefresh)
                {
                    ExpensiveQuerySearch();
                }
            }

            static bool FillFilterAndQuery(string clickedColName, string targetColName, ComboBox comboBox, string cellValue)
            {
                if (clickedColName == targetColName)
                {
                    comboBox.Text = cellValue;
                    return true;
                }

                return false;
            }
        }

        #endregion ExpensiveQueries

        #region ObjectSearch
        private void ObjectSearchSearch()
        {
            List<ObjectSearch> list = ObjectSearchService.Search(new ObjectSearchFilter
            {
                Server = cmbFilterServer.Text,
                Database = cmbFilterDatabase.Text,
                Term = txtObjectSearchFilterTerm.Text,
                SearchInName = chkObjectSearchFilterSearchInName.IsChecked == true,
                SearchInCode = chkObjectSearchFilterSearchInCode.IsChecked == true
            });

            list.ForEach(item =>
            {
                item.CodeViewCommand = new RelayCommand<ObjectSearch>(param => ObjectSearchCodeView(param));
                item.CodeCopyCommand = new RelayCommand<ObjectSearch>(param => ObjectSearchCodeCopy(param));
            });

            gridObjectSearch.ItemsSource = list;
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
                    ObjectSearchSearch();
                }
            }
        }
        #endregion ObjectSearch

        #region IndexFragmentation
        private void IndexFragmentationSearch()
        {
            List<IndexFragmentation> list = IndexFragmentationService.Search(new IndexFragmentationFilter
            {
                Server = cmbFilterServer.Text,
                Database = cmbFilterDatabase.Text,
                Table = txtIndexFragmentationFilterTable.Text
            });

            list.ForEach(item =>
            {
                item.RebuildScriptViewCommand = new RelayCommand<IndexFragmentation>(param => IndexFragmentationViewCommand(param));
                item.RebuildScriptExecCommand = new RelayCommand<IndexFragmentation>(param => IndexFragmentationExecCommand(param));
                item.RebuildScriptCopyCommand = new RelayCommand<IndexFragmentation>(param => IndexFragmentationCopyCommand(param));
            });

            gridIndexFragmentation.ItemsSource = list;
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
                    JobMonitorSearch();
                }

                if (columnName == "Rebuild script")
                {
                    IndexFragmentationViewCommand(gridIndexFragmentation.SelectedItem);
                }

                if (columnName == "Table")
                {
                    txtIndexFragmentationFilterTable.Text = cellValue;
                    IndexFragmentationSearch();
                }
            }
        }
        #endregion IndexFragmentation

        #region TableInformation
        private void TableInformationSearch()
        {
            List<TableInformation> list = TableInformationService.Search(new TableInformationFilter
            {
                Server = cmbFilterServer.Text,
                Database = cmbFilterDatabase.Text,
                Table = txtTableInformationFilterTable.Text
            });

            gridTableInformation.ItemsSource = list;
        }

        private void btnTableInformationSearch_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }
        #endregion TableInformation

        #region JobMonitor
        private void JobMonitorSearch()
        {
            List<JobMonitor> list = JobMonitorService.Search(new JobMonitorFilter
            {
                Server = cmbFilterServer.Text,
                Database = cmbFilterDatabase.Text,
                Name = txtJobMonitorFilterName.Text,
                EnabledYes = chkJobMonitorFilterEnabledYes.IsChecked == true,
                EnabledNo = chkJobMonitorFilterEnabledNo.IsChecked == true
            });

            list.ForEach(item =>
            {
                item.StartJobCommand = new RelayCommand<JobMonitor>(param => JobMonitorStartJob(param));
                item.StopJobCommand = new RelayCommand<JobMonitor>(param => JobMonitorStopJob(param));
            });

            gridJobMonitor.ItemsSource = list;
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
                    JobMonitorSearch();
                }
            }
        }
        #endregion JobMonitor

        #region Query
        private void QueryExecute()
        {
            try
            {
                QueryInitialize();

                string code = QueryGetCode();
                if (string.IsNullOrWhiteSpace(code))
                {
                    return;
                }

                Query query = QueryService.Run(new QueryFilter
                {
                    Server = cmbFilterServer.Text,
                    Database = cmbFilterDatabase.Text,
                    Query = code
                });

                if (query.HasGridResult)
                {
                    gridQuery.ItemsSource = query.Result?.DefaultView;
                    gridQuery.Visibility = Visibility.Visible;
                    tabQueryResultResults.Visibility = Visibility.Visible;
                    tabQueryResult.SelectedItem = tabQueryResultResults;
                }

                txtQueryResultMessages.Text = query.Messages;
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
            RecentAddSelectedItemIfNotPresent(cmbExpQueriesFilterHost);
            RecentAddSelectedItemIfNotPresent(cmbExpQueriesFilterLogin);
            RecentAddSelectedItemIfNotPresent(cmbExpQueriesFilterProgram);

            recent.Items.AddRange(RecentGetItemsFromCombobox(cmbFilterServer));
            recent.Items.AddRange(RecentGetItemsFromCombobox(cmbFilterDatabase));
            recent.Items.AddRange(RecentGetItemsFromCombobox(cmbExpQueriesFilterHost));
            recent.Items.AddRange(RecentGetItemsFromCombobox(cmbExpQueriesFilterLogin));
            recent.Items.AddRange(RecentGetItemsFromCombobox(cmbExpQueriesFilterProgram));

            RecentService.Save(recent);
        }

        private void RecentLoadComboBoxes()
        {
            Recent recent = RecentService.Get();
            RecentLoadDropdown(cmbFilterServer, recent);
            RecentLoadDropdown(cmbFilterDatabase, recent);
            RecentLoadDropdown(cmbExpQueriesFilterHost, recent);
            RecentLoadDropdown(cmbExpQueriesFilterLogin, recent);
            RecentLoadDropdown(cmbExpQueriesFilterProgram, recent);
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
                var cb when cb == cmbExpQueriesFilterHost => RecentTypeEnum.Host,
                var cb when cb == cmbExpQueriesFilterLogin => RecentTypeEnum.Login,
                var cb when cb == cmbExpQueriesFilterProgram => RecentTypeEnum.Program,
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
            cmbExpQueriesFilterHost.KeyDown += Handle_KeyDownForSearch;
            cmbExpQueriesFilterLogin.KeyDown += Handle_KeyDownForSearch;
            cmbExpQueriesFilterProgram.KeyDown += Handle_KeyDownForSearch;
            txtExpQueriesFilterQuery.KeyDown += Handle_KeyDownForSearch;
            txtObjectSearchFilterTerm.KeyDown += Handle_KeyDownForSearch;
            txtIndexFragmentationFilterTable.KeyDown += Handle_KeyDownForSearch;
            txtTableInformationFilterTable.KeyDown += Handle_KeyDownForSearch;
            txtJobMonitorFilterName.KeyDown += Handle_KeyDownForSearch;

            // Preview Key Down
            cmbFilterServer.PreviewKeyDown += Handle_PreviewKeyDown;
            cmbFilterDatabase.PreviewKeyDown += Handle_PreviewKeyDown;
            cmbExpQueriesFilterHost.PreviewKeyDown += Handle_PreviewKeyDown;
            cmbExpQueriesFilterLogin.PreviewKeyDown += Handle_PreviewKeyDown;
            cmbExpQueriesFilterProgram.PreviewKeyDown += Handle_PreviewKeyDown;
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
        #endregion Events
    }
}