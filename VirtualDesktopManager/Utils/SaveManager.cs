using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Reflection;

namespace VirtualDesktopManager.Utils
{
    public static class SaveManager
    {
        #region Save systems

        public static class SaveSystem1
        {
            /// <summary>
            /// Ensures that all new values are specifed by setting any unspecified (list is empty) values to the original values.
            /// </summary>
            /// <param name="valuesToCheck">The values to check.</param>
            /// <param name="originalValues">The values that will be used to replace any unspecifed value.</param>
            /// <returns>A list that are asured to have values for all.</returns>
            public static List<string>[] EnsureValuesForAll(List<string>[] valuesToCheck, List<string>[] originalValues)
            {
                EnsureValuesForAll(ref valuesToCheck, originalValues);
                return valuesToCheck;
            }

            /// <summary>
            /// Ensures that all new values are specifed by setting any unspecified (list is empty) values to the original values.
            /// </summary>
            /// <param name="valuesToCheck">The values to check.</param>
            /// <param name="originalValues">The values that will be used to replace any unspecifed value.</param>
            public static void EnsureValuesForAll(ref List<string>[] valuesToCheck, List<string>[] originalValues)
            {
                for (int iii = 0; iii < originalValues.Length; iii++)
                {
                    if (valuesToCheck[iii].Count == 0) valuesToCheck[iii] = originalValues[iii];
                }
            }


            /// <summary>
            /// Converts values of different types to text.
            /// </summary>
            /// <param name="values">An array with lists that contain data of different types. Each array hold only one data type.</param>
            /// <param name="dataType">Identifies what type the values are by referencing to an enum with the tyoes.</param>
            /// <returns>A list with the text that the values were converted to.</returns>
            public static List<string> SaveToText(List<string>[] values, Type dataType)
            {
                string[] dataIDKeywords = Enum.GetNames(dataType);

                List<string> text = new List<string>();

                for (int iii = 0; iii < dataIDKeywords.Length; iii++)
                {
                    foreach (string value in values[iii]) text.Add(dataIDKeywords[iii] + " = " + TextManager.SurroundWithQuotes(value));
                }

                return text;
            }

            /// <summary>
            /// Converts text into values of different types.
            /// </summary>
            /// <param name="text">The text to convert.</param>
            /// <param name="dataType">A reference to an enum type which corresponds to the types of data in the text.</param>
            /// <returns>An array of lists with values in string form. Each array holds one type of data.</returns>
            public static List<string>[] LoadFromText(List<string> text, Type dataType)
            {
                string[] dataIDKeywords = Enum.GetNames(dataType);

                List<string>[] values = new List<string>[dataIDKeywords.Length];
                for (int iii = 0; iii < values.Length; iii++) values[iii] = new List<string>();  // initiate lists for all values.

                if (text == null) return values;

                for (int iii = 0; iii < text.Count; iii++)
                {
                    string line = text.ElementAt(iii);

                    if (line.Contains("//"))
                    {
                        line = line.Remove(line.IndexOf("//"));
                    }

                    if (line.Contains('='))
                    {
                        string dataIDKeyword = line.Remove(line.IndexOf('='));
                        string dataValue = line.Remove(0, dataIDKeyword.Length + 1);

                        dataIDKeyword = TextManager.RemoveSurroundingSpaces(dataIDKeyword);
                        dataValue = TextManager.RemoveSurroundingQuotes(TextManager.RemoveSurroundingSpaces(dataValue));

                        try
                        {
                            values[getDataID(dataIDKeyword, dataIDKeywords)].Add(dataValue);
                        }
                        catch
                        {
                            // Non existant keyword most likely!
                        }

                    }
                }


                return values;
            }

            /// <summary>
            /// Helper function to ConvertTextToData. Finds type of data from keyword in text.
            /// </summary>
            /// <param name="keyword">Keyword to indetify data type from.</param>
            /// <param name="dataIDKeywords">Keywords for different data types.</param>
            /// <returns>Index of the referenced data type.</returns>
            private static int getDataID(string keyword, string[] dataIDKeywords)
            {
                for (int iii = 0; iii < dataIDKeywords.Length; iii++)
                {
                    if (keyword == dataIDKeywords[iii])
                    {
                        return iii;
                    }
                }
                throw new Exception("Keyword doesn't exist!");
            }
        }

