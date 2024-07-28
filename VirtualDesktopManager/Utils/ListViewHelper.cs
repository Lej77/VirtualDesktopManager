using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;

namespace VirtualDesktopManager.Utils
{
    public class ListViewHelper
    {
        #region Classes

        private class ListComparer : IComparer<ListViewItem>, System.Collections.IComparer
        {
            private int index = 0;
            private bool inverted = false;

            public ListComparer(int subItemIndex, bool invertedSorting)
            {
                index = subItemIndex;
                this.inverted = invertedSorting;
            }

            public int Compare(ListViewItem x, ListViewItem y)
            {
                if (index < 0)
                    return 0;

                if ((x == null && y == null) || ReferenceEquals(x, y))
                    return 0;
                if (x == null)
                    return -1;
                if (y == null)
                    return 1;

                bool xIndex = index < x.SubItems.Count;
                bool yIndex = index < y.SubItems.Count;

                if (!xIndex || !yIndex)
                    return 0;

                string xText = x.SubItems[index].Text;
                string yText = y.SubItems[index].Text;

                int returnValue;
                if (int.TryParse(xText, out int xInt) && int.TryParse(yText, out int yInt))
                    returnValue = xInt.CompareTo(yInt);
                else
                    returnValue = xText.CompareTo(yText);
                return (inverted ? -1 : 1) * returnValue;
            }

            public int Compare(object x, object y)
            {
                return Compare(x as ListViewItem, y as ListViewItem);
            }
        }

        #endregion Classes


        #region Member Variables

        private ListView listView = null;

        private bool columnMinWidthEnabled = false;
        private int[] columnMinWidthSizes = new int[0];

        private bool sortEnabled = false;
        private int sortColumn = -1;
        private bool sortInverted = false;
        private bool sortOnClicks = false;
        private bool sortOnClicksAllowDisable = false;

        #endregion Member Variables


        #region Constructors

        public ListViewHelper(ListView listView)
        {
            this.listView = listView;
        }

        #endregion Constructors


        #region Methods

        public static ListViewHelper Manage(ListView listView)
        {
            return new ListViewHelper(listView);
        }


        private void PreventTooSmallColumns(object sender, ColumnWidthChangedEventArgs e)
        {
            if (!columnMinWidthEnabled || e.ColumnIndex < 0 || listView.Columns.Count <= e.ColumnIndex || columnMinWidthSizes == null || columnMinWidthSizes.Length <= e.ColumnIndex)
                return;

            if (listView.Columns[e.ColumnIndex].Width < columnMinWidthSizes[e.ColumnIndex])
                listView.Columns[e.ColumnIndex].Width = columnMinWidthSizes[e.ColumnIndex];
        }

        private void CheckColumnWidths()
        {
            for (int iii = 0; iii < columnMinWidthSizes.Length && iii < listView.Columns.Count; iii++)
            {
                if (listView.Columns[iii].Width < columnMinWidthSizes[iii])
                    listView.Columns[iii].Width = columnMinWidthSizes[iii];
            }
        }

        private void ColumnHeaderClick(object sender, ColumnClickEventArgs e)
        {
            if (IndexOfSortingColumn != e.Column)
            {
                IndexOfSortingColumn = e.Column;
                IsSortingInverted = false;
            }
            else if (IsSortingInverted && AllowDisablingSortingOnColumnHeaderClick)
                IndexOfSortingColumn = -1;
            else
                IsSortingInverted = !IsSortingInverted;
        }

        private void OnSortingChanged()
        {
            if (SortingEnabled)
                listView.ListViewItemSorter = new ListComparer(sortColumn, sortInverted);
        }

        private void ManuallySortList()
        {
            List<ListViewItem> items = listView.Items.Cast<ListViewItem>().ToList();
            items.Sort(new ListComparer(sortColumn, sortInverted));

            listView.Items.Clear();
            listView.Items.AddRange(items.ToArray());
        }

        #endregion Methods


        #region Properties

        public bool ColumnMinWidthsEnabled
        {
            get
            {
                return columnMinWidthEnabled;
            }
            set
            {
                if (value == columnMinWidthEnabled)
                    return;

                columnMinWidthEnabled = value;

                if (value)
                {
                    listView.ColumnWidthChanged -= PreventTooSmallColumns;
                    listView.ColumnWidthChanged += PreventTooSmallColumns;

                    CheckColumnWidths();
                }
                else
                {
                    listView.ColumnWidthChanged -= PreventTooSmallColumns;
                }
            }
        }

        public int[] ColumnMinWidths
        {
            get
            {
                return columnMinWidthSizes.ToArray();
            }
            set
            {
                columnMinWidthSizes = value.ToArray();

                CheckColumnWidths();
            }
        }

        public int[] ColumnWidths
        {
            get
            {
                return listView.Columns.Cast<ColumnHeader>().Select(column => column.Width).ToArray();
            }
            set
            {
                for (int iii = 0; iii < value.Length && iii < listView.Columns.Count; iii++)
                {
                    int width = value[iii];
                    if (width < 0)
                        continue;
                    listView.Columns[iii].Width = width;
                }
            }
        }

        public int[] ColumnHeaderWidths
        {
            get
            {
                bool minEnabled = columnMinWidthEnabled;
                try
                {
                    columnMinWidthEnabled = false;

                    int[] currentWidths = ColumnWidths;
                    try
                    {
                        ColumnHeader dummyHeader = new ColumnHeader();
                        try
                        {
                            listView.Columns.Add(dummyHeader);

                            return listView.Columns.Cast<ColumnHeader>().Select(header =>
                            {
                                header.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
                                return header.Width;
                            }).ToArray();
                        }
                        finally
                        {
                            listView.Columns.Remove(dummyHeader);
                        }
                    }
                    finally
                    {
                        ColumnWidths = currentWidths;
                    }
                }
                finally
                {
                    columnMinWidthEnabled = minEnabled;
                }
            }
        }


        public bool SortingEnabled
        {
            get { return sortEnabled; }
            set
            {
                if (sortEnabled == value)
                    return;

                sortEnabled = value;

                if (value)
                {
                    OnSortingChanged();
                }
                else
                {
                    listView.ListViewItemSorter = null;
                }
            }
        }

        /// <summary>
        /// Index of the colum to use for sorting. Sorting is disabled if index is out of bounds.
        /// </summary>
        public int IndexOfSortingColumn
        {
            get
            {
                return sortColumn;
            }
            set
            {
                if (sortColumn == value)
                    return;

                sortColumn = value;

                OnSortingChanged();
            }
        }

        public bool IsSortingInverted
        {
            get
            {
                return sortInverted;
            }
            set
            {
                if (sortInverted == value)
                    return;

                sortInverted = value;

                OnSortingChanged();
            }
        }
        

        public bool ChangeSortingOnColumnHeaderClick
        {
            get
            {
                return sortOnClicks;
            }
            set
            {
                if (sortOnClicks == value)
                    return;

                sortOnClicks = value;

                if (value)
                {
                    listView.ColumnClick -= ColumnHeaderClick;
                    listView.ColumnClick += ColumnHeaderClick;
                }
                else
                {
                    listView.ColumnClick -= ColumnHeaderClick;
                }
            }
        }

        public bool AllowDisablingSortingOnColumnHeaderClick
        {
            get { return sortOnClicksAllowDisable; }
            set
            {
                if (sortOnClicksAllowDisable == value)
                    return;

                sortOnClicksAllowDisable = value;
            }
        }

        #endregion Properties
    }
}