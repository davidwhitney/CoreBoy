// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Setter needed in order to dinamically construct the ViewModels in the MainWindow constructor", Scope = "member", Target = "~P:CoreBoy.Avalonia.MenuItemViewModel.Items")]