        public static class SaveSystem2
        {
            #region classes and interfaces

            public interface ISaveable
            {
                /// <summary>
                /// A class that contains properties of the type string or list string. 
                /// </summary>
                object SaveableObject { get; }
            }

            public interface ISaveableNotified : ISaveable
            {
                /// <summary>
                /// Will be called when a save is started on this object and it should become ready to have its values retrieved.
                /// Can be used to make a lock since it is garenteed that the completed function will be called.
                /// </summary>
                void SaveStarted();

                void SaveCompleted();

                /// <summary>
                /// Will be called when a load is started on this object and it should become ready to have its values set.
                /// Can be used to make a lock since it is garenteed that the completed function will be called.
                /// </summary>
                void LoadStarted();

                void LoadCompleted();
            }

            public interface ISaveableSerializable : ISaveable
            {
                string SaveBySerializingObject();

                Type LoadByDeserializingOjbectOfType();

                void LoadFromDeserialized(object loadedObject);
            }

            /// <summary>
            /// Helper class for saveable classes. Contains default properties, values and methods for the interfaces.
            /// </summary>
            public abstract class Saveable : ISaveableNotified, ISaveableSerializable
            {
                /// <summary>
                /// This object can be used to read properties of certain types and use that data to save with.
                /// </summary>
                public virtual object SaveableObject { get; } = null;

                /// <summary>
                /// This property can be used as an intermediate to a class that can be serielized. If this is done saving will be done by serielizing this object.
                /// </summary>
                protected virtual object DataContainer { get; set; } = null;

                public virtual string SaveBySerializingObject()
                {
                    if (DataContainer == null) throw new NotImplementedException();
                    else return SaveManager.Serialize(DataContainer);
                }

                /// <summary>
                /// Will be called when a save is started on this object and it should become ready to have its values retrieved.
                /// Can be used to make a lock since it is garenteed that the completed function will be called.
                /// </summary>
                public virtual void SaveStarted()
                {

                }

                public virtual void SaveCompleted()
                {

                }

                /// <summary>
                /// Will be called when a load is started on this object and it should become ready to have its values set.
                /// Can be used to make a lock since it is garenteed that the completed function will be called.
                /// </summary>
                public virtual void LoadStarted()
                {

                }

                public virtual void LoadCompleted()
                {

                }

                public virtual Type LoadByDeserializingOjbectOfType()
                {
                    return DataContainer.GetType();
                }

                public virtual void LoadFromDeserialized(object loadedObject)
                {
                    if (DataContainer == null) throw new NotImplementedException();
                    else DataContainer = loadedObject;
                }
            }

            #endregion classes and interfaces

            public static string SaveToText(object objectToSave)
            {
                if (typeof(ISaveable).IsAssignableFrom(objectToSave.GetType()))
                {
                    return SaveToText((ISaveable)objectToSave);
                }
                else
                {
                    // serialize:
                    return Serialize(objectToSave);
                }
            }
            public static string SaveToText(ISaveable objectToSave)
            {
                ISaveableNotified notifiedVersion = null;
                if (typeof(ISaveableNotified).IsAssignableFrom(objectToSave.GetType()))
                {
                    notifiedVersion = (ISaveableNotified)objectToSave;
                }
                return SaveToText(objectToSave, notifiedVersion);
            }
            public static string SaveToText(ISaveableNotified objectToSave)
            {
                return SaveToText(objectToSave, objectToSave);
            }

