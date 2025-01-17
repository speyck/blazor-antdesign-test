﻿using BlazorMedia;
using BlazorMedia.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using System;
using speyck.BarcodeReader;
using blazor_antdesign_test.Data;

namespace blazor_antdesign_test.Shared
{
    public partial class Scanner
    {
        [Parameter]
        public string DividerTopMargin { get; set; }

        [Parameter]
        public string Height { get; set; }

        [Parameter]
        public string Width { get; set; }

        [Parameter]
        public string SelectedCamera { get; set; } = string.Empty;

        [Parameter]
        public BarcodeReader Reader { get; set; } = new();

        [Inject]
        private IJSRuntime JSRuntime { get; set; }
        private Global Global { get; set; }
        private VideoMedia CameraControl { get; set; }
        private IEnumerable<MediaDeviceInfo> Cameras { get; set; } = Enumerable.Empty<MediaDeviceInfo>();
        private BlazorMediaAPI MediaAPI { get; set; }
        private int ResWidth { get; set; } = 1920;
        private int ResHeight { get; set; } = 1080;
        private bool ShowCamera { get; set; } = false;

        protected override async void OnInitialized()
        {
            MediaAPI = new BlazorMediaAPI(JSRuntime);
            Global = new Global(JSRuntime);
            Reader.DetectedBarcode += BarcodeDetected_Handler;

            await base.OnInitializedAsync();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await MediaAPI.StartDeviceChangeListenerAsync();
                await FetchCamerasAsync();
                await InvokeAsync(StateHasChanged);
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        private async void OnData()
        {
            Image img = null;

            // Get the base64 code of the current image
            string data = await CameraControl.CaptureImageAsync();

            // Remove the first 22 characters ('data:image/png;base64,')
            data = data.Remove(0, 22);

            // Convert the valid image string to a byte[]
            byte[] dataArr = Convert.FromBase64String(data);

            // Convert the byte[] to a System.Drawing.Image using a MemoryStream
            using (MemoryStream ms = new(dataArr))
            {
                img = Image.FromStream(ms);
            }

            Reader.Decode(new Bitmap(img));
        }

        public async void BarcodeDetected_Handler(object sender, BarcodeEventArgs e)
        {
            await Global.Alert("Barcode found with Value: " + e.Value);
        }

        private async void OnError(MediaError error)
        {
            await Global.Alert(error.Message);
        }

        private async Task FetchCamerasAsync()
        {
            IEnumerable<MediaDeviceInfo> Devices = await MediaAPI.EnumerateMediaDevices();

            Cameras = Devices.Where(d => d.Kind == MediaDeviceKind.VideoInput);

            if (Cameras.Any() && !Cameras.Any(d => d.DeviceId == SelectedCamera))
            {
                SelectedCamera = Cameras.ElementAt(0).DeviceId;

                ShowCamera = true;
            }
        }
    }
}
