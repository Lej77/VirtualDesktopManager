using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsDesktop;

using System.IO;

namespace VirtualDesktopManager
{
    public partial class ConfigureForm : Form
    {
        #region Classes

        #endregion Classes


        #region Member Variables

        private WindowInfo.Data listData = null;
        private ListViewItem[] listItems = new ListViewItem[0];

        private List<Filter> filterListReference = new List<Filter>();
        private Filter[] filterData = new Filter[0];
        private ListViewItem[] filterItems = new ListViewItem[0];

        private Filter loadedFilter = new Filter();
        private bool updatingFilterConfigurationData = true;

        Utils.ListViewHelper listHelper = null;

        private ListViewGroup activeWindowGroup = new ListViewGroup("Active Windows");
        private ListViewGroup filterGroup = new ListViewGroup("Filters");

        private readonly WindowInfo.Holder updater = null;
        private VirtualDesktopApplicationContext appContext = null;

        #endregion Member Variables


        #region Constructors

        public ConfigureForm() : this(null) { }
        internal ConfigureForm(VirtualDesktopApplicationContext context)
        {
            InitializeComponent();

            Icon = Properties.Resources.edges___transparent_with_white;

            this.appContext = context;

            // Configure ListView:            
            listView1.Groups.AddRange(new ListViewGroup[] { activeWindowGroup, filterGroup });
            listHelper = new Utils.ListViewHelper(listView1)
            {
                AllowDisablingSortingOnColumnHeaderClick = false,
                ChangeSortingOnColumnHeaderClick = true,
                ColumnMinWidthsEnabled = true,
                IndexOfSortingColumn = 0,
                SortingEnabled = true,
                IsSortingInverted = false,
            };
            listHelper.ColumnMinWidths = listHelper.ColumnHeaderWidths;


            // Get filters:
            if (context != null)
            {
                filterListReference = context.FilterListReference;
                if (filterListReference.Count > 0)
                    loadedFilter = filterListReference.ElementAt(0);
            }

            // Hook into updater:
            updater = context != null && context.WindowsInfoHolder != null ? context.WindowsInfoHolder : new WindowInfo.Holder(true);
            updater.InfoUpdated += updater_Update;
            FormClosed += (sender, args) => { updater.InfoUpdated -= updater_Update; };

            // Update list data:
            updater.Invalidate();
            LoadFilterData(loadedFilter);
            UpdateList(updater.LatestInfo);

            // Set window info from settings:
            this.Load += (sender, e) =>
            {
                var rectangle = context.Data.ConfigurationWindowLocation;
                if (rectangle.Size.Width > 0 && rectangle.Size.Height > 0)
                    DesktopBounds = rectangle;
                if (context.Data.ConfigurationWindowMaximized)
                    this.WindowState = FormWindowState.Maximized;

                SaveDataUpdated();
            };
            this.ResizeEnd += (sender, e) =>
            {
                if (WindowState != FormWindowState.Minimized)
                    context.Data.ConfigurationWindowMaximized = WindowState == FormWindowState.Maximized;
                context.Data.ConfigurationWindowLocation = DesktopBounds;
                context.InvalidateSavedSettings();
            };

        }

        #endregion Constructors


        #region Methods


        public void SaveDataUpdated()
        {
            chbSmoothCurrentDesktopSwitch.Checked = appContext.Data.SmoothDesktopSwitching;
            chbPreventFlashingWindows.Checked = appContext.Data.PreventFlashingWindows;
            chbStartWithAdminRights.Checked = appContext.Data.StartWithAdminRights;
        }