            private static string SaveToText(ISaveable objectToSave, ISaveableNotified notifiedVersion)
            {
                if (notifiedVersion != null)
                {
                    objectToSave = notifiedVersion;
                }

                string saveText = "";
                bool saved = false;

                try
                {
                    if (notifiedVersion != null) notifiedVersion.SaveStarted();

                    ISaveableSerializable serializableVersion = null;
                    if (typeof(ISaveableSerializable).IsAssignableFrom(objectToSave.GetType()))
                    {
                        serializableVersion = (ISaveableSerializable)objectToSave;
                    }

                    if (serializableVersion != null)
                    {
                        try
                        {
                            saveText = serializableVersion.SaveBySerializingObject();
                            saved = true;
                        }
                        catch (NotImplementedException) { }
                    }
                    if (!saved)
                    {
                        saveText = SaveToTextUsingReflection(objectToSave.SaveableObject);
                    }
                }
                finally
                {
                    if (notifiedVersion != null) notifiedVersion.SaveCompleted();
                }
                return saveText;
            }
            private static string SaveToTextUsingReflection(object data)
            {
                List<string> text = new List<string>();
                PropertyInfo[] properties = data.GetType().GetProperties();

                foreach (PropertyInfo property in properties)
                {
                    string name = property.Name;
                    Type propertyType = property.PropertyType;
                    object value = property.GetValue(data);

                    List<string> valueAsList;
                    if (propertyType == typeof(List<string>))
                    {
                        valueAsList = (List<string>)value;
                    }
                    else if (propertyType == typeof(string))
                    {
                        valueAsList = new List<string>();
                        valueAsList.Add((string)value);
                    }
                    else continue;

                    if (valueAsList.Count == 1)
                    {
                        text.Add(name + " = " + TextManager.SurroundWithQuotes(valueAsList[0]));
                    }
                    else if (valueAsList.Count > 1)
                    {
                        text.Add(name + ":");
                        text.Add("{");
                        text.AddRange(valueAsList);
                        text.Add("}");
                    }
                }
                return TextManager.CombineStringCollection(text);
            }


            public static object LoadFromText(object objectToLoad, string textToLoadFrom)
            {
                if (typeof(ISaveable).IsAssignableFrom(objectToLoad.GetType()))
                {
                    LoadFromText((ISaveable)objectToLoad, textToLoadFrom);
                    return null;
                }
                else
                {
                    // deserialize:
                    return Deserialize(textToLoadFrom, objectToLoad.GetType());
                }
            }
            public static void LoadFromText(ISaveable objectToLoad, string textToLoadFrom)
            {
                ISaveableNotified notifiedVersion = null;
                if (typeof(ISaveableNotified).IsAssignableFrom(objectToLoad.GetType()))
                {
                    notifiedVersion = (ISaveableNotified)objectToLoad;
                }
                LoadFromText(objectToLoad, textToLoadFrom, notifiedVersion);
            }
            public static void LoadFromText(ISaveableNotified objectToLoad, string textToLoadFrom)
            {
                LoadFromText(objectToLoad, textToLoadFrom, objectToLoad);
            }

            private static void LoadFromText(ISaveable objectToLoad, string textToLoadFrom, ISaveableNotified notifiedVersion)
            {
                if (textToLoadFrom == null) return;

                if (notifiedVersion != null)
                {
                    objectToLoad = notifiedVersion;
                }

                bool loaded = false;

                try
                {
                    if (notifiedVersion != null) notifiedVersion.LoadStarted();

                    ISaveableSerializable serializableVersion = null;
                    if (typeof(ISaveableSerializable).IsAssignableFrom(objectToLoad.GetType()))
                    {
                        serializableVersion = (ISaveableSerializable)objectToLoad;
                    }

                    if (serializableVersion != null)
                    {
                        Type serializationType = serializableVersion.LoadByDeserializingOjbectOfType();

                        if (serializationType != null && serializationType.IsSerializable)
                        {
                            try
                            {
                                serializableVersion.LoadFromDeserialized(Deserialize(textToLoadFrom, serializationType));
                                loaded = true;
                            }
                            catch (NotImplementedException) { }
                        }
                    }

                    if (!loaded)
                    {
                        LoadFromTextUsingReflection(objectToLoad.SaveableObject, textToLoadFrom);
                    }
                }
                finally
                {
                    if (notifiedVersion != null) notifiedVersion.LoadCompleted();
                }
            }
            private static void LoadFromTextUsingReflection(object data, string textToLoadFrom)
            {
                PropertyInfo[] properties = data.GetType().GetProperties();

                List<string> simpleText;
                List<List<string>> multiLineText;
                List<Point> multiLineTextPosition;
                TextManager.SeparateEncapsulatedText(new List<string>() { textToLoadFrom }, out simpleText, out multiLineText, "{", "}", out multiLineTextPosition);

                // Load one liner entries:
                for (int readLines = 0; readLines < simpleText.Count; readLines++)
                {
                    string line = simpleText.ElementAt(readLines);

                    if (line.Contains("//"))
                    {
                        line = line.Remove(line.IndexOf("//"));
                    }

                    if (line.Contains('='))
                    {
                        string keyword = line.Remove(line.IndexOf('='));
                        string value = line.Remove(0, keyword.Length + 1);

                        keyword = TextManager.RemoveSurroundingSpaces(keyword);
                        value = TextManager.RemoveSurroundingQuotes(TextManager.RemoveSurroundingSpaces(value));

                        foreach (PropertyInfo property in properties)
                        {
                            Type dataType = property.PropertyType;
                            if (property.Name == keyword)
                            {
                                object dataToLoad = null;

                                if (dataType == typeof(string))
                                {
                                    dataToLoad = value;
                                }
                                else if (dataType == typeof(List<string>))
                                {
                                    List<string> valueAsList = new List<string>();
                                    valueAsList.Add(value);
                                    dataToLoad = valueAsList;
                                }

                                if (dataToLoad != null)
                                {
                                    property.SetValue(data, dataToLoad);
                                    break;
                                }
                            }
                        }
                    }
                }

                // Load string lists (multi line entries):
                for (int iii = 0; iii < multiLineText.Count; iii++)
                {
                    Point pos = multiLineTextPosition.ElementAt(iii);

                    int lineToFindKeyword = pos.Y;
                    string keyword = "";
                    while (keyword == "" && lineToFindKeyword > 0)
                    {
                        keyword = simpleText.ElementAt(lineToFindKeyword);
                        keyword = TextManager.RemoveSurroundingSpaces(keyword);
                        lineToFindKeyword++;
                    }
                    List<string> value = multiLineText.ElementAt(iii);

                    foreach (PropertyInfo property in properties)
                    {
                        Type dataType = property.PropertyType;
                        if (dataType == typeof(List<string>) && property.Name == keyword)
                        {
                            property.SetValue(data, value);
                            break;
                        }
                    }
                }
            }
        }

