using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using ScreenshotProRegion = ScreenshotPro.Core.Models.Region;
using ScreenshotPro.Core.Services;
using System;
using System.Buffers;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;

namespace ScreenshotPro.UI.Views
{
    public sealed partial class RegionSelectionWindow : Window
    {
        private bool _isSelecting;
        private Windows.Foundation.Point _startPoint;
        private Windows.Foundation.Point _currentPoint;
        private WriteableBitmap? _desktopSurface;
        private Bitmap? _backingBitmap;
        private readonly object _bitmapLock = new();
        private double _savedOverlayOpacity = 1;
        private double _savedDesktopOpacity = 1;
        private bool _overlaySuppressed;

        public event EventHandler<RegionSelectedEventArgs>? RegionSelected;
        public event EventHandler? SelectionCancelled;

        public RegionSelectionWindow(Bitmap? desktopBitmap = null)
        {
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;

            var appWindow = AppWindow;
            if (appWindow != null)
            {
                // Hide from app switcher
                appWindow.IsShownInSwitchers = false;

                // Note: Presenter will be set by the caller (LibraryPage) after positioning
            }

            if (desktopBitmap != null)
            {
                BindDesktopBitmap(desktopBitmap);
            }

            OverlayGrid.PointerPressed += OverlayGrid_PointerPressed;
            OverlayGrid.PointerMoved += OverlayGrid_PointerMoved;
            OverlayGrid.PointerReleased += OverlayGrid_PointerReleased;

            Content.KeyDown += Window_KeyDown;
            Content.Focus(FocusState.Programmatic);

            // Initialize dimming to cover entire screen
            this.Activated += RegionSelectionWindow_Activated;
        }

        private void RegionSelectionWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            // Only initialize once
            this.Activated -= RegionSelectionWindow_Activated;
            InitializeDimming();
        }

        private void InitializeDimming()
        {
            var canvasWidth = OverlayGrid.ActualWidth;
            var canvasHeight = OverlayGrid.ActualHeight;

            // Initially dim the entire screen with top rectangle
            Canvas.SetLeft(TopDimmingRect, 0);
            Canvas.SetTop(TopDimmingRect, 0);
            TopDimmingRect.Width = canvasWidth;
            TopDimmingRect.Height = canvasHeight;

            // Hide other rectangles initially
            BottomDimmingRect.Width = 0;
            BottomDimmingRect.Height = 0;
            LeftDimmingRect.Width = 0;
            LeftDimmingRect.Height = 0;
            RightDimmingRect.Width = 0;
            RightDimmingRect.Height = 0;
        }



        public async Task BindDesktopBitmapAsync(Bitmap bitmap)
        {
            lock (_bitmapLock)
            {
                _backingBitmap = bitmap;
                CopyBitmapToSurface(bitmap);
            }

            // Wait for image to be rendered
            var tcs = new TaskCompletionSource<bool>();
            void OnImageOpened(object sender, RoutedEventArgs e)
            {
                DesktopImage.ImageOpened -= OnImageOpened;
                tcs.SetResult(true);
            }

            DesktopImage.ImageOpened += OnImageOpened;

            // Set a timeout in case ImageOpened doesn't fire
            var delay = Task.Delay(500);
            await Task.WhenAny(tcs.Task, delay);
        }

        public void BindDesktopBitmap(Bitmap bitmap)
        {
            lock (_bitmapLock)
            {
                _backingBitmap = bitmap;
                CopyBitmapToSurface(bitmap);
            }
        }

        public void RefreshDesktopBackground()
        {
            lock (_bitmapLock)
            {
                if (_backingBitmap != null)
                {
                    CopyBitmapToSurface(_backingBitmap);
                }
            }
        }

        public void SuppressOverlayForCapture()
        {
            if (_overlaySuppressed)
            {
                return;
            }

            _savedOverlayOpacity = OverlayGrid.Opacity;
            _savedDesktopOpacity = DesktopImage.Opacity;

            OverlayGrid.Opacity = 0;
            DesktopImage.Opacity = 0;
            _overlaySuppressed = true;
        }

        public void RestoreOverlayAfterCapture()
        {
            if (!_overlaySuppressed)
            {
                return;
            }

            DesktopImage.Opacity = _savedDesktopOpacity;
            OverlayGrid.Opacity = _savedOverlayOpacity;
            _overlaySuppressed = false;
        }
        private void EnsureSurfaceSize(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                return;
            }

            if (_desktopSurface != null &&
                _desktopSurface.PixelWidth == width &&
                _desktopSurface.PixelHeight == height)
            {
                return;
            }

