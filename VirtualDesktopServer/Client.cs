using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Google.Protobuf;
using System.Security.Authentication.ExtendedProtection;
using System.Threading.Channels;
using System.Runtime.InteropServices.ComTypes;
using Google.Protobuf.WellKnownTypes;
using System.Reflection;
using System.Linq;
using Virtualdesktop;

namespace VirtualDesktopServer
{
    /// <summary>
    /// Implements the <see cref="IVirtualDesktop"/> interface by sending commands and queries to a server via protocol buffers.
    /// </summary>
    public class Client : IDisposable, IVirtualDesktop
    {
        #region Classes

        private class ResponseHandler
        {
            public TaskCompletionSource<Virtualdesktop.Response> SingleResponse;
            public ChannelWriter<Virtualdesktop.Response> MutlipleResponses;
            public CancellationTokenSource WhenCompleted;
        }

        #endregion Classes


        #region Member Variables

        /// <summary>
        /// Read the servers responses from this stream.
        /// </summary>
        public Stream Input { get; }
        /// <summary>
        /// Write requests to the server via this stream.
        /// </summary>
        public Stream Output { get; }

        private readonly CancellationTokenSource cancellationTokenSource = null;
        /// <summary>
        /// Requests that are queued to be sent to the server.
        /// </summary>
        private Channel<Virtualdesktop.Request> queuedRequests = Channel.CreateBounded<Virtualdesktop.Request>(new BoundedChannelOptions(100) { SingleReader = true });

        private readonly object activeRequestsLocker = new object();
        /// <summary>
        /// Data associated with an active request. Key is a request's id.
        /// </summary>
        private Dictionary<uint, ResponseHandler> activeRequests = new Dictionary<uint, ResponseHandler>();
        /// <summary>
        /// The id to use for the next request.
        /// 
        /// Use only even numbers so that odd numbers can be used for requests that are initiated by the server. 0 is reserved for stateless requests that don't need a response.
        /// </summary>
        private uint currentRequestId = 2;

        /// <summary>
        /// The maximum size of messages to send to the server for a request.
        /// </summary>
        public volatile int MaxRequestSize = 1_000_000;
        /// <summary>
        /// The maximum size to accept for messages sent by the server.
        /// </summary>
        public volatile int MaxResponseSize = 1_000_000;

        /// <summary>
        /// Invoked when a response read from the input stream could not be parsed correctly.
        ///
        /// Maybe the client was based on a different incompatible protobuf schema or maybe the server has a bug.
        /// </summary>
        public event EventHandler<EventArgs> OnResponseParseError;
        /// <summary>
        /// Invoked when a response was ignored due to its message size.
        /// </summary>
        public event EventHandler<EventArgs> OnTooLargeResponse;

        /// <summary>
        /// The client received a response to a request id that was unknown. This probably indicates a bug in the server or client.
        /// </summary>
        public event EventHandler<EventArgs> OnResponseToUnknownRequest;

        /// <summary>
        /// Did not receive a single response for a request which expected only one response. This probably indicates a bug in the server or client.
        /// </summary>
        public event EventHandler<EventArgs> OnMultipleResponsesToSingleResponseRequest;

        /// <summary>
        /// The server ignored one or more requests that were sent from this client. This could be because the sent requests were too large but it is more likely because of a bug or because someone else was also writing to the output stream.
        /// </summary>
        public event EventHandler<EventArgs> OnServerDroppedRequest;

        /// <summary>
        /// The client encountered an error when handling messages.
        /// 
        /// This is not invoked for error responses from the server, instead this indicates a larger issue.
        /// </summary>
        public event EventHandler<EventArgs> OnClientError;

        #endregion Member Variables


        #region Constructors

        public Client(Stream input, Stream output, CancellationToken[] cancelTokens = null)
        {
            Input = input ?? throw new ArgumentNullException("input");
            Output = output ?? throw new ArgumentNullException("output");

            if (cancelTokens != null && cancelTokens.Length > 0)
                cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancelTokens);
            else
                cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.Token.Register(() =>
            {
                ResponseHandler[] handlers;
                lock (activeRequestsLocker)
                {
                    handlers = activeRequests.Values.ToArray();
                    activeRequests.Clear();
                }
                foreach (var handler in handlers)
                {
                    Exception tooLargeException = new ObjectDisposedException(nameof(Client));
                    handler.SingleResponse?.TrySetException(tooLargeException);
                    handler.MutlipleResponses?.TryComplete(tooLargeException);
                    handler.WhenCompleted?.Cancel();
                    handler.WhenCompleted?.Dispose();
                }
            });

            OnResponseParseError += (sender, e) => OnClientError?.Invoke(this, EventArgs.Empty);
            OnTooLargeResponse += (sender, e) => OnClientError?.Invoke(this, EventArgs.Empty);
            OnResponseToUnknownRequest += (sender, e) => OnClientError?.Invoke(this, EventArgs.Empty);
            OnMultipleResponsesToSingleResponseRequest += (sender, e) => OnClientError?.Invoke(this, EventArgs.Empty);
            OnServerDroppedRequest += (sender, e) => OnClientError?.Invoke(this, EventArgs.Empty);