        #endregion Save systems


        #region serialization

        // Saving with inbuilt serialization (requires that you mark object with SerializableAttribute that is: write "[Serializable]" on the line before the class declaration.


        #region Serialization Help

        /// <summary>
        /// The <see cref="SerializationHelper"/> and <see cref="AdvSerializable"/> special case properties that have this type to allow a property to handle a field's serialization and deserialization.
        /// </summary>
        public class SerializationOverride
        {
            public string Name { get; }
            /// <summary>
            /// The type of the Value object. This will depend on the deserialization method.
            /// </summary>
            public Type ObjectType { get; }
            /// <summary>
            /// An object that contains information about the deserialized data. The object type depends on the deserialization method.
            /// 
            /// This should be set for serialization.
            /// </summary>
            public object Value { get; }
            protected Func<Type, object> ConvertCallback { get; }
            public bool Ignore { get; } = false;

            /// <summary>
            /// Create a new object to pass a value for serialization purposes.
            /// </summary>
            /// <param name="value">Value to use when serializing the property.</param>
            /// <param name="ignore">True if the name should not be serialized.</param>
            public SerializationOverride(object value, bool ignore = false)
            {
                Value = value;
                Ignore = ignore;
            }
            public SerializationOverride(string name, Type type, object value, Func<Type, object> convertCallback = null)
            {
                Name = name;
                ObjectType = type;
                Value = value;
                ConvertCallback = convertCallback;
            }

            public T GetValue<T>()
            {
                object value = GetValue(typeof(T));
                if (value == null)
                    return default(T);
                else
                    return (T)value;
            }
            public object GetValue(Type type)
            {
                if (ConvertCallback != null)
                    return ConvertCallback(type);
                else if (type.IsAssignableFrom(ObjectType))
                    return Value;
                return null;
            }
        }

        /// <summary>
        /// Help with correctly serializating and deserializing an object via the <see cref="ISerializable"/> interface. See <see cref="AdvSerializable"/> for more info on what is required to implement <see cref="ISerializable"/>.
        /// </summary>
        public class SerializationHelper
        {
            #region Classes

            #endregion Classes


            #region Member Variables