        private void UpdateList(WindowInfo.Data newData = null)
        {
            var currentSelectedWindows = SelectedWindowHandles;
            var currentSelectedFilters = SelectedFilters;
            listView1.Items.Clear();

            if (newData == null)
            {
                newData = listData;
            }

            if (newData != null)
            {
                List<ListViewItem> windowItems = new List<ListViewItem>();

                foreach (var window in newData.WindowInfo)
                {
                    int desktopIndex = newData.DetermineDesktopIndex(window.Desktop);

                    int parentIndex = -1;
                    int rootParentIndex = -1;
                    for (int iii = 0; iii < newData.WindowInfo.Length; iii++)
                    {
                        var parWin = newData.WindowInfo[iii];
                        if (parWin.WindowHandle == window.ParentWindowHandle)
                        {
                            parentIndex = iii;
                        }
                        if (parWin.WindowHandle == window.RootParentWindowHandle)
                        {
                            rootParentIndex = iii;
                        }
                    }

                    Filter[] filters = Filter.GetApplicableFilters(window, newData, filterListReference.ToArray());
                    Filter.DesktopTarget target = Filter.GetFirstDesktopTarget(filters) ?? new Filter.DesktopTarget();

                    windowItems.Add(new ListViewItem(new string[] {
                        (windowItems.Count + 1).ToString(),
                        desktopIndex >= 0 ? (desktopIndex + 1).ToString() : "",
                        window.Title,
                        window.Process != null ? window.Process.ProcessName : "",
                        newData.MainWindows.Contains(window.WindowHandle).ToString(),
                        parentIndex < 0 ? "" : (parentIndex + 1).ToString(),
                        rootParentIndex < 0 ? "" : (rootParentIndex + 1).ToString(),
                        Filter.FormatIntArray(Filter.GetFilterIndices(filters, filterListReference.ToArray())),
                        target.shouldPin ? "Pin" : (target.targetDesktopIndex >= 0 ? (target.targetDesktopIndex + 1).ToString() : ""),
                    })
                    {
                        Tag = window.WindowHandle,
                        Group = activeWindowGroup,
                    });
                }

                // Update globals:
                listData = newData;
                listItems = windowItems.ToArray();
            }

            {
                // Update filter data:

                List<ListViewItem> filterItems = new List<ListViewItem>();
                foreach (Filter filter in filterListReference)
                {
                    Filter.Data data = filter.FilterData;


                    string otherFilters = Filter.FormatIntArray(Filter.GetFilterIndices(data.references.filters, filterListReference.ToArray()));

                    filterItems.Add(new ListViewItem(new string[] {
                        Filter.FormatIntervall(data.indexLowerBound + 1, data.indexUpperBound + 1),
                        Filter.FormatIntervall(data.desktopLowerBound + 1, data.desktopUpperBound + 1),
                        "\"" + Utils.TextManager.CombineStringCollection(data.title, "\"*\"") + "\"",
                        "\"" + Utils.TextManager.CombineStringCollection(data.process, "\"*\"") + "\"",
                        data.checkIfMainWindow ? data.isMainProcessWindow.ToString() : "",
                        Filter.FormatIntArray(Filter.GetFilterIndices(data.references.parentFilters, filterListReference.ToArray())),
                        Filter.FormatIntArray(Filter.GetFilterIndices(data.references.rootParentFilters, filterListReference.ToArray())),
                        (filterItems.Count + 1).ToString() + (otherFilters == "" ? "" : " - " + otherFilters),
                        (data.desktopTargetAdv.shouldPin ? "Pin" : (data.desktopTargetAdv.targetDesktopIndex + 1).ToString()),
                    })
                    {
                        Tag = filter,
                        Group = filterGroup,
                    });
                }

                // Update globals:
                this.filterItems = filterItems.ToArray();
                filterData = filterListReference.ToArray();

                appContext.InvalidateSavedSettings();
            }


            // Get from globals:
            List<ListViewItem> items = listItems.ToList();
            foreach (ListViewItem window in items)
            {
                window.Group = activeWindowGroup;
            }
            items.AddRange(filterItems);
            for (int iii = listItems.Length; iii < items.Count; iii++)
            {
                items[iii].Group = filterGroup;
            }


            listView1.Items.AddRange(items.ToArray());

            SelectedWindowHandles = currentSelectedWindows;
            SelectedFilters = currentSelectedFilters;
        }

