using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace VirtualDesktopServer
{
    internal static class Utils
    {
        /// <summary>
        /// Copy a part of an array to a different index.
        /// </summary>
        /// <param name="array">The array whose data should be moved.</param>
        /// <param name="sourceIndex">Index to start copy data from.</param>
        /// <param name="length">Length of the data that should be copied.</param>
        /// <param name="destinationIndex">Index to start writing data to.</param>
        public static void CopyWithin(byte[] array, int sourceIndex, int length, int destinationIndex)
        {
            if (sourceIndex < destinationIndex)
            {
                // We are possibly overwriting the end of the source data, so read that first.
                for (int iii = length - 1; iii >= 0; iii--)
                {
                    array[destinationIndex + iii] = array[sourceIndex + iii];
                }
            }
            else
            {
                for (int iii = 0; iii < length; iii++)
                {
                    array[destinationIndex + iii] = array[sourceIndex + iii];
                }
            }
        }

        /// <summary>
        /// Convert a 64 bit integer (<see cref="long"/>) to an <see cref="IntPtr"/>. The value could be truncuated on 32 bit platforms.
        /// </summary>
        /// <param name="value">The value to convert into an <see cref="IntPtr"/>.</param>
        /// <returns>The <see cref="IntPtr"/> that was created from the provided value.</returns>
        public static IntPtr ToLossyIntPtr(this long value)
        {
            if (IntPtr.Size == 4)
                return new IntPtr((int)value);
            else
                return new IntPtr(value);
        }

        public static Virtualdesktop.Rectangle Convert(this Rectangle rectangle)
        {
            return new Virtualdesktop.Rectangle()
            {
                Top = rectangle.Top,
                Left = rectangle.Left,
                Width = rectangle.Width,
                Height = rectangle.Height,
            };
        }

        public static Rectangle Convert(this Virtualdesktop.Rectangle rectangle)
        {
            return new Rectangle(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
        }

        public static async Task ReadFromInput(Stream input, CancellationTokenSource cancellationTokenSource, Func<int, Task<bool>> allowedMessageSize, Func<byte[], int, int, Task> handleMessage)
        {
            var buffer = new byte[1024];
            int totalReadBytes = 0;

            while (!cancellationTokenSource.IsCancellationRequested)
            {
                // Read until full message has been received:
                int messageLength = 0;
                int skippedLength = 0;
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    // Parse message length:
                    if (messageLength == 0 && totalReadBytes >= 4)
                    {
                        if (!BitConverter.IsLittleEndian)
                            Array.Reverse(buffer, 0, 4);
                        messageLength = BitConverter.ToInt32(buffer, 0);

                        if (messageLength == 0)
                        {
                            totalReadBytes -= 4;
                            Utils.CopyWithin(buffer, 4, totalReadBytes, 0);
                            continue;
                        }

                        if (allowedMessageSize != null && !await allowedMessageSize(messageLength))
                            skippedLength = 4;
                        else if (buffer.Length < messageLength)
                            buffer = new byte[messageLength];

                        if (cancellationTokenSource.IsCancellationRequested)
                            break;
                    }
                    // If we have read the whole message then break:
                    if (messageLength != 0 && totalReadBytes - 4 >= messageLength)
                        break;
                    // Ignore all currently read bytes (will use whole buffer for read):
                    if (skippedLength > 0)
                        skippedLength = totalReadBytes;
                    // Read more data:
                    var startOffset = totalReadBytes - skippedLength;
                    var readBytes = await input.ReadAsync(buffer, startOffset, buffer.Length - startOffset, cancellationTokenSource.Token);
                    if (readBytes == 0) return;
                    totalReadBytes += readBytes;
                }
                if (cancellationTokenSource.IsCancellationRequested)
                    break;

                if (skippedLength == 0)
                {
                    await handleMessage(buffer, 4, messageLength);
                }

                // Remove current message from buffer:
                int leftOver = totalReadBytes - 4 - messageLength;
                int usedBufferLength = totalReadBytes - skippedLength;
                Utils.CopyWithin(buffer, usedBufferLength - leftOver, leftOver, 0);
                totalReadBytes -= 4 + messageLength;
            }
            cancellationTokenSource.Token.ThrowIfCancellationRequested();
        }

        public enum OutputMessageSizeCheck
        {
            Allow,
            Resized,
            Cancel,
        }

        public static async Task WriteToOutput<T>(
            Stream output,
            CancellationTokenSource cancellationTokenSource,
            ChannelReader<T> messageChannel,
            Func<int, T, Task<OutputMessageSizeCheck>> checkMessageSize
        ) where T : IMessage
        {
            var buffer = new byte[1024];
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                // Get the next queued response:
                T message = await messageChannel.ReadAsync(cancellationTokenSource.Token);
                if (cancellationTokenSource.IsCancellationRequested)
                    break;
                int size = message.CalculateSize() + 4;

                // Handle too large responses:
                if (checkMessageSize != null)
                {
                    var result = await checkMessageSize(size - 4, message);
                    switch (result) {
                        case OutputMessageSizeCheck.Resized:
                            size = message.CalculateSize() + 4;
                            break;
                        case OutputMessageSizeCheck.Cancel:
                            continue;
                        default:
                        case OutputMessageSizeCheck.Allow:
                            break;
                    }
                }
                if (cancellationTokenSource.IsCancellationRequested)
                    break;
                if (buffer.Length < size)
                    buffer = new byte[size];

                // Write the response to the output:
                {
                    // Write size in first 4 bytes (little endian):
                    int messageLength = size - 4;
                    buffer[0] = (byte)messageLength;
                    buffer[1] = (byte)(messageLength >> 8);
                    buffer[2] = (byte)(messageLength >> 0x10);
                    buffer[3] = (byte)(messageLength >> 0x18);
                }
                message.WriteTo(new Span<byte>(buffer, 4, size - 4));
                await output.WriteAsync(buffer, 0, size, cancellationTokenSource.Token);
            }
        }
    }
}