            /// <summary>
            /// The object that data will be serialized from or that will be updated with deserialized data.
            /// </summary>
            public object ReferenceObject { get; }
            /// <summary>
            /// Serialization info provided by the deserialization constructor <code>constructor(SerializationInfo info, StreamingContext context)</code>.
            /// </summary>
            public SerializationInfo Info { get; }
            /// <summary>
            /// Context provided by the deserialization constructor <code>constructor(SerializationInfo info, StreamingContext context)</code>.
            /// </summary>
            public StreamingContext Context { get; }
            /// <summary>
            /// True to attempt to catch all exceptions that might occur during serialization and deserialization. This will be done on a per field basis and an exception will cause the affected field to be skipped.
            /// </summary>
            public bool Safe { get; }

            #endregion Member Variables


            #region Constructors
            private SerializationHelper(SerializationInfo info, StreamingContext context, bool safe = false)
            {
                Info = info;
                Context = context;
                Safe = safe;
            }
            public SerializationHelper(object referenceObject, SerializationInfo info, StreamingContext context, bool safe = false) : this(info, context, safe)
            {
                ReferenceObject = referenceObject;
            }

            #endregion Constructors


            #region Methods

            public static T EvaluateSafe<T>(Func<T> callback)
            {
                return EvaluateSafe(callback, out bool success);
            }
            public static T EvaluateSafe<T>(Func<T> callback, out bool success)
            {
                success = false;
                try
                {
                    var value = callback();
                    success = true;
                    return value;
                }
                catch { }
                return default(T);
            }


            public static bool SafeSet<T>(ref T variable, Func<T> callback)
            {
                variable = EvaluateSafe(callback, out bool success);
                return success;
            }
            public static bool SafeSet<T>(ref T variable, string key, SerializationInfo info)
            {
                return SafeSet(ref variable, () => (T)info.GetValue(key, typeof(T)));
            }
            public bool SafeSet<T>(ref T variable, string key)
            {
                return SafeSet(ref variable, key, Info);
            }


            /// <summary>
            /// Get the set of serializable members for the class and base classes.
            /// </summary>
            /// <returns></returns>
            public IEnumerable<FieldInfo> GetSerializableMembers()
            {
                return GetSerializableMembers(ReferenceObject, Context);
            }
            /// <summary>
            /// Get the set of serializable members for the class and base classes.
            /// </summary>
            /// <param name="classObj"></param>
            /// <param name="context"></param>
            /// <returns></returns>
            public static IEnumerable<FieldInfo> GetSerializableMembers(object classObj, StreamingContext context)
            {
                return FormatterServices.GetSerializableMembers(classObj.GetType(), context)
                    .Where(mi => !Attribute.IsDefined(mi, typeof(NonSerializedAttribute)))  // Skip fields that are marked as NonSerialized.
                    .Select(mi => (FieldInfo)mi);                                           // For easier coding, treat the member as a FieldInfo object
            }