            _desktopSurface = new WriteableBitmap(width, height);
            DesktopImage.Source = _desktopSurface;
        }

        private void CopyBitmapToSurface(Bitmap bitmap)
        {
            EnsureSurfaceSize(bitmap.Width, bitmap.Height);

            if (_desktopSurface == null)
            {
                return;
            }

            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            try
            {
                var sourceStride = bitmapData.Stride;
                var destStride = _desktopSurface.PixelWidth * 4;
                var height = bitmap.Height;
                var sourceLength = sourceStride * height;
                var sourceBuffer = ArrayPool<byte>.Shared.Rent(sourceLength);
                byte[]? destBuffer = null;

                try
                {
                    Marshal.Copy(bitmapData.Scan0, sourceBuffer, 0, sourceLength);

                    using var pixelStream = _desktopSurface.PixelBuffer.AsStream();
                    pixelStream.Seek(0, SeekOrigin.Begin);

                    if (sourceStride == destStride)
                    {
                        pixelStream.Write(sourceBuffer, 0, sourceLength);
                    }
                    else
                    {
                        var destLength = destStride * height;
                        destBuffer = ArrayPool<byte>.Shared.Rent(destLength);

                        for (var y = 0; y < height; y++)
                        {
                            var sourceOffset = y * sourceStride;
                            var destOffset = y * destStride;
                            var copyLength = Math.Min(sourceStride, destStride);

                            Buffer.BlockCopy(sourceBuffer, sourceOffset, destBuffer, destOffset, copyLength);

                            if (destStride > copyLength)
                            {
                                Array.Clear(destBuffer, destOffset + copyLength, destStride - copyLength);
                            }
                        }

                        pixelStream.Write(destBuffer, 0, destLength);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(sourceBuffer);
                    if (destBuffer != null)
                    {
                        ArrayPool<byte>.Shared.Return(destBuffer);
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            _desktopSurface.Invalidate();
            DesktopImage.Opacity = 1;
        }

        private void Window_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                CancelSelection();
            }
        }

        private void OverlayGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isSelecting = true;
            _startPoint = e.GetCurrentPoint(OverlayGrid).Position;
            _currentPoint = _startPoint;

            OverlayGrid.CapturePointer(e.Pointer);
            SelectionRectangle.Visibility = Visibility.Visible;

            UpdateSelectionRectangle();
        }

        private void OverlayGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isSelecting)
            {
                _currentPoint = e.GetCurrentPoint(OverlayGrid).Position;
                UpdateSelectionRectangle();
            }
        }

        private void OverlayGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_isSelecting)
            {
                _isSelecting = false;
                OverlayGrid.ReleasePointerCapture(e.Pointer);

                CompleteSelection();
            }
        }

        private void UpdateSelectionRectangle()
        {
            var left = Math.Min(_startPoint.X, _currentPoint.X);
            var top = Math.Min(_startPoint.Y, _currentPoint.Y);
            var width = Math.Abs(_currentPoint.X - _startPoint.X);
            var height = Math.Abs(_currentPoint.Y - _startPoint.Y);

            // Update selection rectangle border
            Canvas.SetLeft(SelectionRectangle, left);
            Canvas.SetTop(SelectionRectangle, top);
            SelectionRectangle.Width = width;
            SelectionRectangle.Height = height;

            // Get canvas dimensions
            var canvasWidth = OverlayGrid.ActualWidth;
            var canvasHeight = OverlayGrid.ActualHeight;

            // Ensure we have valid dimensions
            if (canvasWidth <= 0 || canvasHeight <= 0) return;

            // Top rectangle - from top of screen to top of selection
            Canvas.SetLeft(TopDimmingRect, 0);
            Canvas.SetTop(TopDimmingRect, 0);
            TopDimmingRect.Width = canvasWidth;
            TopDimmingRect.Height = Math.Max(0, top);

            // Bottom rectangle - from bottom of selection to bottom of screen
            var bottomTop = top + height;
            Canvas.SetLeft(BottomDimmingRect, 0);
            Canvas.SetTop(BottomDimmingRect, bottomTop);
            BottomDimmingRect.Width = canvasWidth;
            BottomDimmingRect.Height = Math.Max(0, canvasHeight - bottomTop);

            // Left rectangle - from left edge to left of selection (only in selection row)
            Canvas.SetLeft(LeftDimmingRect, 0);
            Canvas.SetTop(LeftDimmingRect, top);
            LeftDimmingRect.Width = Math.Max(0, left);
            LeftDimmingRect.Height = Math.Max(0, height);

            // Right rectangle - from right of selection to right edge (only in selection row)
            var rightLeft = left + width;
            Canvas.SetLeft(RightDimmingRect, rightLeft);
            Canvas.SetTop(RightDimmingRect, top);
            RightDimmingRect.Width = Math.Max(0, canvasWidth - rightLeft);
            RightDimmingRect.Height = Math.Max(0, height);
        }

        private void CompleteSelection()
        {
            var left = Math.Min(_startPoint.X, _currentPoint.X);
            var top = Math.Min(_startPoint.Y, _currentPoint.Y);
            var width = Math.Abs(_currentPoint.X - _startPoint.X);
            var height = Math.Abs(_currentPoint.Y - _startPoint.Y);

            if (width > 10 && height > 10)
            {
                // Convert UI coordinates to bitmap coordinates when using Stretch="Fill"
                if (_backingBitmap != null && DesktopImage.ActualWidth > 0 && DesktopImage.ActualHeight > 0)
                {
                    var scaleX = _backingBitmap.Width / DesktopImage.ActualWidth;
                    var scaleY = _backingBitmap.Height / DesktopImage.ActualHeight;

                    var bitmapLeft = (int)(left * scaleX);
                    var bitmapTop = (int)(top * scaleY);
                    var bitmapWidth = (int)(width * scaleX);
                    var bitmapHeight = (int)(height * scaleY);

                    var region = new ScreenshotProRegion(bitmapLeft, bitmapTop, bitmapWidth, bitmapHeight);
                    RegionSelected?.Invoke(this, new RegionSelectedEventArgs(region));
                }
                else
                {
                    // Fallback if scaling can't be determined
                    var region = new ScreenshotProRegion((int)left, (int)top, (int)width, (int)height);
                    RegionSelected?.Invoke(this, new RegionSelectedEventArgs(region));
                }
            }
            else
            {
                CancelSelection();
            }
        }

        private void CancelSelection()
        {
            SelectionCancelled?.Invoke(this, EventArgs.Empty);
        }
    }

    public class RegionSelectedEventArgs : EventArgs
    {
        public ScreenshotProRegion SelectedRegion { get; }

        public RegionSelectedEventArgs(ScreenshotProRegion region)
        {
            SelectedRegion = region;
        }
    }
}