        private void updater_Update(object sender, EventArgs args)
        {
            try
            {
                this.BeginInvoke((Action)delegate ()
                {
                    UpdateList((sender as WindowInfo.Holder).LatestInfo);
                });
            }
            catch (Exception)
            {
                (sender as WindowInfo.Holder).InfoUpdated -= updater_Update;
            }
        }


        #region Filter Config

        private void LoadFilterData(Filter filter)
        {
            updatingFilterConfigurationData = true;

            if (!filterListReference.Contains(filter))
                filterListReference.Add(filter);

            Filter.Data d = filter.FilterData;


            numericUpDownFiltersIndex.Enabled = true;
            numericUpDownParentIndex.Enabled = true;
            numericUpDownRootParentIndex.Enabled = true;

            numericUpDownSelectedFilterIndex.Enabled = true;
            numericUpDownTargetDesktopIndex.Enabled = true;

            numericUpDownVirtualDesktopLower.Enabled = true;
            numericUpDownVirtualDesktopUpper.Enabled = true;

            numericUpDownWindowIndexLower.Enabled = true;
            numericUpDownWindowIndexUpper.Enabled = true;



            numericUpDownSelectedFilterIndex.Value = filterListReference.IndexOf(filter) + 1;

            checkBoxWindowIndexLower.Checked = d.indexLowerBound >= 0;
            if (d.indexLowerBound < 0) { numericUpDownWindowIndexLower.Value = 1; numericUpDownWindowIndexLower.Enabled = false; }
            else numericUpDownWindowIndexLower.Value = d.indexLowerBound + 1;

            checkBoxWindowIndexUpper.Checked = d.indexUpperBound >= 0;
            if (d.indexUpperBound < 0) { numericUpDownWindowIndexUpper.Value = 1; numericUpDownWindowIndexUpper.Enabled = false; }
            else numericUpDownWindowIndexUpper.Value = d.indexUpperBound + 1;


            checkBoxVirtualDesktopLower.Checked = d.desktopLowerBound >= 0;
            if (d.desktopLowerBound < 0) { numericUpDownVirtualDesktopLower.Value = 1; numericUpDownVirtualDesktopLower.Enabled = false; }
            else numericUpDownVirtualDesktopLower.Value = d.desktopLowerBound + 1;

            checkBoxVirtualDesktopUpper.Checked = d.desktopUpperBound >= 0;
            if (d.desktopUpperBound < 0) { numericUpDownVirtualDesktopUpper.Value = 1; numericUpDownVirtualDesktopUpper.Enabled = false; }
            else numericUpDownVirtualDesktopUpper.Value = d.desktopUpperBound + 1;


            richTextBoxWindowTitle.Text = Utils.TextManager.CombineStringCollection(d.title);
            richTextBoxProcessName.Text = Utils.TextManager.CombineStringCollection(d.process);


            checkBoxMainProcessWindowCheckIf.Checked = d.checkIfMainWindow;
            checkBoxMainProcessWindowIsMain.Checked = d.isMainProcessWindow;


            UpdateLoadedFilterReferences(filter);

            chbAllowUnpin.Checked = d.desktopTargetAdv.allowUnpin;
            chbShouldPin.Checked = d.desktopTargetAdv.shouldPin;

            checkBoxTargetDesktopEnabled.Checked = d.desktopTargetAdv.targetDesktopIndex >= 0;
            if (d.desktopTargetAdv.targetDesktopIndex < 0) { numericUpDownTargetDesktopIndex.Value = 1; numericUpDownTargetDesktopIndex.Enabled = false; }
            else numericUpDownTargetDesktopIndex.Value = d.desktopTargetAdv.targetDesktopIndex + 1;

            loadedFilter = filter;

            updatingFilterConfigurationData = false;

            UpdateList();
        }