            /// <summary>
            /// Deserialize the ReferenceObject while mapping certain deserialized fields to different names.
            /// </summary>
            /// <param name="serializationLookup">Map the name of a deserialized field to a different name before updating the object with it. Map a name to <code>null</code> to ignore it.</param>
            public void DeserializeViaLookup(Dictionary<string, string> serializationLookup = null)
            {
                DeserializeViaLookup(ReferenceObject, Info, Context, serializationLookup, Safe);
            }
            /// <summary>
            /// Advanced options for deserializing an object.
            /// </summary>
            /// <param name="classObj">An instance of the class that will be updated with the deserialized values.</param>
            /// <param name="info">Serialization info provided by the deserialization constructor <code>constructor(SerializationInfo info, StreamingContext context)</code>.</param>
            /// <param name="context">Context provided by the deserialization constructor <code>constructor(SerializationInfo info, StreamingContext context)</code>.</param>
            /// <param name="serializationLookup">Map the name of a deserialized field to a different name before updating the object with it. Map a name to <code>null</code> to ignore it.</param>
            /// <param name="safe">True to attempt to catch all exceptions that might occur and just not update the fields that are affected.</param>
            public static void DeserializeViaLookup(object classObj, SerializationInfo info, StreamingContext context, Dictionary<string, string> serializationLookup = null, bool safe = false)
            {
                if (serializationLookup == null)
                    serializationLookup = new Dictionary<string, string>();

                // Get the set of serializable members for the class and base classes.
                var members = GetSerializableMembers(classObj, context);

                // Get data to deserialize:
                var e = info.GetEnumerator();

                Type classType = classObj.GetType();
                var properties = classType.GetProperties();

                bool done = false;
                while (!done)
                {
                    Action safeCallback = () =>
                    {
                        while (e.MoveNext())
                        {
                            // Console.WriteLine("Name={0}, ObjectType={1}, Value={2}", e.Name, e.ObjectType, e.Value);

                            // Allow mapping a deserialized name to another name (handles names that aren't valid in C# like names with spaces):
                            string correctedName = serializationLookup.Keys.Contains(e.Name) ? serializationLookup[e.Name] : e.Name;
                            // Names that are mapped to null are ignored:
                            if (correctedName == null)
                                continue;

                            // If there is a field with the deserialized name then set that:
                            var relevantMembers = members.Where(m => m.Name == correctedName).ToArray();
                            foreach (var field in relevantMembers)
                            {
                                // Get the value of this field from the SerializationInfo object.
                                field.SetValue(classObj, info.GetValue(e.Name, field.FieldType));
                            }

                            // Otherwise: look for a property with the deserialized name:
                            if (relevantMembers.Length == 0)
                            {
                                foreach (var prop in properties.Where(p => p.Name == correctedName && p.CanWrite))
                                {
                                    if (prop.PropertyType == typeof(SerializationOverride))
                                    {
                                        // If the propery takes a SerializationOverride class then give it that directly for better control:
                                        string name = e.Name;
                                        prop.SetValue(classObj, new SerializationOverride(e.Name, e.ObjectType, e.Value, (type) => info.GetValue(name, type)));
                                    }
                                    else
                                    {
                                        // Otherwise: get a value that matches the property's type from the SerializationInfo object.
                                        prop.SetValue(classObj, info.GetValue(e.Name, prop.PropertyType));
                                    }
                                }
                            }
                        }
                        done = true;
                    };
                    if (safe)
                    {
                        try
                        {
                            safeCallback();
                        }
                        catch { }
                    }
                    else
                    {
                        safeCallback();
                    }
                }
            }


            public void SerializeViaLookup(Dictionary<string, string> serializationLookup)
            {
                SerializeViaLookup(ReferenceObject, Info, Context, serializationLookup, Safe);
            }
            public static void SerializeViaLookup(object classObj, SerializationInfo info, StreamingContext context, Dictionary<string, string> serializationLookup = null, bool safe = false, bool ignoreNull = false)
            {
                if (serializationLookup == null)
                    serializationLookup = new Dictionary<string, string>();

                // Get the set of serializable members for the class and base classes.
                var members = GetSerializableMembers(classObj, context);

                // Names to use when serialized
                var keys = members
                    .Select(m => m.Name)
                    .Where(key => !serializationLookup.Values.Contains(key))
                    .Union(serializationLookup.Keys).ToList();

                // Properties to get values from:
                Type classType = classObj.GetType();
                var properties = classType.GetProperties();


                bool done = false;
                while (!done)
                {
                    Action safeCallback = () =>
                    {
                        while (keys.Count > 0)
                        {
                            var key = keys[0];
                            keys.RemoveAt(0);

                            string correctedName = serializationLookup.Keys.Contains(key) ? serializationLookup[key] : key;

                            if (correctedName == null)
                                continue;

                            var relevantMembers = members.Where(m => m.Name == correctedName).ToArray();
                            if (relevantMembers.Length > 0)
                            {
                                foreach (var field in relevantMembers)
                                {
                                    var value = field.GetValue(classObj);
                                    if (!ignoreNull || value != null)
                                        info.AddValue(key, value);
                                }
                            }
                            else
                            {
                                foreach (var prop in properties.Where(p => p.Name == correctedName && p.CanRead))
                                {
                                    object value;
                                    if (prop.PropertyType == typeof(SerializationOverride))
                                    {
                                        var obj = prop.GetValue(classObj) as SerializationOverride;
                                        value = obj?.Value;
                                        if (obj != null && obj.Ignore)
                                            continue;
                                    }
                                    else
                                        value = prop.GetValue(classObj);

                                    if (!ignoreNull || value != null)
                                        info.AddValue(key, value);
                                }
                            }
                        }
                        done = true;
                    };
                    if (safe)
                    {
                        try
                        {
                            safeCallback();
                        }
                        catch { }
                    }
                    else
                    {
                        safeCallback();
                    }
                }
            }

