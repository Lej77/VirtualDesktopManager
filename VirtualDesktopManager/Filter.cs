using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VirtualDesktopManager.Extensions;

namespace VirtualDesktopManager
{
    public class Filter
    {
        #region Classes

        [Serializable]
        public class SaveFile
        {
            public List<SaveData> filters = new List<SaveData>();

            private SaveFile()
            {

            }

            public Filter[] RecreateData()
            {
                List<Filter> filterList = new List<Filter>();

                for (int iii = 0; iii < filters.Count; iii++)
                {
                    filterList.Add(new Filter());
                }

                for (int iii = 0; iii < filters.Count; iii++)
                {
                    filterList[iii].data = filters[iii].Load(filterList.ToArray());
                }

                return filterList.ToArray();
            }

            public static SaveFile CreateSaveFile(Filter[] filterList)
            {
                SaveFile save = new SaveFile();
                foreach (Filter filter in filterList)
                {
                    save.filters.Add(SaveData.CreateSaveData(filter.data, filterList));
                }
                return save;
            }
        }

        [Serializable]
        public class SaveData
        {
            public Data data = null;

            private int[] parentFilterIndices = new int[0];
            private int[] rootParentFilterIndices = new int[0];

            private int[] filterIndices = new int[0];

            private SaveData()
            {

            }

            public static SaveData CreateSaveData(Data data, Filter[] filterList)
            {
                SaveData save = new SaveData();
                save.data = data.DeepCopyWithSerialization();

                save.parentFilterIndices = GetFilterIndices(data.references.parentFilters, filterList);
                save.rootParentFilterIndices = GetFilterIndices(data.references.rootParentFilters, filterList);
                save.filterIndices = GetFilterIndices(data.references.filters, filterList);

                return save;
            }

            public Data Load(Filter[] filterList)
            {
                Data load = data.Clone() as Data;

                load.references = new Data.RefData()
                {
                    parentFilters = GetFilters(parentFilterIndices, filterList),
                    rootParentFilters = GetFilters(rootParentFilterIndices, filterList),
                    filters = GetFilters(filterIndices, filterList)
                };

                // Check for legacy setting:
                load.UpdateLegacySettings();

                return load;
            }
        }

        [Serializable]
        public class DesktopTarget : ICloneable
        {

            public bool allowUnpin = false;
            public bool shouldPin = false;

            public int targetDesktopIndex = -1;

            public object Clone()
            {
                DesktopTarget clone = MemberwiseClone() as DesktopTarget;
                return clone;
            }

            public bool IsActive()
            {
                return shouldPin || targetDesktopIndex >= 0;
            }
        }

        [Serializable]
        public class Data : ICloneable
        {
            #region Classes

            public class RefData : ICloneable
            {
                public Filter[] parentFilters = new Filter[0];
                public Filter[] rootParentFilters = new Filter[0];

                public Filter[] filters = new Filter[0];

                public object Clone()
                {
                    RefData clone = MemberwiseClone() as RefData;
                    clone.parentFilters = parentFilters.ToArray();
                    clone.rootParentFilters = rootParentFilters.ToArray();
                    clone.filters = filters.ToArray();

                    return clone;
                }
            }

            #endregion Classes


            #region Member Variables

            public int indexLowerBound = -1;
            public int indexUpperBound = -1;

            public int desktopLowerBound = -1;
            public int desktopUpperBound = -1;

            public string[] title = new string[0]; // title text with wildcards between them. First index must be at start of title and last index must be at end of title.
            public string[] process = new string[0];

            public bool isMainProcessWindow = false;
            public bool checkIfMainWindow = false;

            [NonSerialized]
            public RefData references = new RefData();

            /// <summary>
            /// This value is just to load old save files correctly and will not correspond to the users actual settings. Use <see cref="desktopTargetAdv"/> instead.
            /// </summary>
            public int? desktopTarget = null;
            public DesktopTarget desktopTargetAdv = new DesktopTarget();