            _ = handleInput();
            _ = handleOutput();
        }

        #endregion Constructors


        #region Methods

        #region Client Methods

        private async Task handleInput()
        {
            await Utils.ReadFromInput(
                Input,
                cancellationTokenSource,
                allowedMessageSize: (messageSize) =>
                {
                    int max = MaxResponseSize;
                    bool allowed = messageSize <= max;
                    if (!allowed)
                        OnTooLargeResponse?.Invoke(this, EventArgs.Empty);
                    return Task.FromResult(allowed);
                },
                handleMessage: async (buffer, startOffset, length) =>
                {
                    // Parse and handle request:
                    Virtualdesktop.Response response;
                    try
                    {
                        response = Virtualdesktop.Response.Parser.ParseFrom(buffer, startOffset, length);
                    }
                    catch (Exception)
                    {
                        OnResponseParseError?.Invoke(this, EventArgs.Empty);
                        return;
                    }

                    if (response.DataCase == Virtualdesktop.Response.DataOneofCase.DroppedRequest)
                    {
                        OnServerDroppedRequest?.Invoke(this, EventArgs.Empty);
                        return;
                    }

                    ResponseHandler responseHandler = GetResponseData(response.Id, response.Done);
                    if (responseHandler != null)
                    {
                        if (responseHandler.SingleResponse != null)
                        {
                            if (response.DataCase == Virtualdesktop.Response.DataOneofCase.Canceled)
                                responseHandler.SingleResponse.TrySetCanceled();
                            else if (response.DataCase == Virtualdesktop.Response.DataOneofCase.Error)
                                responseHandler.SingleResponse.TrySetException(new Exception(response.Error.ErrorMessage));
                            else
                                responseHandler.SingleResponse.TrySetResult(response);

                            if (!response.Done)
                                OnMultipleResponsesToSingleResponseRequest?.Invoke(this, EventArgs.Empty);
                        }
                        if (responseHandler.MutlipleResponses != null)
                        {
                            if (response.Done)
                            {
                                if (response.DataCase == Virtualdesktop.Response.DataOneofCase.Canceled)
                                    responseHandler.MutlipleResponses.TryComplete(new OperationCanceledException());
                                else if (response.DataCase == Virtualdesktop.Response.DataOneofCase.Error)
                                    responseHandler.MutlipleResponses.TryComplete(new Exception(response.Error.ErrorMessage));
                                else
                                {
                                    responseHandler.MutlipleResponses.TryComplete();
                                    await responseHandler.MutlipleResponses.WriteAsync(response);
                                }
                            }
                            else
                            {
                                await responseHandler.MutlipleResponses.WriteAsync(response);
                            }
                        }

                        if (responseHandler.SingleResponse != null || response.Done)
                        {
                            responseHandler.WhenCompleted?.Cancel();
                            responseHandler.WhenCompleted?.Dispose();
                        }
                    }
                    else
                        OnResponseToUnknownRequest?.Invoke(this, EventArgs.Empty);
                }
            );
        }

        private ResponseHandler GetResponseData(uint id, bool done)
        {
            lock (activeRequestsLocker)
            {
                if (activeRequests.TryGetValue(id, out var value))
                {
                    if (done || value.SingleResponse != null)
                        activeRequests.Remove(id);
                    return value;
                }
            }
            return null;
        }

        private bool IsRequestActive(uint id, ResponseHandler data = null)
        {
            lock (activeRequestsLocker)
            {
                if (activeRequests.TryGetValue(id, out var value))
                {
                    if (data == null || value == data)
                        return true;
                }
            }
            return false;
        }

        private void RegisterNewRequest(ResponseHandler responseHandler, out uint id)
        {
            lock (activeRequestsLocker)
            {
                if (IsDisposed) throw new ObjectDisposedException(nameof(Client));
                do
                {
                    id = currentRequestId;
                    if (currentRequestId >= uint.MaxValue - 1)
                    {
                        // Would overflow:
                        currentRequestId = 2;
                    }
                    else
                    {
                        currentRequestId += 2;
                    }
                } while (activeRequests.ContainsKey(id));
                activeRequests.Add(id, responseHandler);
            }
        }

        private async Task handleOutput()
        {
            await Utils.WriteToOutput<Virtualdesktop.Request>(
                Output,
                cancellationTokenSource,
                queuedRequests.Reader,
                checkMessageSize: (size, message) =>
                {
                    // Handle too large requests:
                    var maxSize = MaxRequestSize;
                    if (size <= maxSize)
                        return Task.FromResult(Utils.OutputMessageSizeCheck.Allow);
                    else
                    {
                        ResponseHandler responseHandler = GetResponseData(message.Id, true);
                        if (responseHandler != null)
                        {
                            Exception tooLargeException = new Exception("Request message was " + size + " bytes large which exceeded the maximum size of " + maxSize + " bytes.");
                            responseHandler.SingleResponse?.TrySetException(tooLargeException);
                            responseHandler.MutlipleResponses?.TryComplete(tooLargeException);
                            responseHandler.WhenCompleted?.Cancel();
                            responseHandler.WhenCompleted?.Dispose();
                        }

                        return Task.FromResult(Utils.OutputMessageSizeCheck.Cancel);
                    }
                }
            );
        }

