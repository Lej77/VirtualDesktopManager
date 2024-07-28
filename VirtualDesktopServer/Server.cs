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
using Virtualdesktop;

namespace VirtualDesktopServer
{
    /// <summary>
    /// Uses a provided <see cref="IVirtualDesktop"/> implementation to handle commands and queries that are parsed from protocol buffers.
    /// </summary>
    public class Server : IDisposable
    {
        #region Classes

        #endregion Classes


        #region Member Variables

        public Stream Input { get; }
        public Stream Output { get; }

        private readonly CancellationTokenSource cancellationTokenSource = null;
        private Channel<Virtualdesktop.Response> queuedResponses = Channel.CreateBounded<Virtualdesktop.Response>(new BoundedChannelOptions(100) { SingleReader = true });
        private readonly IVirtualDesktop implementation;

        private readonly object activeRequestsLocker = new object();
        private Dictionary<uint, CancellationTokenSource> activeRequests = new Dictionary<uint, CancellationTokenSource>();

        public volatile int MaxRequestSize = 1_000_000;
        public volatile int MaxResponseSize = 1_000_000;

        /// <summary>
        /// Invoked when a request read from the input stream could not be parsed correctly. This probably means that the client is in an invalid state and should be restarted.
        /// </summary>
        public event EventHandler<EventArgs> OnRequestParseError;
        /// <summary>
        /// Invoked when a request was ignored due to its message size.
        /// </summary>
        public event EventHandler<EventArgs> OnTooLargeRequest;

        public event EventHandler<Exception> OnInputClosed;
        public event EventHandler<Exception> OnOutputClosed;

        /// <summary>
        /// The server encountered an error when handling messages.
        /// 
        /// This is not invoked when the handling for a request sends an error responses, instead this is reserved for larger issues.
        /// </summary>
        public event EventHandler<EventArgs> OnServerError;

        #endregion Member Variables


        #region Constructors

        public Server(Stream input, Stream output, IVirtualDesktop implementation, CancellationToken[] cancelTokens = null)
        {
            Input = input;
            Output = output;
            this.implementation = implementation;

            if (cancelTokens != null && cancelTokens.Length > 0)
                cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancelTokens);
            else
                cancellationTokenSource = new CancellationTokenSource();

            OnRequestParseError += (sender, e) => OnServerError?.Invoke(this, EventArgs.Empty);
            OnTooLargeRequest += (sender, e) => OnServerError?.Invoke(this, EventArgs.Empty);
            OnInputClosed += (sender, e) => { if (e != null && !(e is OperationCanceledException)) OnServerError?.Invoke(this, EventArgs.Empty); };
            OnOutputClosed += (sender, e) => { if (e != null && !(e is OperationCanceledException)) OnServerError?.Invoke(this, EventArgs.Empty); };

            _ = handleInput();
            _ = handleOutput();
        }

        #endregion Constructors


        #region Methods

        private async Task handleInput()
        {
            try
            {
                await Utils.ReadFromInput(
                    Input,
                    cancellationTokenSource,
                    allowedMessageSize: async (messageSize) =>
                    {
                        int max = MaxRequestSize;
                        bool allowed = messageSize <= max;
                        if (!allowed)
                        {
                            OnTooLargeRequest?.Invoke(this, EventArgs.Empty);
                            await queuedResponses.Writer.WriteAsync(new Virtualdesktop.Response()
                            {
                                Id = 0,
                                Done = false,
                                DroppedRequest = new Virtualdesktop.DroppedRequestResponse()
                                {
                                    ErrorMessage = "Too large request: maximum message size is " + max + " bytes but the message was " + messageSize + " bytes long.",
                                    Reason = Virtualdesktop.DroppedRequestResponse.Types.Reason.TooLargeRequest,
                                },
                            }, cancellationTokenSource.Token);
                        }
                        return allowed;
                    },
                    handleMessage: async (buffer, startOffset, length) =>
                    {
                        // Parse and handle reqeust:
                        try
                        {
                            var request = Virtualdesktop.Request.Parser.ParseFrom(buffer, startOffset, length);
                            _ = handleRequest(request);
                        }
                        catch (Exception ex)
                        {
                            OnRequestParseError?.Invoke(this, EventArgs.Empty);
                            await queuedResponses.Writer.WriteAsync(new Virtualdesktop.Response()
                            {
                                Id = 0,
                                Done = false,
                                DroppedRequest = new Virtualdesktop.DroppedRequestResponse()
                                {
                                    ErrorMessage = "Parse error: " + ex.ToString(),
                                    Reason = Virtualdesktop.DroppedRequestResponse.Types.Reason.ParseError,
                                },
                            }, cancellationTokenSource.Token);
                        }
                    }
                );
            }
            catch (Exception ex)
            {
                OnInputClosed?.Invoke(this, ex);
                return;
            }
            OnInputClosed?.Invoke(this, null);
        }

