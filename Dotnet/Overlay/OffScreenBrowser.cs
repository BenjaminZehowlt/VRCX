// Copyright(c) 2019-2022 pypy, Natsumi and individual contributors.
// All rights reserved.
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using CefSharp;
using CefSharp.Enums;
using CefSharp.OffScreen;
using CefSharp.Structs;
using SharpDX.Direct3D11;
using System;
using System.Threading;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Mathematics.Interop;
using Range = CefSharp.Structs.Range;
using Rect = CefSharp.Structs.Rect;

namespace VRCX
{
    public class OffScreenBrowser : ChromiumWebBrowser, IRenderHandler
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private bool _isAllowedToRender = true;
        
        private Device _targetDevice;
        private Device1 _sourceDevice1;
        private DeviceMultithread _deviceMultithread;
        private Query _query;
        private Texture2D _renderTarget;

        public OffScreenBrowser(string address, int width, int height)
            : base(address, automaticallyCreateBrowser: false)
        {
            var windowInfo = new WindowInfo();
            windowInfo.SetAsWindowless(IntPtr.Zero);
            windowInfo.WindowlessRenderingEnabled = true;
            windowInfo.SharedTextureEnabled = true;
            windowInfo.Width = width;
            windowInfo.Height = height;
            
            var browserSettings = new BrowserSettings()
            {
                DefaultEncoding = "UTF-8",
                WindowlessFrameRate = 60
            };
            
            CreateBrowser(windowInfo, browserSettings);

            Size = new System.Drawing.Size(width, height);
            RenderHandler = this;
            
            JavascriptBindings.ApplyVrJavascriptBindings(JavascriptObjectRepository);
        }

        public void UpdateRender(Device device, Texture2D renderTarget)
        {
            // We're going to give it a chance to reset, unlikely to work
            _isAllowedToRender = true;
            
            _sourceDevice1?.Dispose();
            _sourceDevice1 = null;
            
            _targetDevice = device;
            
            _deviceMultithread?.Dispose();
            _deviceMultithread = _targetDevice.QueryInterfaceOrNull<DeviceMultithread>();
            _deviceMultithread?.SetMultithreadProtected(true);

            _renderTarget = renderTarget;
            
            _query?.Dispose();
            _query = new Query(_targetDevice, new QueryDescription
            {
                Type = QueryType.Event,
                Flags = QueryFlags.None
            });
        }

        public new void Dispose()
        {
            RenderHandler = null;
            base.Dispose();
        }

        ScreenInfo? IRenderHandler.GetScreenInfo()
        {
            return new ScreenInfo
            {
                DeviceScaleFactor = 1.0F
            };
        }

        bool IRenderHandler.GetScreenPoint(int viewX, int viewY, out int screenX, out int screenY)
        {
            screenX = viewX;
            screenY = viewY;
            return false;
        }

        Rect IRenderHandler.GetViewRect()
        {
            return new Rect(0, 0, Size.Width, Size.Height);
        }

        void IRenderHandler.OnAcceleratedPaint(PaintElementType type, Rect dirtyRect, AcceleratedPaintInfo paintInfo)
        {
            if (!_isAllowedToRender)
                return;
            
            if (type != PaintElementType.View)
                return;

            if (_targetDevice == null)
                return;
            
            try
            {
                if (_sourceDevice1 == null)
                {
                    Device device = new Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport);
                    _sourceDevice1 = device.QueryInterface<Device1>();
                    device.Dispose();
                }
                
                using Texture2D cefTexture = _sourceDevice1.OpenSharedResource1<Texture2D>(paintInfo.SharedTextureHandle);
                _targetDevice.ImmediateContext.CopyResource(cefTexture, _renderTarget);
                _targetDevice.ImmediateContext.End(_query);
                _targetDevice.ImmediateContext.Flush();

                RawBool q = _targetDevice.ImmediateContext.GetData<RawBool>(_query, AsynchronousFlags.DoNotFlush);

                while (!q)
                {
                    Thread.Yield();
                    q = _targetDevice.ImmediateContext.GetData<RawBool>(_query, AsynchronousFlags.DoNotFlush);
                }
            }
            catch (SharpDXException ex)
            {
                _isAllowedToRender = false;
                logger.Error(ex);
                MessageBox.Show(ex.ToString(), "Failed to render VRCX VR Overlay", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void IRenderHandler.OnCursorChange(IntPtr cursor, CursorType type, CursorInfo customCursorInfo)
        {
        }

        void IRenderHandler.OnImeCompositionRangeChanged(Range selectedRange, Rect[] characterBounds)
        {
        }

        void IRenderHandler.OnPaint(PaintElementType type, Rect dirtyRect, IntPtr buffer, int width, int height)
        {
        }

        void IRenderHandler.OnPopupShow(bool show)
        {
        }

        void IRenderHandler.OnPopupSize(Rect rect)
        {
        }

        void IRenderHandler.OnVirtualKeyboardRequested(IBrowser browser, TextInputMode inputMode)
        {
        }

        bool IRenderHandler.StartDragging(IDragData dragData, DragOperationsMask mask, int x, int y)
        {
            return false;
        }

        void IRenderHandler.UpdateDragCursor(DragOperationsMask operation)
        {
        }
    }
}