        private void UpdateLoadedFilterReferences(Filter newFilter = null)
        {
            Filter.Data d;
            if (newFilter == null)
                d = loadedFilter.FilterData;
            else
                d = newFilter.FilterData;

            listBoxParentIndex.Items.Clear();
            foreach (int index in Filter.GetFilterIndices(d.references.parentFilters, filterListReference.ToArray()))
            {
                listBoxParentIndex.Items.Add(index + 1);
            }

            listBoxRootParentIndex.Items.Clear();
            foreach (int index in Filter.GetFilterIndices(d.references.rootParentFilters, filterListReference.ToArray()))
            {
                listBoxRootParentIndex.Items.Add(index + 1);
            }

            listBoxFiltersIndex.Items.Clear();
            foreach (int index in Filter.GetFilterIndices(d.references.filters, filterListReference.ToArray()))
            {
                listBoxFiltersIndex.Items.Add(index + 1);
            }
        }


        #region Filter Configuration Form Events

        private void checkBoxWindowIndexLower_CheckedChanged(object sender, EventArgs e)
        {
            if (updatingFilterConfigurationData)
                return;

            numericUpDownWindowIndexLower.Value = 1;
            numericUpDownWindowIndexLower.Enabled = checkBoxWindowIndexLower.Checked;
            loadedFilter.FilterData.indexLowerBound = checkBoxWindowIndexLower.Checked ? 0 : -1;

            UpdateList();
        }

        private void checkBoxWindowIndexUpper_CheckedChanged(object sender, EventArgs e)
        {
            if (updatingFilterConfigurationData)
                return;

            numericUpDownWindowIndexUpper.Value = 1;
            numericUpDownWindowIndexUpper.Enabled = checkBoxWindowIndexUpper.Checked;
            loadedFilter.FilterData.indexUpperBound = checkBoxWindowIndexUpper.Checked ? 0 : -1;

            UpdateList();
        }

        private void numericUpDownWindowIndexLower_ValueChanged(object sender, EventArgs e)
        {
            if (updatingFilterConfigurationData)
                return;

            int current = (int)(numericUpDownWindowIndexLower.Value);
            if (current < 1)
                numericUpDownWindowIndexLower.Value = 1;
            else if (loadedFilter.FilterData.indexUpperBound >= 0 && loadedFilter.FilterData.indexUpperBound + 1 < current)
                numericUpDownWindowIndexLower.Value = loadedFilter.FilterData.indexUpperBound + 1;
            else
            {
                loadedFilter.FilterData.indexLowerBound = current - 1;
                UpdateList();
            }
        }

        private void numericUpDownWindowIndexUpper_ValueChanged(object sender, EventArgs e)
        {
            if (updatingFilterConfigurationData)
                return;

            int current = (int)(numericUpDownWindowIndexUpper.Value);
            if (current < 1)
                numericUpDownWindowIndexUpper.Value = 1;
            else if (loadedFilter.FilterData.indexLowerBound >= 0 && loadedFilter.FilterData.indexLowerBound + 1 > current)
                numericUpDownWindowIndexUpper.Value = loadedFilter.FilterData.indexLowerBound + 1;
            else
            {
                loadedFilter.FilterData.indexUpperBound = current - 1;
                UpdateList();
            }
        }

        private void checkBoxVirtualDesktopLower_CheckedChanged(object sender, EventArgs e)
        {
            if (updatingFilterConfigurationData)
                return;

            numericUpDownVirtualDesktopLower.Value = 1;
            numericUpDownVirtualDesktopLower.Enabled = checkBoxVirtualDesktopLower.Checked;
            loadedFilter.FilterData.desktopLowerBound = checkBoxVirtualDesktopLower.Checked ? 0 : -1;

            UpdateList();
        }

