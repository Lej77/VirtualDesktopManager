using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VirtualDesktopManager.Extensions
{
    public static class Extensions
    {
        /// <summary>
        /// Make a deep copy of a serializable object.
        /// </summary>
        /// <typeparam name="T">The type of object to copy.</typeparam>
        /// <param name="source">Object to copy.</param>
        /// <returns>>A deep copy of the provided object.</returns>
        public static T DeepCopyWithSerialization<T>(this T source)
        {
            if (ReferenceEquals(source, null))
                return default(T);

            if (!source.GetType().IsSerializable)
                throw new ArgumentException("The type must be serializable.", "source");

            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            using (var stream = new System.IO.MemoryStream())
            {
                formatter.Serialize(stream, source);
                stream.Seek(0, System.IO.SeekOrigin.Begin);
                return (T)formatter.Deserialize(stream);
            }
        }

        /// <summary>
        /// Compare two objects and determine if they have the same values.
        /// </summary>
        /// <typeparam name="T">The type of object to compare.</typeparam>
        /// <param name="a">The first object to compare.</param>
        /// <param name="b">The second object to compare.</param>
        /// <returns>True if the objects are equal otherwise false.</returns>
        public static bool EqualCompareWithSerialization<T>(this T a, T b)
        {
            // check references for equality:
            if (ReferenceEquals(a, b))
                return true;

            bool aIsNull = ReferenceEquals(a, null);
            bool bIsNull = ReferenceEquals(b, null);

            if (aIsNull && bIsNull)
                return true;
            else if (aIsNull || bIsNull)
                return false;

            // Serialize and check data:
            if (!a.GetType().IsSerializable)
                throw new ArgumentException("The type must be serializable.", "a");
            if (!b.GetType().IsSerializable)
                throw new ArgumentException("The type must be serializable.", "b");

            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            using (var streamA = new System.IO.MemoryStream()) using (var streamB = new System.IO.MemoryStream())
            {
                formatter.Serialize(streamA, a);
                formatter.Serialize(streamB, b);
                return streamA.GetBuffer().SequenceEqual(streamB.GetBuffer());
            }
        }


        /// <summary>
        /// Safely raises an event. Makes a local copy (the parameter is a local variable) and null-checks it.
        /// Drawback is method overhead and creating event args if there are no subscribers. 
        /// The second issue can be prevented by checking if event is null before raising it but that adds another null-check if the event has subscribers.
        /// </summary>
        /// <typeparam name="T">The type of the event args.</typeparam>
        /// <param name="handler">The event to raise.</param>
        /// <param name="sender">The sender to raise the event with.</param>
        /// <param name="args">The arguments to raise the event with.</param>
        public static void Raise<T>(this EventHandler<T> handler, object sender, T args) // where T : EventArgs
        {
            // Same thing as local copy because the parameter is a local variable. This is slightly less efficient than ordinary local copy because of the method overhead, 
            // and you have to pass in an event args (which probably needs to be constructed) regardless of whether a subscriber is attached or not.

            if (handler != null) handler(sender, args);
        }

        /// <summary>
        /// Safely raises an event in another thread. Makes a local copy (the parameter is a local variable) and null-checks it.
        /// Drawback is method overhead and creating event args if there are no subscribers. 
        /// The second issue can be prevented by checking if event is null before raising it but that adds another null-check if the event has subscribers.
        /// </summary>
        /// <typeparam name="T">The type of the event args.</typeparam>
        /// <param name="handler">The event to raise.</param>
        /// <param name="sender">The sender to raise the event with.</param>
        /// <param name="args">The arguments to raise the event with.</param>
        public static void RaiseParallel<T>(this EventHandler<T> handler, object sender, T args) // where T : EventArgs
        {
            // Same thing as local copy because the parameter is a local variable. This is slightly less efficient than ordinary local copy because of the method overhead, 
            // and you have to pass in an event args (which probably needs to be constructed) regardless of whether a subscriber is attached or not.

            if (handler != null)
                Task.Run(delegate ()
                {
                    handler(sender, args);
                });
        }


        /// <summary>
        /// If the task takes longer then the timout time this will throw an exception. Otherwise it will await the task.
        /// </summary>
        /// <param name="taskToAwait">The task to wait for.</param>
        /// <param name="timeout">Time to wait for the task before throwing an exception instead.</param>
        public async static Task TimeoutAwaitAfter(this Task taskToAwait, int timeoutInMilliseconds)
        {
            await TimeoutAwaitAfter(taskToAwait, TimeSpan.FromMilliseconds(timeoutInMilliseconds));
        }
        /// <summary>
        /// If the task takes longer then the timout time this will throw an exception. Otherwise it will await the task.
        /// </summary>
        /// <param name="taskToAwait">The task to wait for.</param>
        /// <param name="timeout">Time to wait for the task before throwing an exception instead.</param>
        public async static Task TimeoutAwaitAfter(this Task taskToAwait, TimeSpan timeout)
        {
            if (taskToAwait == await Task.WhenAny(taskToAwait, Task.Delay(timeout)))
            {
                // task completed within timeout
                await taskToAwait;
            }
            else
            {
                // timeout logic
                throw new TimeoutException($"Operation did not finish in {timeout.TotalMilliseconds} ms");
            }
        }

        /// <summary>
        /// If the task takes longer then the timout time this will throw an exception. Otherwise it will return with the result of the task.
        /// </summary>
        /// <typeparam name="TResult">Tasks return type.</typeparam>
        /// <param name="taskToAwait">The task to wait for.</param>
        /// <param name="timeout">Time to wait for the task before throwing an exception instead.</param>
        /// <returns>The awaited task's return value.</returns>
        public async static Task<TResult> TimeoutAwaitAfter<TResult>(this Task<TResult> taskToAwait, int timeoutInMilliseconds)
        {
            return await TimeoutAwaitAfter<TResult>(taskToAwait, TimeSpan.FromMilliseconds(timeoutInMilliseconds));
        }
        /// <summary>
        /// If the task takes longer then the timout time this will throw an exception. Otherwise it will return with the result of the task.
        /// </summary>
        /// <typeparam name="TResult">Tasks return type.</typeparam>
        /// <param name="taskToAwait">The task to wait for.</param>
        /// <param name="timeout">Time to wait for the task before throwing an exception instead.</param>
        /// <returns>The awaited task's return value.</returns>
        public async static Task<TResult> TimeoutAwaitAfter<TResult>(this Task<TResult> taskToAwait, TimeSpan timeout)
        {
            if (taskToAwait == await Task.WhenAny(taskToAwait, Task.Delay(timeout)))
            {
                // task completed within timeout
                return await taskToAwait;
            }
            else
            {
                // timeout logic
                throw new TimeoutException($"Operation did not finish in {timeout.TotalMilliseconds} ms");
            }
        }


        /// <summary>
        /// Creates a rich text box helper for a provided rich text box.
        /// </summary>
        /// <param name="richTextBoxToManage">The rich text box that the helper should manage.</param>
        /// <returns>A rich text box helper that manages the provided rich text box.</returns>
        public static Utils.RichTextBoxHelper CreateHelper(this System.Windows.Forms.RichTextBox richTextBoxToManage)
        {
            return new Utils.RichTextBoxHelper(richTextBoxToManage);
        }
    }

    namespace Enums
    {
        /// <summary>
        /// Contains extensions methods that are offered by this library.
        /// </summary>
        public static class Extensions
        {
            /// <summary>
            /// Get a comma seperated list of bitwise flags for an enum that uses bitwise flags.
            /// </summary>
            /// <param name="flagValue">The current flags that are set.</param>
            /// <returns>A comma separated list of the names of the set flags.</returns>
            public static string PrintFlags(this Enum flagValue)
            {
                return PrintFlags(GetFlags(flagValue));
            }
            /// <summary>
            /// Get a comma seperated list of bitwise flags for an enum that uses bitwise flags.
            /// </summary>
            /// <param name="flags">All enum values that represent flags that are set.</param>
            /// <returns>A comma separated list of the names of the set flags.</returns>
            public static string PrintFlags(this Enum[] flags)
            {
                return String.Join(", ", String.Join(", ", flags.Select(flag => flag.ToString())).Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries).Distinct());
            }
            /// <summary>
            /// Use on an enum that uses bitwise flags to determine what flags are set.
            /// </summary>
            /// <param name="flagValue">The current flags that are set.</param>
            /// <returns>All flags that are set.</returns>
            public static Enum[] GetFlags(this Enum flagValue)
            {
                return flagValue.GetType().GetEnumValues().Cast<Enum>().Where(flag => flagValue.HasFlag(flag)).Distinct().ToArray();
            }

            /// <summary>
            /// If the combo box has enum names as values then gets the enum with the same name.
            /// Will throw exception if the SelectedItem in the combo box is null.
            /// </summary>
            /// <typeparam name="T">The type of enum to match against the items in the combo box.</typeparam>
            /// <param name="comboBox">The combo box with the selected item to match against an enum.</param>
            /// <returns>The enum matching the name if the selected item in the combi box.</returns>
            public static T GetEnum<T>(this ComboBox comboBox)
            {
                return (T)Enum.Parse(typeof(T), comboBox.SelectedItem.ToString());
            }
            public static void SetEnum<T>(this ComboBox comboBox, T value)
            {
                var index = -1;
                for (int iii = 0; iii < comboBox.Items.Count; iii++)
                {
                    var itemValue = (T)Enum.Parse(typeof(T), comboBox.Items[iii].ToString());
                    if (itemValue.ToString() == value.ToString())
                    {
                        index = iii;
                        break;
                    }
                }
                if (index >= 0)
                    comboBox.SelectedItem = comboBox.Items[index];
            }
        }
    }
}