            #endregion Member Variables


            #region Constructors

            #endregion Constructors


            #region Methods

            public object Clone()
            {
                Data clone = MemberwiseClone() as Data;

                clone.title = title.ToArray();
                clone.process = process.ToArray();
                clone.references = references.Clone() as RefData;
                clone.desktopTargetAdv = desktopTargetAdv.Clone() as DesktopTarget;

                return clone;
            }

            public void UpdateLegacySettings()
            {
                if (desktopTarget.HasValue)
                {
                    desktopTargetAdv.targetDesktopIndex = desktopTarget.Value;
                }
                desktopTarget = null;
            }

            #endregion Methods


            #region Properties

            #endregion Properties
        }

        #endregion Classes


        #region Member Variables

        private Data data = new Data();

        #endregion Member Variables


        #region Constructors

        #endregion Constructors


        #region Methods

        #region Check Filter

        public bool CheckIfWindowPassFilter(WindowInfo info, WindowInfo.Data windowData)
        {
            return CheckIfWindowPassFilter(info, windowData, new List<Filter>());
        }

        private bool CheckIfWindowPassFilter(WindowInfo info, WindowInfo.Data windowData, List<Filter> testedFilters)
        {
            if (data.indexLowerBound >= 0 || data.indexUpperBound >= 0)
            {
                int index = windowData.WindowInfo.ToList().IndexOf(info);
                if (index >= 0)
                {
                    if (data.indexLowerBound >= 0 && data.indexLowerBound > index)
                        return false;
                    if (data.indexUpperBound >= 0 && data.indexUpperBound < index)
                        return false;
                }
            }
            if (data.desktopLowerBound >= 0 || data.desktopUpperBound >= 0)
            {
                int index = windowData.DetermineDesktopIndex(info.Desktop);
                if (index >= 0)
                {
                    if (data.desktopLowerBound >= 0 && data.desktopLowerBound > index)
                        return false;
                    if (data.desktopUpperBound >= 0 && data.desktopUpperBound < index)
                        return false;
                }
            }
            if (!CheckTextFilter(info.Title, data.title) || (info.Process != null && !CheckTextFilter(info.Process.ProcessName, data.process)))
            {
                return false;
            }
            if (data.checkIfMainWindow)
            {
                bool isMain = windowData.MainWindows.ToList().Contains(info.WindowHandle);
                if (isMain != data.isMainProcessWindow)
                {
                    return false;
                }
            }


            testedFilters.Add(this);

            List<Filter> extraFilters = new List<Filter>();
            extraFilters.AddRange(data.references.filters);
            if (info.WindowHandle == info.ParentWindowHandle)
            {
                extraFilters.AddRange(data.references.parentFilters);
            }
            if (info.WindowHandle == info.RootParentWindowHandle)
            {
                extraFilters.AddRange(data.references.rootParentFilters);
            }

            if (!CheckIfWindowPassFilters(extraFilters.ToArray(), info, windowData, testedFilters))
            {
                return false;
            }

            if (data.references.parentFilters.Length > 0 && info.ParentWindowHandle != IntPtr.Zero && info.ParentWindowHandle != info.WindowHandle)
            {
                WindowInfo parent = null;
                foreach (var window in windowData.WindowInfo)
                {
                    if (window.WindowHandle == info.ParentWindowHandle)
                    {
                        parent = window;
                        break;
                    }
                }
                if (parent != null)
                {
                    if (!CheckIfWindowPassFilters(data.references.parentFilters, parent, windowData))
                    {
                        return false;
                    }
                }
            }

            if (data.references.rootParentFilters.Length > 0 && info.RootParentWindowHandle != IntPtr.Zero && info.RootParentWindowHandle != info.WindowHandle)
            {
                WindowInfo rootParent = null;
                foreach (var window in windowData.WindowInfo)
                {
                    if (window.WindowHandle == info.RootParentWindowHandle)
                    {
                        rootParent = window;
                        break;
                    }
                }
                if (rootParent != null)
                {
                    if (!CheckIfWindowPassFilters(data.references.rootParentFilters, rootParent, windowData))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool CheckIfWindowPassFilters(Filter[] filtersToPass, WindowInfo info, WindowInfo.Data windowData, List<Filter> testedFilters = null)
        {
            if (testedFilters == null)
                testedFilters = new List<Filter>();


            foreach (Filter filter in filtersToPass)
            {
                if (!testedFilters.Contains(filter))
                {
                    bool passed = filter.CheckIfWindowPassFilter(info, windowData, testedFilters);
                    if (!passed)
                        return false;
                }
            }
            return true;
        }

        private static bool CheckTextFilter(string text, string[] filter, bool caseSensitive = false)
        {
            if (!caseSensitive)
                text = text.ToLowerInvariant();

            for (int iii = 0; iii < filter.Length; iii++)
            {
                string entry = filter[iii];
                if (entry == "")
                    continue;
                if (!caseSensitive)
                    entry = entry.ToLowerInvariant();

                if (iii == 0)
                {
                    if (!text.StartsWith(entry))
                        return false;
                }
                if (iii + 1 == filter.Length)
                {
                    if (!text.EndsWith(entry))
                        return false;
                }
                int index = text.IndexOf(entry);
                if (index < 0)
                    return false;
                else
                {
                    text = text.Remove(0, index + entry.Length);
                }
            }
            return true;
        }

        #endregion Check Filter


        #region Filter Array Management

        public static Filter[] GetApplicableFilters(WindowInfo info, WindowInfo.Data data, Filter[] filterList)
        {
            List<Filter> filters = new List<Filter>();
            foreach (Filter filter in filterList)
            {
                if (filter.CheckIfWindowPassFilter(info, data))
                {
                    filters.Add(filter);
                }
            }
            return filters.ToArray();
        }

        public static DesktopTarget GetFirstDesktopTarget(Filter[] applicableFilters)
        {
            foreach (Filter filter in applicableFilters)
            {
                if (filter.data.desktopTargetAdv.IsActive())
                {
                    return filter.data.desktopTargetAdv;
                }
            }
            return null;
        }

        public static int[] GetFilterIndices(Filter[] filtersToFind, Filter[] filterList)
        {
            List<int> indices = new List<int>();
            List<Filter> fl = filterList.ToList();
            foreach (Filter toFind in filtersToFind)
            {
                int index = fl.IndexOf(toFind);
                if (index >= 0)
                    indices.Add(index);
            }
            return indices.ToArray();
        }

        public static Filter[] GetFilters(int[] filterIndices, Filter[] filterList)
        {
            List<Filter> filters = new List<Filter>();
            foreach (int index in filterIndices)
            {
                if (0 < index && index < filterList.Length)
                    filters.Add(filterList[index]);
            }
            return filters.ToArray();
        }

        #endregion Filter Array Management


        #region Info Prints

        public static string FormatIntervall(int lowerBound, int upperBound)
        {
            bool lower = lowerBound > 0;
            bool upper = upperBound > 0;
            if (lower && upper)
            {
                if (lowerBound == upperBound)
                    return upperBound.ToString();
                else if (lowerBound > upperBound)
                    return "";
                else
                    return lowerBound + "-" + upperBound;
            }
            else if (lower || upper)
            {
                string text = "";
                if (lower)
                    text += lowerBound;
                text += "-";
                if (upper)
                    text += upperBound;
                return text;
            }
            else return "";
        }

        public static string FormatIntArray(int[] array)
        {
            string text = "";
            foreach (int i in array)
            {
                if (text != "")
                    text += ", ";
                text += (i + 1);
            }
            return text;
        }

        #endregion Info Prints

        #endregion Methods


        #region Properties

        public Data FilterData
        {
            get
            {
                return data;
            }
        }

        #endregion Properties
    }
}