        private void checkBoxVirtualDesktopUpper_CheckedChanged(object sender, EventArgs e)
        {
            if (updatingFilterConfigurationData)
                return;

            numericUpDownVirtualDesktopUpper.Value = 1;
            numericUpDownVirtualDesktopUpper.Enabled = checkBoxVirtualDesktopUpper.Checked;
            loadedFilter.FilterData.desktopUpperBound = checkBoxVirtualDesktopUpper.Checked ? 0 : -1;

            UpdateList();
        }

        private void numericUpDownVirtualDesktopLower_ValueChanged(object sender, EventArgs e)
        {
            if (updatingFilterConfigurationData)
                return;

            int current = (int)(numericUpDownVirtualDesktopLower.Value);
            if (current < 1)
                numericUpDownVirtualDesktopLower.Value = 1;
            else if (loadedFilter.FilterData.desktopUpperBound >= 0 && loadedFilter.FilterData.desktopUpperBound + 1 < current)
                numericUpDownVirtualDesktopLower.Value = loadedFilter.FilterData.desktopUpperBound + 1;
            else
            {
                loadedFilter.FilterData.desktopLowerBound = current - 1;
                UpdateList();
            }
        }

        private void numericUpDownVirtualDesktopUpper_ValueChanged(object sender, EventArgs e)
        {
            if (updatingFilterConfigurationData)
                return;

            int current = (int)(numericUpDownVirtualDesktopUpper.Value);
            if (loadedFilter.FilterData.desktopLowerBound >= 0 && loadedFilter.FilterData.desktopLowerBound + 1 > current)
                numericUpDownVirtualDesktopUpper.Value = loadedFilter.FilterData.desktopLowerBound + 1;
            else if (current < 1)
                numericUpDownVirtualDesktopUpper.Value = 1;
            else
            {
                loadedFilter.FilterData.desktopUpperBound = current - 1;
                UpdateList();
            }
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            if (updatingFilterConfigurationData)
                return;

            loadedFilter.FilterData.title = Utils.TextManager.SplitMutiLineString(richTextBoxWindowTitle.Text);

            UpdateList();
        }

        private void richTextBox2_TextChanged(object sender, EventArgs e)
        {
            if (updatingFilterConfigurationData)
                return;

            loadedFilter.FilterData.process = Utils.TextManager.SplitMutiLineString(richTextBoxProcessName.Text);

            UpdateList();
        }

        private void checkBoxMainProcessWindowCheckIf_CheckedChanged(object sender, EventArgs e)
        {
            if (updatingFilterConfigurationData)
                return;

            loadedFilter.FilterData.checkIfMainWindow = checkBoxMainProcessWindowCheckIf.Checked;

            UpdateList();
        }

        private void checkBoxMainProcessWindowIsMain_CheckedChanged(object sender, EventArgs e)
        {
            if (updatingFilterConfigurationData)
                return;

            loadedFilter.FilterData.isMainProcessWindow = checkBoxMainProcessWindowIsMain.Checked;

            UpdateList();
        }

        private void numericUpDownParentIndex_ValueChanged(object sender, EventArgs e)
        {
            if (updatingFilterConfigurationData)
                return;

            int current = (int)(numericUpDownParentIndex.Value);
            if (current < 1)
                numericUpDownParentIndex.Value = 1;
            else if (current > filterListReference.Count)
                numericUpDownParentIndex.Value = filterListReference.Count;
        }

        private void buttonParentAddIndex_Click(object sender, EventArgs e)
        {
            if (updatingFilterConfigurationData)
                return;

            int index = (int)numericUpDownParentIndex.Value - 1;
            if (0 <= index && index < filterListReference.Count)
            {
                List<Filter> current = loadedFilter.FilterData.references.parentFilters.ToList();
                if (!current.Contains(filterListReference[index]))
                {
                    current.Add(filterListReference[index]);
                    loadedFilter.FilterData.references.parentFilters = current.ToArray();

                    UpdateLoadedFilterReferences();
                    UpdateList();
                }
            }
        }

