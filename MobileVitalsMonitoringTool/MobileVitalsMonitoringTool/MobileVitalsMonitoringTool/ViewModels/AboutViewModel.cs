﻿using System;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

using MonitoringSuiteLibrary.Services;
using MonitoringSuiteLibrary.Models;
using System.Collections.ObjectModel;
using MobileVitalsMonitoringTool.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MobileVitalsMonitoringTool.Views;
using MonitoringSuiteLibrary.MachineLearning;
using System.Reflection;
using System.IO;
using System.Security.Cryptography;

namespace MobileVitalsMonitoringTool.ViewModels
{
    /// <summary>
    /// A class that represents the AboutViewModel. This is the main page of the application
    /// and only logged in users can see it.
    /// </summary>
    public class AboutViewModel : BaseViewModel
    {
        bool disableSOSButton = false;
        string fname;
        string ONNXModelPath = Path.Combine("data", "user", "0", "com.frs.mobilevitalsmonitoringtool", "files", ".local", "share", "DistressONNXModel.onnx");

        /// <summary>
        /// Creates a <see cref="AboutViewModel"/>.
        /// </summary>
        public AboutViewModel()
        {
            Title = "Home";
            Preferences.Set("checkDistressFlag", true);
            Preferences.Set("hasAlert", false);
            WorkerId = Preferences.Get("w_id", -1);
            bool isLoggedIn = Preferences.Get("isLogin", false);

            // Take user to login page if not logged in
            if (WorkerId == -1 || !isLoggedIn)
            {
                Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
            }

            SOSCommand = new Command(OnSOS);

            // subscribe to messaging center and start location and vitals service (only set for Android)
            if (Device.RuntimePlatform == Device.Android)
            {
                // get location message from GetLocationVitalsService
                MessagingCenter.Subscribe<LocationMessage>(this, "Location", message => {
                    Device.BeginInvokeOnMainThread(() => {

                        // for debugging:
                        //Location = $"{Environment.NewLine}{message.Location.YCoord}, {message.Location.XCoord}, {DateTime.Now.ToLongTimeString()}";
                        //Console.WriteLine($"{message.Location.YCoord}, {message.Location.XCoord}, {DateTime.Now.ToLongTimeString()}");

                        UpdateDBLocation(message.Location);
                        GetFirstResponder();
                    });
                });

                // get Vitals message GetLocationVitalsService
                MessagingCenter.Subscribe<VitalsMessage>(this, "Vitals", message => {
                    Device.BeginInvokeOnMainThread(() => {

                        UpdateDBVitals(message.Vitals);

                        // checkDistressFlag prevents multiple alert pages to open
                        if (Preferences.Get("checkDistressFlag", true))
                        {
                            Console.WriteLine($"ML CHECKED!!!");
                            if (CheckDistressONNX.GetDistressStatus(FirstResponder.Age, FirstResponder.Sex, message.Vitals, ONNXModelPath))
                            {
                                OnSOS();
                            }

                            // check alert status only if hasAlert is false
                            if (!Preferences.Get("hasAlert", false))
                            {
                                CheckAlertStatus();
                            }
                        }
                    });
                });

                // get message when service has been stopped
                MessagingCenter.Subscribe<StopServiceMessage>(this, "ServiceStopped", message => {
                    Device.BeginInvokeOnMainThread(() => {
                        Location = "Location Service has been stopped!";
                    });
                });

                // get message when there is an error getting the location or vitals
                MessagingCenter.Subscribe<ErrorMessage>(this, "LocationVitalsError", message => {
                    Device.BeginInvokeOnMainThread(() => {
                        Location = "There was an error updating location and/or vitals!";
                    });
                });

                if (Preferences.Get("LocationVitalsServiceRunning", false) == true)
                {
                    StartService();
                }
            }

        }

        /// <summary>
        /// Gets the SOSCommand.
        /// </summary>
        public Command SOSCommand { get; }

        /// <summary>
        /// Navigates user to AlertPage.
        /// </summary>
        private async void OnSOS()
        {
            // to prevent double taps
            if (disableSOSButton)
            {
                return;
            }

            disableSOSButton = true;

            Preferences.Set("checkDistressFlag", false);
            await Shell.Current.GoToAsync(nameof(AlertPage));

            disableSOSButton = false;
        }

        /// <summary>
        /// Pulls first responder information from the database.
        /// </summary>
        private async void GetFirstResponder()
        {
            WorkerId = Preferences.Get("w_id", -1);
            if (FirstResponder == null || FirstResponder.FirstResponderId != WorkerId)
            {
                FirstResponder = await dataService.GetFirstResponderAsync(WorkerId);
                fname = FirstResponder.FName;
            }
        }

        /// <summary>
        /// Checks if the first responder has an active alert status and if they do they are navigated to AlertPage.
        /// </summary>
        private async void CheckAlertStatus()
        {
            WorkerId = Preferences.Get("w_id", -1);
            if (await dataService.FirstResponderHasAlertAsync(WorkerId))
            {
                Preferences.Set("hasAlert", true);
                OnSOS();
            }
        }

        /// <summary>
        /// Updates the location entry of the first responder in the database.
        /// </summary>
        private async void UpdateDBLocation(MonitoringSuiteLibrary.Models.Location location)
        {
            WorkerId = Preferences.Get("w_id", -1);
            if (await dataService.GetFirstResponderLocationAsync(WorkerId) == null)
            {
                await dataService.CreateFirstResponderLocationAsync(WorkerId, location);
            }
            else
            {
                await dataService.UpdateFirstResponderLocationAsync(WorkerId, location);
            }
        }

        /// <summary>
        /// Updates the vitals entry of the first responder in the database.
        /// </summary>
        private async void UpdateDBVitals(Vitals vitals)
        {
            WorkerId = Preferences.Get("w_id", -1);
            if (await dataService.GetFirstResponderVitalsAsync(WorkerId) == null)
            {
                await dataService.CreateFirstResponderVitalsAsync(WorkerId, vitals);
            }
            else
            {
                await dataService.UpdateFirstResponderVitalsAsync(WorkerId, vitals);
            }
        }
    }
}
