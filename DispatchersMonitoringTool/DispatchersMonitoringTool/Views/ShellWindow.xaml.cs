﻿using System.Windows.Controls;

using DispatchersMonitoringTool.Contracts.Views;
using DispatchersMonitoringTool.ViewModels;

using MahApps.Metro.Controls;

namespace DispatchersMonitoringTool.Views;

public partial class ShellWindow : MetroWindow, IShellWindow
{
    public ShellWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public Frame GetNavigationFrame()
        => shellFrame;

    public void ShowWindow()
        => Show();

    public void CloseWindow()
        => Close();
}