        private void buttonParentRemove_Click(object sender, EventArgs e)
        {
            if (updatingFilterConfigurationData)
                return;

            List<int> indicesToRemove = new List<int>();
            foreach (int index in listBoxParentIndex.SelectedIndices)
            {
                indicesToRemove.Add(index);
            }
            indicesToRemove.Sort();
            List<Filter> current = loadedFilter.FilterData.references.parentFilters.ToList();
            int removed = 0;
            foreach (int index in indicesToRemove)
            {
                if (index - removed < current.Count)
                {
                    current.RemoveAt(index - removed++);
                }
            }
            if (removed > 0)
            {
                loadedFilter.FilterData.references.parentFilters = current.ToArray();

                UpdateLoadedFilterReferences();
                UpdateList();
            }
        }

        private void numericUpDownRootParentIndex_ValueChanged(object sender, EventArgs e)
        {
            if (updatingFilterConfigurationData)
                return;

            int current = (int)(numericUpDownRootParentIndex.Value);
            if (current < 1)
                numericUpDownRootParentIndex.Value = 1;
            else if (current > filterListReference.Count)
                numericUpDownRootParentIndex.Value = filterListReference.Count;
        }

        private void buttonRootParentAddIndex_Click(object sender, EventArgs e)
        {
            if (updatingFilterConfigurationData)
                return;


            int index = (int)numericUpDownRootParentIndex.Value - 1;
            if (0 <= index && index < filterListReference.Count)
            {
                List<Filter> current = loadedFilter.FilterData.references.rootParentFilters.ToList();
                if (!current.Contains(filterListReference[index]))
                {
                    current.Add(filterListReference[index]);
                    loadedFilter.FilterData.references.rootParentFilters = current.ToArray();

                    UpdateLoadedFilterReferences();
                    UpdateList();
                }
            }
        }

        private void buttonRootParentRemove_Click(object sender, EventArgs e)
        {
            if (updatingFilterConfigurationData)
                return;


            List<int> indicesToRemove = new List<int>();
            foreach (int index in listBoxRootParentIndex.SelectedIndices)
            {
                indicesToRemove.Add(index);
            }
            indicesToRemove.Sort();
            List<Filter> current = loadedFilter.FilterData.references.rootParentFilters.ToList();
            int removed = 0;
            foreach (int index in indicesToRemove)
            {
                if (index - removed < current.Count)
                {
                    current.RemoveAt(index - removed++);
                }
            }
            if (removed > 0)
            {
                loadedFilter.FilterData.references.rootParentFilters = current.ToArray();

                UpdateLoadedFilterReferences();
                UpdateList();
            }
        }

        private void numericUpDownFiltersIndex_ValueChanged(object sender, EventArgs e)
        {
            if (updatingFilterConfigurationData)
                return;


            int current = (int)(numericUpDownFiltersIndex.Value);
            if (current < 1)
                numericUpDownFiltersIndex.Value = 1;
            else if (current > filterListReference.Count)
                numericUpDownFiltersIndex.Value = filterListReference.Count;
        }

        private void buttonFiltersAddIndex_Click(object sender, EventArgs e)
        {
            if (updatingFilterConfigurationData)
                return;


            int index = (int)numericUpDownFiltersIndex.Value - 1;
            if (0 <= index && index < filterListReference.Count)
            {
                List<Filter> current = loadedFilter.FilterData.references.filters.ToList();
                if (!current.Contains(filterListReference[index]) && filterListReference[index] != loadedFilter)
                {
                    current.Add(filterListReference[index]);
                    loadedFilter.FilterData.references.filters = current.ToArray();

                    UpdateLoadedFilterReferences();
                    UpdateList();
                }
            }
        }