        /// <summary>
        /// Try to convert a generic response to a specific response type.
        /// </summary>
        /// <typeparam name="T">The wanted response type.</typeparam>
        /// <param name="response">The generic response message.</param>
        /// <returns>The wanted response.</returns>
        private static T ToResponse<T>(Virtualdesktop.Response response)
        {
            var responseType = typeof(Virtualdesktop.Response);
            var wantedType = typeof(T);
            PropertyInfo property = null;
            foreach (var p in responseType.GetProperties())
            {
                if (p.PropertyType == wantedType)
                {
                    if (p != null)
                        throw new Exception("More than one property in the generic response type could return the wanted concrete response type. Its likely an invalid response type was specified. Wanted response type: " + wantedType.FullName);
                    property = p;
                }
            }
            if (property == null)
            {
                throw new Exception("Internal error: the wanted response type could not be found in the generated protocol buffer bindings. Wanted type: " + wantedType.FullName);
            }

            var wantedResponse = property.GetValue(response);
            if (wantedResponse == null)
            {
                throw new Exception("Unexpected response type from server: expected the response type \"" + wantedType.Name + "\" but got \"" + response.DataCase + "\".\nFull server response: " + response.ToString());
            }

            return (T)wantedResponse;
        }

        /// <summary>
        /// Send a request and expect a certain response back.
        /// </summary>
        /// <param name="request">The request to send.</param>
        /// <param name="cancellationToken">A token to that will cancel the request.</param>
        /// <returns>A task that will complete once the server has sent back a response.</returns>
        private async Task<T> SendRequestWithSingleResponse<T>(Virtualdesktop.Request request, CancellationToken cancellationToken)
        {
            return ToResponse<T>(await SendRequestWithSingleResponse(request, cancellationToken));
        }
        /// <summary>
        /// Send a request and wait to get a response back.
        /// </summary>
        /// <param name="request">The request to send.</param>
        /// <param name="cancellationToken">A token to that will cancel the request.</param>
        /// <returns>A task that will complete once the server has sent back a response.</returns>
        private async Task<Virtualdesktop.Response> SendRequestWithSingleResponse(Virtualdesktop.Request request, CancellationToken cancellationToken)
        {
            var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var singleResponse = new TaskCompletionSource<Virtualdesktop.Response>();
            var responseHandler = new ResponseHandler() { SingleResponse = singleResponse, WhenCompleted = cancellationSource, };

            RegisterNewRequest(responseHandler, out uint id);
            request.Id = id;
            await queuedRequests.Writer.WriteAsync(request);

            void cancel()
            {
                bool stillRunning = IsRequestActive(id, responseHandler);
                if (stillRunning)
                {
                    _ = queuedRequests.Writer.WriteAsync(new Virtualdesktop.Request()
                    {
                        Id = id,
                        Cancel = new Virtualdesktop.CancelRequest(),
                    });
                }
            }

            CancellationTokenRegistration cancelListener;
            try
            {
                try
                {
                    cancelListener = cancellationSource.Token.Register(cancel);
                }
                catch (Exception)
                {
                    cancel();
                    throw new OperationCanceledException();
                }
                return await singleResponse.Task;
            }
            finally
            {
                cancelListener.Dispose();
            }
        }