            #endregion Methods


            #region Properties

            #endregion Properties
        }

        /// <summary>
        /// Allows for advanced serialization features such as custom names for variables.
        /// To use:
        /// <list type="bullet">
        /// <item>optionally <code>override void GetSerializationLookup</code></item>
        /// <item>create a <code>protected constructor(SerializationInfo info, StreamingContext context)</code> and call the "Deserialize" function from it.</item>
        /// <item>add the <code>[Serializable]</code> attribute to the class.</item>
        /// </list>
        /// 
        /// Note: if there is a field with a type that doesn't have the <code>[Serializable]</code> attribute then make sure it is marked with the <code>[NonSerialized]</code> attribute or the deserialization constructor might not be called correctly.
        /// Note: the XML serializer will ignore these configurations, you should implement <see cref="System.Xml.Serialization.XmlSerializer"/> instead.
        /// </summary>
        [Serializable]
        public abstract class AdvSerializable : ISerializable
        {
            [NonSerialized]
            protected bool safeSerialization = false;
            protected virtual bool SafeSerialization { get { return safeSerialization; } set { safeSerialization = value; } }
            [NonSerialized]
            protected bool safeDeserialization = false;
            protected virtual bool SafeDeserialization { get { return safeDeserialization; } set { safeDeserialization = value; } }

            [NonSerialized]
            protected bool ignoreNullAtSerialization = false;
            protected virtual bool IgnoreNullAtSerialization { get { return ignoreNullAtSerialization; } set { ignoreNullAtSerialization = value; } }

            /// <summary>
            /// Get info to use for serialization.
            /// </summary>
            /// <param name="isSerializing">True if the info will be used for serializing the data. False if the info will be used to deserialize the data.</param>
            /// <returns>Key: alias in serialized data. Value: name of field or property to get and set serialized data to (Tip: use nameof(variable)). Use a property with the "SaveManager.SerializationOverride" type to manualy handle serialization for a specific variable name.</returns>
            protected virtual Dictionary<string, string> GetSerializationLookup(bool isSerializing)
            { return null; }

            public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                SerializationHelper.SerializeViaLookup(this, info, context, GetSerializationLookup(true), SafeSerialization, IgnoreNullAtSerialization);
            }
            /// <summary>
            /// Call from a constructor with the same parameters.
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            protected virtual void Deserialize(SerializationInfo info, StreamingContext context)
            {
                SerializationHelper.DeserializeViaLookup(this, info, context, GetSerializationLookup(false), SafeDeserialization);
            }
        }

        #endregion Serialization Help


        #region XML serialization

        public static string Serialize(object dataToSerialize)
        {
            if (dataToSerialize == null) return null;

            using (System.IO.StringWriter stringwriter = new System.IO.StringWriter())
            {
                var serializer = new System.Xml.Serialization.XmlSerializer(dataToSerialize.GetType());
                serializer.Serialize(stringwriter, dataToSerialize);
                return stringwriter.ToString();
            }
        }


        public static T Deserialize<T>(string xmlText)
        {
            if (String.IsNullOrWhiteSpace(xmlText)) return default(T);

            using (System.IO.StringReader stringReader = new System.IO.StringReader(xmlText))
            {
                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
                return (T)serializer.Deserialize(stringReader);
            }
        }

        public static object Deserialize(string xmlText, Type type)
        {
            if (String.IsNullOrWhiteSpace(xmlText)) return null;

            using (System.IO.StringReader stringReader = new System.IO.StringReader(xmlText))
            {
                var serializer = new System.Xml.Serialization.XmlSerializer(type);
                return serializer.Deserialize(stringReader);
            }
        }

        #endregion XML serialization


        #region Binary serialization

        public static byte[] SerializeToBinary(object dataToSerialize)
        {
            using (var stream = new System.IO.MemoryStream())
            {
                SerializeToBinary(dataToSerialize, stream);
                return stream.GetBuffer();
            }
        }

        public static void SerializeToBinary(object dataToSerialize, System.IO.Stream streamToSerializeTo)
        {
            if (!dataToSerialize.GetType().IsSerializable)
                throw new ArgumentException("The type must be serializable.", "dataToSerialize");


            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            formatter.Serialize(streamToSerializeTo, dataToSerialize);
        }