        private void buttonFiltersRemove_Click(object sender, EventArgs e)
        {
            if (updatingFilterConfigurationData)
                return;


            List<int> indicesToRemove = new List<int>();
            foreach (int index in listBoxFiltersIndex.SelectedIndices)
            {
                indicesToRemove.Add(index);
            }
            indicesToRemove.Sort();
            List<Filter> current = loadedFilter.FilterData.references.filters.ToList();
            int removed = 0;
            foreach (int index in indicesToRemove)
            {
                if (index - removed < current.Count)
                {
                    current.RemoveAt(index - removed++);
                }
            }
            if (removed > 0)
            {
                loadedFilter.FilterData.references.filters = current.ToArray();

                UpdateLoadedFilterReferences();
                UpdateList();
            }
        }

        private void chbAllowUnpin_CheckedChanged(object sender, EventArgs e)
        {
            if (updatingFilterConfigurationData)
                return;

            loadedFilter.FilterData.desktopTargetAdv.allowUnpin = chbAllowUnpin.Checked;

            UpdateList();
        }

        private void chbShouldPin_CheckedChanged(object sender, EventArgs e)
        {
            if (updatingFilterConfigurationData)
                return; 
            
            loadedFilter.FilterData.desktopTargetAdv.shouldPin = chbShouldPin.Checked;

            UpdateList();
        }

        private void checkBoxTargetDesktopEnabled_CheckedChanged(object sender, EventArgs e)
        {
            if (updatingFilterConfigurationData)
                return;

            numericUpDownTargetDesktopIndex.Value = 1;
            numericUpDownTargetDesktopIndex.Enabled = checkBoxTargetDesktopEnabled.Checked;
            loadedFilter.FilterData.desktopTargetAdv.targetDesktopIndex = checkBoxTargetDesktopEnabled.Checked ? 0 : -1;

            UpdateList();
        }

        private void numericUpDownTargetDesktopIndex_ValueChanged(object sender, EventArgs e)
        {
            if (updatingFilterConfigurationData)
                return;

            int current = (int)(numericUpDownTargetDesktopIndex.Value);
            if (current < 1)
                numericUpDownTargetDesktopIndex.Value = 1;
            else
            {
                loadedFilter.FilterData.desktopTargetAdv.targetDesktopIndex = current - 1;
                UpdateList();
            }
        }

        private void numericUpDownSelectedFilterIndex_ValueChanged(object sender, EventArgs e)
        {
            if (updatingFilterConfigurationData)
                return;


            int current = (int)(numericUpDownSelectedFilterIndex.Value);
            if (current < 1)
                numericUpDownSelectedFilterIndex.Value = 1;
            else if (current > filterListReference.Count)
                numericUpDownSelectedFilterIndex.Value = filterListReference.Count;
            else
            {
                LoadFilterData(filterListReference[current - 1]);
            }
        }

        private void buttonCreateNewFilter_Click(object sender, EventArgs e)
        {
            if (updatingFilterConfigurationData)
                return;

            LoadFilterData(new Filter());
        }