        private void RegisterRequestId(uint id, CancellationTokenSource requestCancellation)
        {
            lock (activeRequestsLocker)
            {
                if (activeRequests.ContainsKey(id))
                {
                    throw new Exception("Request id already in use, cancel the previous request or wait for it to finish before reusing the id.");
                }
                activeRequests.Add(id, requestCancellation);
            }
        }

        /// <summary>
        /// Dispatch request to an implementation method then serialize return value to a response and queue it to be written to the output.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task handleRequest(Virtualdesktop.Request request)
        {
            Virtualdesktop.Response response = new Virtualdesktop.Response()
            {
                Id = request.Id,
                // Most requests only have a single response. If that isn't the case then set this to false.
                Done = true,
                // By default send a response with no data:
                Success = new Virtualdesktop.SuccessResponse(),
            };
            bool shouldSendResponse = true;
            bool registered = false;
            try
            {
                using (CancellationTokenSource requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token))
                {
                    try
                    {
                        // Register this request's id to allow it to be canceled.
                        void register()
                        {
                            RegisterRequestId(request.Id, requestCancellation);
                            registered = true;
                        }
#pragma warning disable CS8321 // Local function is declared but never used
                        Task untilCancelation()
#pragma warning restore CS8321 // Local function is declared but never used
                        {
                            if (!registered) throw new Exception("Can't wait until cancelation if request hasn't been registered with the server.");
                            return Task.Delay(Timeout.Infinite, requestCancellation.Token);
                        }

                        async Task handleEvent<T>(Func<ChannelReader<T>> getChannel, Action<T, Virtualdesktop.Response> handleMessage)
                        {
                            if (!registered) register();
                            var channel = getChannel();
                            while (await channel.WaitToReadAsync(requestCancellation.Token))
                                while (channel.TryRead(out var e))
                                {
                                    if (e != null)
                                    {
                                        var message = new Virtualdesktop.Response()
                                        {
                                            Id = request.Id,
                                            Done = false,
                                        };
                                        handleMessage(e, message);
                                        await queuedResponses.Writer.WriteAsync(message, requestCancellation.Token);
                                    }

                                }
                        }

                        switch (request.DataCase)
                        {
                            case Virtualdesktop.Request.DataOneofCase.Cancel:
                                // If we error out then we haven't canceled the request and the original requests message will still be sent:
                                response.Done = false;
                                lock (activeRequestsLocker)
                                {
                                    if (activeRequests.TryGetValue(request.Id, out CancellationTokenSource value))
                                    {
                                        value.Cancel();
                                        value.Dispose();
                                        activeRequests.Remove(request.Id);
                                        // The request was canceled so this will be the last response:
                                        response.Done = true;
                                        response.Canceled = new Virtualdesktop.CanceledResponse();
                                    }
                                    else
                                    {
                                        // Request have already been completed:
                                        shouldSendResponse = false;
                                    }
                                }
                                break;

                            case Virtualdesktop.Request.DataOneofCase.Log:
                                register();
                                await implementation.Log(request.Log.LogMessage, requestCancellation.Token);
                                break;

                            case Virtualdesktop.Request.DataOneofCase.CreateVirtualDesktop:
                                register();
                                await implementation.CreateVirtualDesktop(request.CreateVirtualDesktop.SwitchToTheCreatedDesktop, requestCancellation.Token);
                                break;

                            case Virtualdesktop.Request.DataOneofCase.DeleteVirtualDesktop:
                                register();
                                switch (request.DeleteVirtualDesktop.DesktopToRemoveCase)
                                {
                                    case Virtualdesktop.DeleteVirtualDesktopRequest.DesktopToRemoveOneofCase.VirtualDesktopIndex:
                                        await implementation.DeleteVirtualDesktop(request.DeleteVirtualDesktop.PreferFallingBackToTheLeft, request.DeleteVirtualDesktop.VirtualDesktopIndex, requestCancellation.Token);
                                        break;
                                    case Virtualdesktop.DeleteVirtualDesktopRequest.DesktopToRemoveOneofCase.CurrentDesktop:
                                        await implementation.DeleteVirtualDesktop(request.DeleteVirtualDesktop.PreferFallingBackToTheLeft, requestCancellation.Token);
                                        break;
                                    case Virtualdesktop.DeleteVirtualDesktopRequest.DesktopToRemoveOneofCase.None:
                                    default:
                                        throw new NotImplementedException();
                                }
                                break;

                            case Virtualdesktop.Request.DataOneofCase.GetCurrentVirtualDesktop:
                                register();
                                {
                                    var result = await implementation.GetCurrentVirtualDesktopIndex(requestCancellation.Token);
                                    response.GetCurrentVirtualDesktop = new Virtualdesktop.GetCurrentVirtualDesktopResponse()
                                    {
                                        CurrentVirtualDesktopIndex = result,
                                    };
                                }
                                break;

                            case Virtualdesktop.Request.DataOneofCase.ChangeVirtualDesktop:
                                register();
                                switch (request.ChangeVirtualDesktop.ChangeCase)
                                {
                                    case Virtualdesktop.ChangeVirtualDesktopRequest.ChangeOneofCase.WantedVirtualDesktopIndex:
                                        await implementation.ChangeCurrentVirtualDesktopToIndex(request.ChangeVirtualDesktop.SmoothChange, request.ChangeVirtualDesktop.WantedVirtualDesktopIndex, requestCancellation.Token);
                                        break;
                                    case Virtualdesktop.ChangeVirtualDesktopRequest.ChangeOneofCase.ChangeRight:
                                        await implementation.ChangeCurrentVirtualDesktopToRight(request.ChangeVirtualDesktop.SmoothChange, request.ChangeVirtualDesktop.ChangeRight, requestCancellation.Token);
                                        break;
                                    case Virtualdesktop.ChangeVirtualDesktopRequest.ChangeOneofCase.ChangeLeft:
                                        await implementation.ChangeCurrentVirtualDesktopToLeft(request.ChangeVirtualDesktop.SmoothChange, request.ChangeVirtualDesktop.ChangeLeft, requestCancellation.Token);
                                        break;

                                    case Virtualdesktop.ChangeVirtualDesktopRequest.ChangeOneofCase.None:
                                    default:
                                        throw new NotImplementedException();
                                }
                                break;

                            case Virtualdesktop.Request.DataOneofCase.ListenForVirtualDesktopChanged:
                                await handleEvent(
                                    getChannel: () => implementation.ListenForVirtualDesktopChanged(requestCancellation.Token),
                                    handleMessage: (e, message) =>
                                    {
                                        message.ListenForVirtualDesktopChanged = new Virtualdesktop.ListenForVirtualDesktopChangedResponse()
                                        {
                                            NewCurrentVirtualDesktopIndex = e.NewCurrentVirtualDesktopIndex,
                                            OldCurrentVirtualDesktopIndex = e.OldCurrentVirtualDesktopIndex,
                                        };
                                    }
                                );
                                break;
                            case Virtualdesktop.Request.DataOneofCase.ListenForVirtualDesktopCreated:
                                await handleEvent(
                                    getChannel: () => implementation.ListenForVirtualDesktopCreated(requestCancellation.Token),
                                    handleMessage: (e, message) =>
                                    {
                                        message.ListenForVirtualDesktopCreated = new Virtualdesktop.ListenForVirtualDesktopCreatedResponse()
                                        {
                                            IndexOfCreatedVirtualDesktop = e.IndexOfCreatedVirtualDesktop,
                                        };
                                    }
                                );
                                break;
                            case Virtualdesktop.Request.DataOneofCase.ListenForVirtualDesktopDeleted:
                                await handleEvent(
                                    getChannel: () => implementation.ListenForVirtualDesktopDeleted(requestCancellation.Token),
                                    handleMessage: (e, message) =>
                                    {
                                        message.ListenForVirtualDesktopDeleted = new Virtualdesktop.ListenForVirtualDesktopDeletedResponse()
                                        {
                                            IndexOfDeletedVirtualDesktop = e.IndexOfDeletedVirtualDesktop,
                                            IndexOfFallbackVirtualDesktop = e.IndexOfFallbackVirtualDesktop,
                                        };
                                    }
                                );
                                break;

                            case Virtualdesktop.Request.DataOneofCase.PinWindowToAllVirtualDesktops:
                                register();
                                await implementation.PinWindowToAllVirtualDesktops(request.PinWindowToAllVirtualDesktops.WindowHandle.ToLossyIntPtr(), request.PinWindowToAllVirtualDesktops.StopWindowFlashing, requestCancellation.Token);
                                break;

                            case Virtualdesktop.Request.DataOneofCase.UnpinWindowFromAllVirtualDesktops:
                                register();
                                {
                                    var handle = request.UnpinWindowFromAllVirtualDesktops.WindowHandle.ToLossyIntPtr();
                                    switch (request.UnpinWindowFromAllVirtualDesktops.ShouldMoveAsWellCase)
                                    {
                                        case Virtualdesktop.UnpinWindowFromAllVirtualDesktopsRequest.ShouldMoveAsWellOneofCase.JustUnpin:
                                            await implementation.UnpinWindowFromAllVirtualDesktops(handle, requestCancellation.Token);
                                            break;
                                        case Virtualdesktop.UnpinWindowFromAllVirtualDesktopsRequest.ShouldMoveAsWellOneofCase.MoveIfUnpinned:
                                            await implementation.UnpinWindowFromAllVirtualDesktops(
                                                handle,
                                                request.UnpinWindowFromAllVirtualDesktops.MoveIfUnpinned.TargetVirtualDesktopIndex,
                                                moveEvenIfAlreadyUnpinned: false,
                                                request.UnpinWindowFromAllVirtualDesktops.MoveIfUnpinned.MoveOptions,
                                                requestCancellation.Token
                                            );
                                            break;
                                        case Virtualdesktop.UnpinWindowFromAllVirtualDesktopsRequest.ShouldMoveAsWellOneofCase.MoveUnconditionally:
                                            await implementation.UnpinWindowFromAllVirtualDesktops(
                                                handle,
                                                request.UnpinWindowFromAllVirtualDesktops.MoveUnconditionally.TargetVirtualDesktopIndex,
                                                moveEvenIfAlreadyUnpinned: true,
                                                request.UnpinWindowFromAllVirtualDesktops.MoveUnconditionally.MoveOptions,
                                                requestCancellation.Token
                                            );
                                            break;

                                        case Virtualdesktop.UnpinWindowFromAllVirtualDesktopsRequest.ShouldMoveAsWellOneofCase.None:
                                        default:
                                            throw new NotImplementedException();
                                    }
                                }
                                break;

                            case Virtualdesktop.Request.DataOneofCase.MoveWindowToVirtualDesktop:
                                register();
                                {
                                    var handle = request.MoveWindowToVirtualDesktop.WindowHandle.ToLossyIntPtr();
                                    var options = request.MoveWindowToVirtualDesktop.MoveOptions;
                                    switch (request.MoveWindowToVirtualDesktop.ChangeCase)
                                    {
                                        case Virtualdesktop.MoveWindowToVirtualDesktopRequest.ChangeOneofCase.WantedVirtualDesktopIndex:
                                            await implementation.MoveWindowToVirtualDesktopIndex(handle, request.MoveWindowToVirtualDesktop.WantedVirtualDesktopIndex, options, requestCancellation.Token);
                                            break;
                                        case Virtualdesktop.MoveWindowToVirtualDesktopRequest.ChangeOneofCase.ChangeRight:
                                            await implementation.MoveWindowToVirtualDesktopRightOfCurrent(handle, request.MoveWindowToVirtualDesktop.ChangeRight, options, requestCancellation.Token);
                                            break;
                                        case Virtualdesktop.MoveWindowToVirtualDesktopRequest.ChangeOneofCase.ChangeLeft:
                                            await implementation.MoveWindowToVirtualDesktopLeftOfCurrent(handle, request.MoveWindowToVirtualDesktop.ChangeLeft, options, requestCancellation.Token);
                                            break;

                                        case Virtualdesktop.MoveWindowToVirtualDesktopRequest.ChangeOneofCase.None:
                                        default:
                                            throw new NotImplementedException();
                                    }
                                }
                                break;

                            case Virtualdesktop.Request.DataOneofCase.GetWindowVirtualDesktopIndex:
                                register();
                                {
                                    var index = await implementation.GetWindowVirtualDesktopIndex(request.GetWindowVirtualDesktopIndex.WindowHandle.ToLossyIntPtr(), requestCancellation.Token);
                                    response.GetWindowVirtualDesktopIndex = new Virtualdesktop.GetWindowVirtualDesktopIndexResponse()
                                    {
                                        VirtualDesktopIndex = index,
                                    };
                                }
                                break;

                            case Virtualdesktop.Request.DataOneofCase.GetWindowIsPinnedToVirtualDesktop:
                                register();
                                {
                                    var isPinned = await implementation.GetWindowIsPinnedToVirtualDesktop(request.GetWindowIsPinnedToVirtualDesktop.WindowHandle.ToLossyIntPtr(), requestCancellation.Token);
                                    response.GetWindowIsPinnedToVirtualDesktop = new Virtualdesktop.GetWindowIsPinnedToVirtualDesktopResponse()
                                    {
                                        IsPinned = isPinned,
                                    };
                                }
                                break;

                            case Virtualdesktop.Request.DataOneofCase.OpenWindowsQuery:
                                {
                                    await handleEvent<QueryOpenWindowsInfo>(
                                        getChannel: () => implementation.QueryOpenWindows(
                                            request.OpenWindowsQuery.WindowsFilter,
                                            request.OpenWindowsQuery.WantedDataSpecifier,
                                            requestCancellation.Token
                                        ),
                                        handleMessage: (e, message) =>
                                        {
                                            var info = new Virtualdesktop.QueryOpenWindowsResponse()
                                            {
                                                WindowHandle = e.WindowHandle.ToInt64(),
                                            };
                                            if (e.ParentWindowHandle != IntPtr.Zero)
                                                info.ParentWindowHandle = e.ParentWindowHandle.ToInt64();
                                            if (e.RootParentWindowHandle != IntPtr.Zero)
                                                info.RootParentWindowHandle = e.RootParentWindowHandle.ToInt64();
                                            if (e.Title != null)
                                                info.Title = e.Title;
                                            if (e.ProcessId.HasValue)
                                                info.ProcessId = (uint)e.ProcessId.Value;
                                            if (e.PinnedToAllVirtualDesktops.HasValue)
                                                info.PinnedToAllVirtualDesktops = e.PinnedToAllVirtualDesktops.Value;
                                            if (e.VirtualDesktopIndex.HasValue)
                                                info.VirtualDesktopIndex = e.VirtualDesktopIndex.Value;
                                            if (e.IsMinimized.HasValue)
                                                info.IsMinimized = e.IsMinimized.Value;
                                            message.OpenWindowsQuery = info;
                                        }
                                    );
                                }
                                break;

                            case Virtualdesktop.Request.DataOneofCase.SetWindowShowState:
                                register();
                                {
                                    var info = request.SetWindowShowState;
                                    var windowWasVisible = await implementation.SetWindowShowState(info.WindowHandle.ToLossyIntPtr(), info.WantedState, requestCancellation.Token);
                                    response.SetWindowShowState = new Virtualdesktop.SetWindowShowStateResponse()
                                    {
                                        WasVisible = windowWasVisible,
                                    };
                                }
                                break;

                            case Virtualdesktop.Request.DataOneofCase.CloseWindow:
                                register();
                                await implementation.CloseWindow(request.CloseWindow.WindowHandle.ToLossyIntPtr(), requestCancellation.Token);
                                break;

                            case Virtualdesktop.Request.DataOneofCase.GetIsWindowMinimized:
                                register();
                                {
                                    var isMinimized = await implementation.GetIsWindowMinimized(request.GetIsWindowMinimized.WindowHandle.ToLossyIntPtr(), requestCancellation.Token);
                                    response.GetIsWindowMinimized = new Virtualdesktop.GetIsWindowMinimizedResponse()
                                    {
                                        IsMinimized = isMinimized,
                                    };
                                }
                                break;

                            case Virtualdesktop.Request.DataOneofCase.GetForegroundWindow:
                                register();
                                {
                                    var windowHandle = await implementation.GetForegroundWindow(requestCancellation.Token);
                                    response.GetForegroundWindow = new Virtualdesktop.GetForegroundWindowResponse()
                                    {
                                        ForegroundWindowHandle = windowHandle.ToInt64(),
                                    };
                                }
                                break;

                            case Virtualdesktop.Request.DataOneofCase.GetDesktopWindow:
                                register();
                                {
                                    var windowHandle = await implementation.GetDesktopWindow(requestCancellation.Token);
                                    response.GetDesktopWindow = new Virtualdesktop.GetDesktopWindowResponse()
                                    {
                                        DesktopWindowHandle = windowHandle.ToInt64(),
                                    };
                                }
                                break;

                            case Virtualdesktop.Request.DataOneofCase.SetForegroundWindow:
                                register();
                                await implementation.SetForegroundWindow(request.SetForegroundWindow.WindowHandle.ToLossyIntPtr(), requestCancellation.Token);
                                break;

                            case Virtualdesktop.Request.DataOneofCase.GetIsElevated:
                                register();
                                {
                                    var isElevated = await implementation.GetIsElevated(requestCancellation.Token);
                                    response.GetIsElevated = new Virtualdesktop.GetIsElevatedResponse()
                                    {
                                        IsElevated = isElevated,
                                    };
                                }
                                break;

                            case Virtualdesktop.Request.DataOneofCase.GetWindowLocation:
                                register();
                                {
                                    var info = request.GetWindowLocation;
                                    var bounds = await implementation.GetWindowLocation(info.WindowHandle.ToLossyIntPtr(), info.ExtendedFrameBound, requestCancellation.Token);
                                    response.GetWindowLocation = new Virtualdesktop.GetWindowLocationResponse()
                                    {
                                        Bounds = bounds.Convert(),
                                    };
                                }
                                break;

                            case Virtualdesktop.Request.DataOneofCase.MoveWindow:
                                register();
                                {
                                    var info = request.MoveWindow;
                                    await implementation.MoveWindow(info.WindowHandle.ToLossyIntPtr(), info.WantedLocation.Convert(), requestCancellation.Token);
                                }
                                break;

                            case Virtualdesktop.Request.DataOneofCase.ListenToShellEvents:
                                {
                                    var info = request.ListenToShellEvents;
                                    await handleEvent(
                                        getChannel: () => implementation.ListenToShellEvents(info.GetSecondEventArg, info.OptionalWantedShellTypeCase == Virtualdesktop.ListenToShellEventsRequest.OptionalWantedShellTypeOneofCase.None ? (ShellEventType?)null : info.WantedEventType, requestCancellation.Token),
                                        handleMessage: (e, message) =>
                                        {
                                            message.ListenToShellEvents = new Virtualdesktop.ListenToShellEventsResponse()
                                            {
                                                EventType = e.EventType,
                                                Data = e.Data.ToInt64(),
                                                SecondaryData = e.SecondaryData.ToInt64(),
                                                EventTypeCode = e.EventTypeCode,
                                            };
                                        }
                                    );
                                }
                                break;

                            case Virtualdesktop.Request.DataOneofCase.StopWindowFlashing:
                                {
                                    await implementation.StopWindowFlashing(request.StopWindowFlashing.WindowsFilter, requestCancellation.Token);
                                }
                                break;

                            case Virtualdesktop.Request.DataOneofCase.None:
                            default:
                                throw new Exception("Unknown request type.");
                        }
                    }
                    finally
                    {
                        // Note: if someone canceled the request then dont send the response.
                        if (registered)
                        {
                            shouldSendResponse = false;
                            lock (activeRequestsLocker)
                            {
                                if (activeRequests.TryGetValue(request.Id, out CancellationTokenSource value))
                                {
                                    if (value == requestCancellation)
                                    {
                                        activeRequests.Remove(request.Id);
                                        shouldSendResponse = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (shouldSendResponse)
                {
                    response.Error = new Virtualdesktop.ErrorResponse()
                    {
                        ErrorMessage = ex.ToString()
                    };
                }
            }
            // Enqueue response to writer:
            if (shouldSendResponse)
                await queuedResponses.Writer.WriteAsync(response, cancellationTokenSource.Token);
        }

        private async Task handleOutput()
        {
            try
            {
                await Utils.WriteToOutput<Virtualdesktop.Response>(
                    Output,
                    cancellationTokenSource,
                    queuedResponses.Reader,
                    checkMessageSize: (size, message) =>
                    {
                        // Handle too large responses:
                        var maxSize = MaxResponseSize;
                        if (size <= maxSize)
                            return Task.FromResult(Utils.OutputMessageSizeCheck.Allow);
                        else
                        {
                            message.Error = new Virtualdesktop.ErrorResponse() { ErrorMessage = "Response was " + size + " bytes large which exceeded the maximum size of " + maxSize + " bytes." };
                            return Task.FromResult(Utils.OutputMessageSizeCheck.Resized);
                        }
                    }
                );
            }
            catch (Exception ex)
            {
                OnOutputClosed?.Invoke(this, ex);
                return;
            }
            OnOutputClosed?.Invoke(this, null);
        }


        public void Dispose()
        {
            if (IsDisposed) return;
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
        }

        #endregion Methods


        #region Properties

        public bool IsDisposed { get => cancellationTokenSource.IsCancellationRequested; }

        #endregion Properties
    }
}
