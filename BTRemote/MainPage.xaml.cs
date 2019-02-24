﻿using System;
using System.Collections.Generic;
using BTRemote.Model;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BTRemote
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : ContentPage
    {
        private readonly BluetoothDevice _btDevice;
        private readonly IBluetoothDeviceHelper _bluetoothDeviceHelper;
        private readonly Dictionary<long, TouchInfo> _idDictionary = new Dictionary<long, TouchInfo>();
        private float _canvasWidth = 0;
        private float _canvasHeight = 0;
        private readonly int _timerDelay = 250;
        private int _liftPosition = 0;

        class TouchInfo
        {
            public SKPoint Location { get; set; }
        }

        public MainPage(BluetoothDevice btDevice)
        {
            InitializeComponent();

            if (btDevice != null)
            {
                _btDevice = btDevice;
                _bluetoothDeviceHelper = DependencyService.Get<IBluetoothDeviceHelper>();
                Connect2BluetoothDevice();
            }
            else
            {
                LabelBluetoothStatus.Text = "No Bluetooth device found.";
            }

            Device.StartTimer(TimeSpan.FromMilliseconds(_timerDelay), OnTimerTick);
        }

        async void Connect2BluetoothDevice()
        {
            var connected = await _bluetoothDeviceHelper.Connect(_btDevice.Address);
            LabelBluetoothStatus.Text = connected ? $"Connected to {_btDevice.Name}" : $"Cannot connect to {_btDevice.Name}!";
        }

        private bool OnTimerTick()
        {
            if (_idDictionary.Count > 0)
            {
                var idPosInfo = _idDictionary[0];
                MessageLabel.Text = $"Last: {idPosInfo.Location.X}, {idPosInfo.Location.Y}";

                if (_bluetoothDeviceHelper != null && _bluetoothDeviceHelper.Connected && _canvasHeight > 0 && _canvasWidth > 0)
                {
                    var percW = (100 * idPosInfo.Location.X) / _canvasWidth;
                    var percH = (100 * idPosInfo.Location.Y) / _canvasHeight;
                    var msg = $"{(int)percW},{(int)percH},{_liftPosition}|";

                    _bluetoothDeviceHelper.SendMessageAsync(msg);
                }
            }
            else
            {
                _bluetoothDeviceHelper.SendMessageAsync($"50,50,{_liftPosition}|");
            }

            return true;
        }

        private void CanvasView_OnTouch(object sender, SKTouchEventArgs args)
        {
            switch (args.ActionType)
            {
                case SKTouchAction.Entered:
                    break;
                case SKTouchAction.Pressed:
                    if (args.InContact)
                    {
                        _idDictionary.Add(args.Id, new TouchInfo { Location = args.Location });
                        //UpdateLabel(args.Id);
                    }

                    break;
                case SKTouchAction.Moved:
                    if (_idDictionary.ContainsKey(args.Id))
                    {
                        _idDictionary[args.Id].Location = args.Location;
                        //UpdateLabel(args.Id);
                    }
                    break;
                case SKTouchAction.Released:
                case SKTouchAction.Cancelled:
                    if (_idDictionary.ContainsKey(args.Id))
                    {
                        _idDictionary.Remove(args.Id);
                        MessageLabel.Text = $"Removed {args.Id}";
                    }
                    break;
                case SKTouchAction.Exited:
                    break;
            }

            args.Handled = true;
            CanvasViewMove.InvalidateSurface();
            // Message2BluetoothDevice();
        }

        //async void Message2BluetoothDevice()
        //{
        //    if (_bluetoothDeviceHelper != null && _bluetoothDeviceHelper.Connected)
        //        await _bluetoothDeviceHelper.SendMessageAsync("Test|");
        //}

        void UpdateLabel(long id)
        {
            var idPosInfo = _idDictionary[id];
            MessageLabel.Text = $"Last: {idPosInfo.Location.X}, {idPosInfo.Location.Y}";
        }

        private bool Connected => _bluetoothDeviceHelper != null && _bluetoothDeviceHelper.Connected;

        private void CanvasView_OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(BackgroundColor.ToSKColor());
            var w = canvas.LocalClipBounds.Width;
            var h = canvas.LocalClipBounds.Height;
            var joystickSize = 60;
            var joystickColor = SKColors.DarkSlateBlue;

            if ((int)_canvasHeight == 0)
            {
                _canvasHeight = h;
                _canvasWidth = w;
            }

            //var btColor = Connected ? SKColors.Green : SKColors.Red;

            // Default image related stuff, TODO: create in bitmap
            // TODO: Label for BT status
            //canvas.DrawCircle(30, 30, 20, new SKPaint { Color = btColor, Style = SKPaintStyle.Fill });
            //canvas.DrawText(_bluetoothStatus, 60, 45, new SKPaint { Color = SKColors.DarkGray, TextSize = 40 });

            //canvas.DrawLine(w / 2, 0, w / 2, h, new SKPaint { Color = SKColors.BlueViolet, StrokeWidth = 3 });
            //canvas.DrawCircle((w / 4) * 3, h / 2, h / 4, new SKPaint { Color = SKColors.Orange, Style = SKPaintStyle.Fill });

            //canvas.DrawCircle(_joystick.CenterPoint, h / 4, new SKPaint { Color = SKColors.Orange, Style = SKPaintStyle.Fill });
            //canvas.DrawCircle((w / 4) * 3, h / 2, h / 4, new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Stroke, StrokeWidth = 3 });
            //canvas.DrawCircle((w / 4) * 3, h / 2, h / 4, new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Stroke, StrokeWidth = 3 });

            var strokeLineStyle = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Purple,
                StrokeWidth = 1,
                PathEffect = SKPathEffect.CreateDash(new float[] { 7, 7 }, 0)
            };

            canvas.DrawLine(w / 2, 0, w / 2, h, strokeLineStyle);
            canvas.DrawLine(0, h / 2, w, h / 2, strokeLineStyle);


            if (_idDictionary.Count == 0)
                canvas.DrawCircle(w / 2, h / 2, joystickSize, new SKPaint { Color = joystickColor, Style = SKPaintStyle.Fill });

            foreach (var key in _idDictionary.Keys)
            {
                var info = _idDictionary[key];

                canvas.DrawCircle(info.Location.X, info.Location.Y, joystickSize, new SKPaint
                {
                    Color = joystickColor,
                    Style = SKPaintStyle.Fill,
                });
            }
        }

        private void ButtonUp_OnPressed(object sender, EventArgs e)
        {
            _liftPosition = 1;
        }

        private void ButtonUp_OnReleased(object sender, EventArgs e)
        {
            _liftPosition = 0;
        }

        private void ButtonDown_OnPressed(object sender, EventArgs e)
        {
            _liftPosition = -1;
        }

        private void ButtonDown_OnReleased(object sender, EventArgs e)
        {
            _liftPosition = 0;
        }
    }
}