        private void buttonDeleteFilter_Click(object sender, EventArgs e)
        {
            if (updatingFilterConfigurationData)
                return;

            DialogResult result = MessageBox.Show("Are you sure you want to delete this filter?", "Delete filter?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
                return;

            int index = filterListReference.IndexOf(loadedFilter);

            filterListReference.Remove(loadedFilter);

            if (index >= filterListReference.Count)
                index = filterListReference.Count - 1;

            Filter toLoad;
            if (index < 0)
                toLoad = new Filter();
            else
                toLoad = filterListReference[index];

            LoadFilterData(toLoad);
        }

        #endregion Filter Configuration Form Events

        #endregion Filter Config


        #region Save / Load

        private void buttonSave_Click(object sender, EventArgs e)
        {
            try
            {
                string savePath = Utils.IOManager.PromtUserForSaveFile("Filter save location", "VirtualDesktopManagerFilters", Utils.IOManager.CreateFileDialogFilter("All files", "*.*") + "|" + Utils.IOManager.CreateFileDialogFilter("Text file", "*.txt"), 2);

                if (!String.IsNullOrEmpty(savePath))
                    File.WriteAllText(savePath, Utils.SaveManager.Serialize(Filter.SaveFile.CreateSaveFile(filterListReference.ToArray())), Encoding.UTF8);
            }
            catch { }
        }

        private void buttonLoad_Click(object sender, EventArgs e)
        {
            bool updated = false;
            try
            {
                string loadPath = Utils.IOManager.PromptUserForFilePath(Utils.IOManager.CreateFileDialogFilter("All files", "*.*") + "|" + Utils.IOManager.CreateFileDialogFilter("Text file", "*.txt"), 2);
                if (String.IsNullOrEmpty(loadPath))
                    return;

                string text = File.ReadAllText(loadPath, Encoding.UTF8);
                Filter.SaveFile loadedData = Utils.SaveManager.Deserialize<Filter.SaveFile>(text);
                Filter[] filters = loadedData.RecreateData();
                updated = true;
                filterListReference.Clear();
                filterListReference.AddRange(filters);
            }
            catch { }

            if (updated)
            {
                if (filterListReference.Count > 0)
                    LoadFilterData(filterListReference[0]);
                else
                    LoadFilterData(new Filter());
            }
        }

        #endregion Save / Load


        #region List View Input

        private void listView1_ItemActivate(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 1)
            {
                Filter selectedFilter = listView1.SelectedItems[0].Tag as Filter;
                if (selectedFilter != null)
                {
                    LoadFilterData(selectedFilter);
                }
            }
        }

        #endregion List View Input


        private void buttonApplyFilters_Click(object sender, EventArgs e)
        {
            listData.ApplyFilters(filterListReference.ToArray(), appContext.Data.PreventFlashingWindows);
        }

        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            if (updater != null)
                updater.Invalidate();
        }

        private void chbSmoothCurrentDesktopSwitch_CheckedChanged(object sender, EventArgs e)
        {
            if (appContext.Data.SmoothDesktopSwitching == chbSmoothCurrentDesktopSwitch.Checked)
                return;
            appContext.Data.SmoothDesktopSwitching = chbSmoothCurrentDesktopSwitch.Checked;
            appContext.InvalidateSavedSettings();
        }
        private void chbPreventFlashingWindows_CheckedChanged(object sender, EventArgs e)
        {
            if (appContext.Data.PreventFlashingWindows == chbPreventFlashingWindows.Checked)
                return;
            appContext.Data.PreventFlashingWindows = chbPreventFlashingWindows.Checked;
            appContext.InvalidateSavedSettings();
        }

        private void chbStartWithAdminRights_CheckedChanged(object sender, EventArgs e)
        {
            if (appContext.Data.StartWithAdminRights == chbStartWithAdminRights.Checked)
                return;
            appContext.Data.StartWithAdminRights = chbStartWithAdminRights.Checked;
            appContext.InvalidateSavedSettings();
        }

        #endregion Methods


        #region Properties

        private IntPtr[] SelectedWindowHandles
        {
            get
            {
                return listView1.SelectedItems.Cast<ListViewItem>().Where(item => item.Tag is IntPtr).Select(item => (IntPtr)item.Tag).ToArray();
            }
            set
            {
                foreach (var item in listView1.Items.Cast<ListViewItem>().Where(item => item.Tag is IntPtr))
                {
                    item.Selected = value.ToList().Contains((IntPtr)item.Tag);
                }
            }
        }

        private Filter[] SelectedFilters
        {
            get
            {
                return listView1.SelectedItems.Cast<ListViewItem>().Where(item => item.Tag is Filter).Select(item => (Filter)item.Tag).ToArray();
            }
            set
            {
                foreach (var item in listView1.Items.Cast<ListViewItem>().Where(item => item.Tag is Filter))
                {
                    item.Selected = value.ToList().Contains((Filter)item.Tag);
                }
            }
        }

        #endregion Properties
    }
}