        /// <summary>
        /// Send a request and expect multiple responses while mapping the server response into a different type.
        /// </summary>
        /// <typeparam name="T">The type of data to return via the channel.</typeparam>
        /// <typeparam name="R">The expected response type.</typeparam>
        /// <param name="request">The request to send.</param>
        /// <param name="mapResponse">Map the expected response to the data that should be sent via the channel.</param>
        /// <param name="cancellationToken">Used to cancel the operation.</param>
        /// <returns>A channel that sends data for each received response from the server.</returns>
        private ChannelReader<T> SendRequestWithMultipleResponses<T, R>(Virtualdesktop.Request request, Func<R, T> mapResponse, CancellationToken cancellationToken)
        {
            return SendRequestWithMultipleResponses<T>(request, response => mapResponse(ToResponse<R>(response)), cancellationToken);
        }
        /// <summary>
        /// Send a request and expect multiple responses while mapping the server response into a different type.
        /// </summary>
        /// <typeparam name="T">The type of data to return via the channel.</typeparam>
        /// <param name="request">The request to send.</param>
        /// <param name="mapResponse">Map the server's response to some data that should be sent via the channel.</param>
        /// <param name="cancellationToken">Used to cancel the operation.</param>
        /// <returns>A channel that sends data for each received response from the server.</returns>
        private ChannelReader<T> SendRequestWithMultipleResponses<T>(Virtualdesktop.Request request, Func<Virtualdesktop.Response, T> mapResponse, CancellationToken cancellationToken)
        {
            // The responseChannel already has a queue so this one shouldn't need one:
            Channel<T> channel = Channel.CreateBounded<T>(0);
            // Need to cancel task if mapResponse throws:
            CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            try
            {
                var responseChannel = SendRequestWithMultipleResponses(request, cancellationTokenSource.Token);
                async void forwardMessages()
                {
                    try
                    {
                        while (await responseChannel.WaitToReadAsync(cancellationTokenSource.Token))
                            while (responseChannel.TryRead(out var response))
                            {
                                if (response.DataCase == Virtualdesktop.Response.DataOneofCase.Success)
                                {
                                    // Last response could be a generic success response.
                                    var nextResponse = await responseChannel.ReadAsync(cancellationTokenSource.Token);
                                    // If it was the last response then the above line will throw a `ChannelClosedException`.

                                    // Not last response (let map function handle it):
                                    await channel.Writer.WriteAsync(mapResponse(response), cancellationTokenSource.Token);
                                    await channel.Writer.WriteAsync(mapResponse(nextResponse), cancellationTokenSource.Token);
                                    continue;
                                }
                                await channel.Writer.WriteAsync(mapResponse(response), cancellationTokenSource.Token);
                                cancellationTokenSource.Token.ThrowIfCancellationRequested();
                            }
                        channel.Writer.TryComplete();
                    }
                    catch (ChannelClosedException)
                    {
                        channel.Writer.TryComplete();
                    }
                    catch (Exception ex)
                    {
                        channel.Writer.TryComplete(ex);
                    }
                    finally
                    {
                        // Ensure we always dispose of the responseChannel when the outer channel has completed. (maybe due to an exception)
                        cancellationTokenSource.Cancel();
                        cancellationTokenSource.Dispose();
                    }
                }
                forwardMessages();
            }
            catch
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                throw;
            }
            return channel;
        }
        /// <summary>
        /// Send a request and expect multiple responses.
        /// </summary>
        /// <param name="request">The request to send.</param>
        /// <param name="cancellationToken">Used to cancel the operation.</param>
        /// <returns>A channel that will be sent all responses that are received from the server.</returns>
        private ChannelReader<Virtualdesktop.Response> SendRequestWithMultipleResponses(Virtualdesktop.Request request, CancellationToken cancellationToken)
        {
            // Bounded channels can apply backpressure to the response parsing from server.
            Channel<Virtualdesktop.Response> channel = Channel.CreateBounded<Virtualdesktop.Response>(10);
            _ = SendRequestWithMultipleResponses(request, channel.Writer, cancellationToken);
            return channel;
        }
        /// <summary>
        /// Send a request and expect multiple responses.
        /// </summary>
        /// <param name="request">The request to send.</param>
        /// <param name="responses">A channel that will be sent all responses that are received from the server. Can apply backpressure to the server response parser task, which would prevent any server responses from being handled.</param>
        /// <param name="cancellationToken">Used to cancel the operation.</param>
        /// <returns>A task that completes after the request has been queued.</returns>
        private async Task SendRequestWithMultipleResponses(Virtualdesktop.Request request, ChannelWriter<Virtualdesktop.Response> responses, CancellationToken cancellationToken)
        {
            var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var responseHandler = new ResponseHandler() { MutlipleResponses = responses, WhenCompleted = cancellationSource, };

            RegisterNewRequest(responseHandler, out uint id);
            request.Id = id;
            void cancel()
            {
                bool stillRunning = IsRequestActive(id, responseHandler);
                if (stillRunning)
                    _ = queuedRequests.Writer.WriteAsync(new Virtualdesktop.Request()
                    {
                        Id = id,
                        Cancel = new Virtualdesktop.CancelRequest(),
                    });
            }
            try
            {
                await queuedRequests.Writer.WriteAsync(request);
            }
            catch (Exception ex)
            {
                // Since the task could still be registered and access from the response parser task its best we lock to ensure there is no concurrent access:
                responses.TryComplete(ex);
                cancel();
                throw;
            }

            try
            {
                cancellationSource.Token.Register(cancel);
            }
            catch
            {
                cancel();
            }
        }

        public void Dispose()
        {
            if (IsDisposed) return;
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
        }

        #endregion Client Methods


        #region Virtual Desktop Methods

        Task IVirtualDesktop.Log(string message, CancellationToken cancellationToken)
        {
            return SendRequestWithSingleResponse<Virtualdesktop.SuccessResponse>(new Virtualdesktop.Request()
            {
                Log = new Virtualdesktop.LogRequest()
                {
                    LogMessage = message
                }
            }, cancellationToken);
        }

        Task IVirtualDesktop.CreateVirtualDesktop(bool switchToCreatedDesktop, CancellationToken cancellationToken)
        {
            return SendRequestWithSingleResponse<Virtualdesktop.SuccessResponse>(new Virtualdesktop.Request()
            {
                CreateVirtualDesktop = new Virtualdesktop.CreateVirtualDesktopRequest()
                {
                    SwitchToTheCreatedDesktop = switchToCreatedDesktop,
                }
            }, cancellationToken);
        }

        Task IVirtualDesktop.DeleteVirtualDesktop(bool preferFallbackToTheLeft, CancellationToken cancellationToken)
        {
            return SendRequestWithSingleResponse<Virtualdesktop.SuccessResponse>(new Virtualdesktop.Request()
            {
                DeleteVirtualDesktop = new Virtualdesktop.DeleteVirtualDesktopRequest()
                {
                    PreferFallingBackToTheLeft = preferFallbackToTheLeft,
                    CurrentDesktop = new Virtualdesktop.DeleteVirtualDesktopRequestCurrentDesktop(),
                }
            }, cancellationToken);
        }

        Task IVirtualDesktop.DeleteVirtualDesktop(bool preferFallbackToTheLeft, int index, CancellationToken cancellationToken)
        {
            return SendRequestWithSingleResponse<Virtualdesktop.SuccessResponse>(new Virtualdesktop.Request()
            {
                DeleteVirtualDesktop = new Virtualdesktop.DeleteVirtualDesktopRequest()
                {
                    PreferFallingBackToTheLeft = preferFallbackToTheLeft,
                    VirtualDesktopIndex = index,
                }
            }, cancellationToken);
        }

        async Task<int> IVirtualDesktop.GetCurrentVirtualDesktopIndex(CancellationToken cancellationToken)
        {
            return (await SendRequestWithSingleResponse<Virtualdesktop.GetCurrentVirtualDesktopResponse>(new Virtualdesktop.Request()
            {
                GetCurrentVirtualDesktop = new Virtualdesktop.GetCurrentVirtualDesktopRequest(),
            }, cancellationToken)).CurrentVirtualDesktopIndex;
        }

        Task IVirtualDesktop.ChangeCurrentVirtualDesktopToIndex(bool smoothChange, int index, CancellationToken cancellationToken)
        {
            return SendRequestWithSingleResponse<Virtualdesktop.SuccessResponse>(new Virtualdesktop.Request()
            {
                ChangeVirtualDesktop = new Virtualdesktop.ChangeVirtualDesktopRequest()
                {
                    SmoothChange = smoothChange,
                    WantedVirtualDesktopIndex = index,
                }
            }, cancellationToken);
        }

        Task IVirtualDesktop.ChangeCurrentVirtualDesktopToRight(bool smoothChange, int count, CancellationToken cancellationToken)
        {
            return SendRequestWithSingleResponse<Virtualdesktop.SuccessResponse>(new Virtualdesktop.Request()
            {
                ChangeVirtualDesktop = new Virtualdesktop.ChangeVirtualDesktopRequest()
                {
                    SmoothChange = smoothChange,
                    ChangeRight = count,
                }
            }, cancellationToken);
        }

        Task IVirtualDesktop.ChangeCurrentVirtualDesktopToLeft(bool smoothChange, int count, CancellationToken cancellationToken)
        {
            return SendRequestWithSingleResponse<Virtualdesktop.SuccessResponse>(new Virtualdesktop.Request()
            {
                ChangeVirtualDesktop = new Virtualdesktop.ChangeVirtualDesktopRequest()
                {
                    SmoothChange = smoothChange,
                    ChangeLeft = count,
                }
            }, cancellationToken);
        }

        ChannelReader<VirtualDesktopChangedEventArgs> IVirtualDesktop.ListenForVirtualDesktopChanged(CancellationToken cancellationToken)
        {
            return SendRequestWithMultipleResponses<VirtualDesktopChangedEventArgs, Virtualdesktop.ListenForVirtualDesktopChangedResponse>(
                new Virtualdesktop.Request()
                {
                    ListenForVirtualDesktopChanged = new Virtualdesktop.ListenForVirtualDesktopChangedRequest(),
                },
                response =>
                {
                    return new VirtualDesktopChangedEventArgs()
                    {
                        OldCurrentVirtualDesktopIndex = response.OldCurrentVirtualDesktopIndex,
                        NewCurrentVirtualDesktopIndex = response.NewCurrentVirtualDesktopIndex,
                    };
                },
                cancellationToken
            );
        }

        ChannelReader<VirtualDesktopCreatedEventArgs> IVirtualDesktop.ListenForVirtualDesktopCreated(CancellationToken cancellationToken)
        {
            return SendRequestWithMultipleResponses<VirtualDesktopCreatedEventArgs, Virtualdesktop.ListenForVirtualDesktopCreatedResponse>(
                new Virtualdesktop.Request()
                {
                    ListenForVirtualDesktopCreated = new Virtualdesktop.ListenForVirtualDesktopCreatedRequest(),
                },
                response =>
                {
                    return new VirtualDesktopCreatedEventArgs()
                    {
                        IndexOfCreatedVirtualDesktop = response.IndexOfCreatedVirtualDesktop,
                    };
                },
                cancellationToken
            );
        }

        ChannelReader<VirtualDesktopDeletedEventArgs> IVirtualDesktop.ListenForVirtualDesktopDeleted(CancellationToken cancellationToken)
        {
            return SendRequestWithMultipleResponses<VirtualDesktopDeletedEventArgs, Virtualdesktop.ListenForVirtualDesktopDeletedResponse>(
                new Virtualdesktop.Request()
                {
                    ListenForVirtualDesktopDeleted = new Virtualdesktop.ListenForVirtualDesktopDeletedRequest(),
                },
                response =>
                {
                    return new VirtualDesktopDeletedEventArgs()
                    {
                        IndexOfDeletedVirtualDesktop = response.IndexOfDeletedVirtualDesktop,
                        IndexOfFallbackVirtualDesktop = response.IndexOfFallbackVirtualDesktop,
                    };
                },
                cancellationToken
            );
        }

        Task IVirtualDesktop.PinWindowToAllVirtualDesktops(IntPtr windowHandle, bool stopWindowFlashing, CancellationToken cancellationToken)
        {
            return SendRequestWithSingleResponse<Virtualdesktop.SuccessResponse>(new Virtualdesktop.Request()
            {
                PinWindowToAllVirtualDesktops = new Virtualdesktop.PinWindowToAllVirtualDesktopsRequest()
                {
                    WindowHandle = windowHandle.ToInt64(),
                    StopWindowFlashing = stopWindowFlashing,
                }
            }, cancellationToken);
        }

        Task IVirtualDesktop.UnpinWindowFromAllVirtualDesktops(IntPtr windowHandle, CancellationToken cancellationToken)
        {
            return SendRequestWithSingleResponse<Virtualdesktop.SuccessResponse>(new Virtualdesktop.Request()
            {
                UnpinWindowFromAllVirtualDesktops = new Virtualdesktop.UnpinWindowFromAllVirtualDesktopsRequest()
                {
                    WindowHandle = windowHandle.ToInt64(),
                    JustUnpin = new Virtualdesktop.UnpinWindowFromAllVirtualDesktopsRequestJustUnpin()
                }
            }, cancellationToken);
        }

        Task IVirtualDesktop.UnpinWindowFromAllVirtualDesktops(IntPtr windowHandle, int targetVirtualDesktopIndex, bool moveEvenIfAlreadyUnpinned, MoveWindowToVirtualDesktopOptions options, CancellationToken cancellationToken)
        {
            var unpin = new Virtualdesktop.UnpinWindowFromAllVirtualDesktopsRequest()
            {
                WindowHandle = windowHandle.ToInt64(),
            };

            if (moveEvenIfAlreadyUnpinned)
            {
                unpin.MoveUnconditionally = new Virtualdesktop.UnpinWindowFromAllVirtualDesktopsRequestMoveUnconditionally()
                {
                    TargetVirtualDesktopIndex = targetVirtualDesktopIndex,
                    MoveOptions = options,
                };
            }
            else
            {
                unpin.MoveIfUnpinned = new Virtualdesktop.UnpinWindowFromAllVirtualDesktopsRequestMoveIfUnpinned()
                {
                    TargetVirtualDesktopIndex = targetVirtualDesktopIndex,
                    MoveOptions = options,
                };
            }

            return SendRequestWithSingleResponse<Virtualdesktop.SuccessResponse>(new Virtualdesktop.Request()
            {
                UnpinWindowFromAllVirtualDesktops = unpin,
            }, cancellationToken);
        }

        Task IVirtualDesktop.MoveWindowToVirtualDesktopIndex(IntPtr windowHandle, int targetVirtualDesktopIndex, MoveWindowToVirtualDesktopOptions options, CancellationToken cancellationToken)
        {
            return SendRequestWithSingleResponse<Virtualdesktop.SuccessResponse>(new Virtualdesktop.Request()
            {
                MoveWindowToVirtualDesktop = new Virtualdesktop.MoveWindowToVirtualDesktopRequest()
                {
                    WindowHandle = windowHandle.ToInt64(),
                    MoveOptions = options,
                    WantedVirtualDesktopIndex = targetVirtualDesktopIndex,
                }
            }, cancellationToken);
        }

        Task IVirtualDesktop.MoveWindowToVirtualDesktopLeftOfCurrent(IntPtr windowHandle, int count, MoveWindowToVirtualDesktopOptions options, CancellationToken cancellationToken)
        {
            return SendRequestWithSingleResponse<Virtualdesktop.SuccessResponse>(new Virtualdesktop.Request()
            {
                MoveWindowToVirtualDesktop = new Virtualdesktop.MoveWindowToVirtualDesktopRequest()
                {
                    WindowHandle = windowHandle.ToInt64(),
                    MoveOptions = options,
                    ChangeLeft = count,
                }
            }, cancellationToken);
        }

        Task IVirtualDesktop.MoveWindowToVirtualDesktopRightOfCurrent(IntPtr windowHandle, int count, MoveWindowToVirtualDesktopOptions options, CancellationToken cancellationToken)
        {
            return SendRequestWithSingleResponse<Virtualdesktop.SuccessResponse>(new Virtualdesktop.Request()
            {
                MoveWindowToVirtualDesktop = new Virtualdesktop.MoveWindowToVirtualDesktopRequest()
                {
                    WindowHandle = windowHandle.ToInt64(),
                    MoveOptions = options,
                    ChangeRight = count,
                }
            }, cancellationToken);
        }

        async Task<int> IVirtualDesktop.GetWindowVirtualDesktopIndex(IntPtr windowHandle, CancellationToken cancellationToken)
        {
            return (await SendRequestWithSingleResponse<Virtualdesktop.GetWindowVirtualDesktopIndexResponse>(new Virtualdesktop.Request()
            {
                GetWindowVirtualDesktopIndex = new Virtualdesktop.GetWindowVirtualDesktopIndexRequest()
                {
                    WindowHandle = windowHandle.ToInt64(),
                }
            }, cancellationToken)).VirtualDesktopIndex;
        }

        async Task<bool> IVirtualDesktop.GetWindowIsPinnedToVirtualDesktop(IntPtr windowHandle, CancellationToken cancellationToken)
        {
            return (await SendRequestWithSingleResponse<Virtualdesktop.GetWindowIsPinnedToVirtualDesktopResponse>(new Virtualdesktop.Request()
            {
                GetWindowIsPinnedToVirtualDesktop = new Virtualdesktop.GetWindowIsPinnedToVirtualDesktopRequest()
                {
                    WindowHandle = windowHandle.ToInt64(),
                }
            }, cancellationToken)).IsPinned;
        }

        ChannelReader<QueryOpenWindowsInfo> IVirtualDesktop.QueryOpenWindows(QueryOpenWindowsFilter filter, QueryOpenWindowsWantedData wantedData, CancellationToken cancellationToken)
        {
            var query = new Virtualdesktop.QueryOpenWindowsRequest()
            {
                WantedDataSpecifier = wantedData,
                WindowsFilter = filter,
            };

            return SendRequestWithMultipleResponses<QueryOpenWindowsInfo, Virtualdesktop.QueryOpenWindowsResponse>(
                new Virtualdesktop.Request()
                {
                    OpenWindowsQuery = query,
                },
                response =>
                {
                    var info = new QueryOpenWindowsInfo()
                    {
                        WindowHandle = response.WindowHandle.ToLossyIntPtr(),
                    };
                    if (response.OptionalParentWindowHandleCase == Virtualdesktop.QueryOpenWindowsResponse.OptionalParentWindowHandleOneofCase.ParentWindowHandle)
                        info.ParentWindowHandle = response.ParentWindowHandle.ToLossyIntPtr();
                    if (response.OptionalRootParentWindowHandleCase == Virtualdesktop.QueryOpenWindowsResponse.OptionalRootParentWindowHandleOneofCase.RootParentWindowHandle)
                        info.RootParentWindowHandle = response.RootParentWindowHandle.ToLossyIntPtr();

                    if (response.OptionalTitleCase == Virtualdesktop.QueryOpenWindowsResponse.OptionalTitleOneofCase.Title)
                        info.Title = response.Title;
                    if (response.OptionalProcessIdCase == Virtualdesktop.QueryOpenWindowsResponse.OptionalProcessIdOneofCase.ProcessId)
                        info.ProcessId = (int)response.ProcessId;
                    if (response.OptionalIsMinimizedCase == QueryOpenWindowsResponse.OptionalIsMinimizedOneofCase.IsMinimized)
                        info.IsMinimized = response.IsMinimized;

                    if (response.OptionalPinnedToAllVirtualDesktopsCase == Virtualdesktop.QueryOpenWindowsResponse.OptionalPinnedToAllVirtualDesktopsOneofCase.PinnedToAllVirtualDesktops)
                        info.PinnedToAllVirtualDesktops = response.PinnedToAllVirtualDesktops;
                    if (response.OptionalVirtualDesktopIndexCase == Virtualdesktop.QueryOpenWindowsResponse.OptionalVirtualDesktopIndexOneofCase.VirtualDesktopIndex)
                        info.VirtualDesktopIndex = response.VirtualDesktopIndex;

                    return info;
                },
                cancellationToken
            );
        }
        async Task<bool> IVirtualDesktop.SetWindowShowState(IntPtr windowHandle, SetWindowShowStateRequest.Types.ShowState showState, CancellationToken cancellationToken)
        {
            return (await SendRequestWithSingleResponse<Virtualdesktop.SetWindowShowStateResponse>(new Virtualdesktop.Request()
            {
                SetWindowShowState = new Virtualdesktop.SetWindowShowStateRequest()
                {
                    WindowHandle = windowHandle.ToInt64(),
                    WantedState = showState,
                }
            }, cancellationToken)).WasVisible;
        }

        Task IVirtualDesktop.CloseWindow(IntPtr windowHandle, CancellationToken cancellationToken)
        {
            return SendRequestWithSingleResponse<Virtualdesktop.SuccessResponse>(new Virtualdesktop.Request()
            {
                CloseWindow = new Virtualdesktop.CloseWindowRequest()
                {
                    WindowHandle = windowHandle.ToInt64(),
                }
            }, cancellationToken);
        }

        async Task<bool> IVirtualDesktop.GetIsWindowMinimized(IntPtr windowHandle, CancellationToken cancellationToken)
        {
            return (await SendRequestWithSingleResponse<Virtualdesktop.GetIsWindowMinimizedResponse>(new Virtualdesktop.Request()
            {
                GetIsWindowMinimized = new Virtualdesktop.GetIsWindowMinimizedRequest()
                {
                    WindowHandle = windowHandle.ToInt64(),
                }
            }, cancellationToken)).IsMinimized;
        }

        async Task<IntPtr> IVirtualDesktop.GetForegroundWindow(CancellationToken cancellationToken)
        {
            return (await SendRequestWithSingleResponse<Virtualdesktop.GetForegroundWindowResponse>(new Virtualdesktop.Request()
            {
                GetForegroundWindow = new Virtualdesktop.GetForegroundWindowRequest()
                { }
            }, cancellationToken)).ForegroundWindowHandle.ToLossyIntPtr();
        }

        async Task<IntPtr> IVirtualDesktop.GetDesktopWindow(CancellationToken cancellationToken)
        {
            return (await SendRequestWithSingleResponse<Virtualdesktop.GetDesktopWindowResponse>(new Virtualdesktop.Request()
            {
                GetDesktopWindow = new Virtualdesktop.GetDesktopWindowRequest()
                { }
            }, cancellationToken)).DesktopWindowHandle.ToLossyIntPtr();
        }

        Task IVirtualDesktop.SetForegroundWindow(IntPtr windowHandle, CancellationToken cancellationToken)
        {
            return SendRequestWithSingleResponse<Virtualdesktop.SuccessResponse>(new Virtualdesktop.Request()
            {
                SetForegroundWindow = new Virtualdesktop.SetForegroundWindowRequest()
                {
                    WindowHandle = windowHandle.ToInt64(),
                }
            }, cancellationToken);
        }

        async Task<bool> IVirtualDesktop.GetIsElevated(CancellationToken cancellationToken)
        {
            return (await SendRequestWithSingleResponse<Virtualdesktop.GetIsElevatedResponse>(new Virtualdesktop.Request()
            {
                GetIsElevated = new Virtualdesktop.GetIsElevatedRequest()
                { }
            }, cancellationToken)).IsElevated;
        }

        async Task<System.Drawing.Rectangle> IVirtualDesktop.GetWindowLocation(IntPtr windowHandle, bool extendedFrameBounds, CancellationToken cancellationToken)
        {
            return (await SendRequestWithSingleResponse<Virtualdesktop.GetWindowLocationResponse>(new Virtualdesktop.Request()
            {
                GetWindowLocation = new Virtualdesktop.GetWindowLocationRequest()
                {
                    WindowHandle = windowHandle.ToInt64(),
                    ExtendedFrameBound = extendedFrameBounds,
                }
            }, cancellationToken)).Bounds.Convert();
        }

        Task IVirtualDesktop.MoveWindow(IntPtr windowHandle, System.Drawing.Rectangle wantedLocation, CancellationToken cancellationToken)
        {
            return SendRequestWithSingleResponse<Virtualdesktop.SuccessResponse>(new Virtualdesktop.Request()
            {
                MoveWindow = new Virtualdesktop.MoveWindowRequest()
                {
                    WindowHandle = windowHandle.ToInt64(),
                    WantedLocation = wantedLocation.Convert(),
                }
            }, cancellationToken);
        }

        ChannelReader<ShellEventArgs> IVirtualDesktop.ListenToShellEvents(bool getSecondEventArg, ShellEventType? wantedEventType, CancellationToken cancellationToken)
        {
            var request = new Virtualdesktop.ListenToShellEventsRequest()
            {
                GetSecondEventArg = getSecondEventArg,
            };
            if (wantedEventType.HasValue)
            {
                request.WantedEventType = wantedEventType.Value;
            }
            return SendRequestWithMultipleResponses<ShellEventArgs, Virtualdesktop.ListenToShellEventsResponse>(
                new Virtualdesktop.Request()
                {
                    ListenToShellEvents = request,
                },
                response =>
                {
                    return new ShellEventArgs()
                    {
                        EventType = response.EventType,
                        Data = response.Data.ToLossyIntPtr(),
                        SecondaryData = response.SecondaryData.ToLossyIntPtr(),
                        EventTypeCode = response.EventTypeCode,
                    };
                },
                cancellationToken
            );
        }

        Task IVirtualDesktop.StopWindowFlashing(QueryOpenWindowsFilter filter, CancellationToken cancellationToken)
        {
            return SendRequestWithSingleResponse<Virtualdesktop.SuccessResponse>(new Virtualdesktop.Request()
            {
                StopWindowFlashing = new Virtualdesktop.StopWindowFlashingRequest()
                {
                    WindowsFilter = filter,
                }
            }, cancellationToken);
        }

        #endregion Virtual Desktop Methods

        #endregion Methods


        #region Properties

        public bool IsDisposed { get => cancellationTokenSource.IsCancellationRequested; }

        #endregion Properties
    }
}