        public static T DeserializeFromBinary<T>(byte[] data)
        {
            return (T)DeserializeFromBinary(data);
        }
        public static object DeserializeFromBinary(byte[] data)
        {
            using (var stream = new System.IO.MemoryStream(data))
            {
                return DeserializeFromBinary(stream);
            }
        }

        public static T DeserializeFromBinary<T>(System.IO.Stream dataStream)
        {
            return (T)DeserializeFromBinary(dataStream);
        }
        public static object DeserializeFromBinary(System.IO.Stream dataStream)
        {
            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            return formatter.Deserialize(dataStream);
        }

        #endregion Binary serialization

        #endregion serialization
    }
}


#region save system 1 comments

/* Example of how to get and set values from this manager:

        public enum DataID
        {
            SettingsName,
            TaskMaxRAMUsage,
            TaskMinRAMForRaw,
            TaskMinCompressSize,
            AskBackupOverwrite,
            UseSevenZipSharp,
            DynamicCompressionOfBackups,
            CompressBackupsAfterCompletion,
            UIShowLogAtStartup,
            UIShowTasksFromDisabledLists,
        }

        public List<string>[] PropertyValues
        {
            get
            {
                List<string>[] values = SaveManager.LoadFromText(null, typeof(DataID));    // initiate lists for all values.

                lock (propertiesLocker)
                {   // Must be locked since some properties depend on others.

                    // Standard command: values[(int)DataID.].Add(;
                    values[(int)DataID.SettingsName].Add(SettingsName);
                    values[(int)DataID.AskBackupOverwrite].Add(AskBackupOverwrite.ToString());
                    values[(int)DataID.CompressBackupsAfterCompletion].Add(CompressBackupsAfterCompletion.ToString());
                    values[(int)DataID.DynamicCompressionOfBackups].Add(DynamicCompressionOfBackups.ToString());
                    values[(int)DataID.TaskMaxRAMUsage].Add(TaskMaxRAMUsage.ToString());
                    values[(int)DataID.TaskMinCompressSize].Add(TaskMinCompressSize.ToString());
                    values[(int)DataID.TaskMinRAMForRaw].Add(TaskMinRAMForRaw.ToString());
                    values[(int)DataID.UseSevenZipSharp].Add(UseSevenZipSharp.ToString());
                    values[(int)DataID.UIShowLogAtStartup].Add(ShowLogAtStartup.ToString());
                    values[(int)DataID.UIShowTasksFromDisabledLists].Add(ShowTasksFromDisabledLists.ToString());
                }
                return values;
            }
            set
            {
                SaveManager.EnsureValuesForAll(ref value, PropertyValues);  // Make sure there are values for all. If there isn't set it to the original value.

                // Standard command:  = value[(int)DataID.][0];
                SettingsName = value[(int)DataID.SettingsName][0];
                AskBackupOverwrite = bool.Parse(value[(int)DataID.AskBackupOverwrite][0]);
                CompressBackupsAfterCompletion = bool.Parse(value[(int)DataID.CompressBackupsAfterCompletion][0]);
                DynamicCompressionOfBackups = bool.Parse(value[(int)DataID.DynamicCompressionOfBackups][0]);
                TaskMaxRAMUsage = long.Parse(value[(int)DataID.TaskMaxRAMUsage][0]);
                TaskMinCompressSize = long.Parse(value[(int)DataID.TaskMinCompressSize][0]);
                taskMinRAMForRaw = long.Parse(value[(int)DataID.TaskMinRAMForRaw][0]);
                UseSevenZipSharp = bool.Parse(value[(int)DataID.UseSevenZipSharp][0]);
                ShowLogAtStartup = bool.Parse(value[(int)DataID.UIShowLogAtStartup][0]);
                ShowTasksFromDisabledLists = bool.Parse(value[(int)DataID.UIShowTasksFromDisabledLists][0]);
            }
        }


    */


#endregion save system 1 comments


/*  Interesting code:

    // Could be useful to for example call a function with a variable type requirement.
    private static dynamic ConvertObjectToType(object objectToConvert, Type typeToConvertTo)
    {
        return Convert.ChangeType(objectToConvert, typeToConvertTo);
    }
    
*/
