# Virtual Desktop Manager

This is a WinForms application that uses [`Slion/VirtualDesktop` (a C# wrapper for the Virtual Desktop API on Windows 11)](https://github.com/Slion/VirtualDesktop) to move windows between different virtual desktops on Windows 10 and Windows 11.

Note that there is newer Rust rewrite of this program available at: [Lej77/virtual-desktop-manager-rs: A Win32 application that can move windows between different virtual desktops](https://github.com/Lej77/virtual-desktop-manager-rs?tab=readme-ov-file).

## Features

- Tray icon shows one-based index of current virtual desktop.

  - The icons were adapted from another project at: [m0ngr31/VirtualDesktopManager](https://github.com/m0ngr31/VirtualDesktopManager)

- Left click tray icon to open a configuration window where you can setup rules for automatically moving windows to specific virtual desktops.

  - Window titles and process names can be used to determine what windows to move.

  - Hint: you can double click on "filters" (rules) to select them in the right sidebar, then you can easily change the filter's options.

  - When specifying a rules "window title" or "process name" there are text boxes with multiple lines. To match a name verbatism use only a single line. The program is made to allow multiple missmatched characters between each lines' text.

  - Note: the "Root Parent Index" and "Parent Index" columns are not very useful.

- Middle click tray icon to apply the configured rules and automatically move windows.

- Right click tray icon to open the context menu and switch to a different virtual desktop.

  - There is an option for "smooth" switching where animations are used when transitions to the traget desktop. This is implemented by opening an invisible window, moving it to the target desktop and then focusing it.

  - The textbox in the top of the context menu allows writing the index of a target desktop to easily switch to it.

    - There are also some other handy quick keys, such as "s" to toggle smooth switching or "+"/"-" to target a neighboring desktop.

- The context menu has an option to "Stop flashing windows", this refers to window icons in the toolbar that can start flashing orange when a window wants your attention. Such taskbar icons are visibile on all virtual desktops and so the purpose of moving a window to another desktop is not quite achived. Therefore this program can stop such flashing.

  - This feature can also be configured to be used every time the automatic window rules are applied.

- The configuration window also has an option for the program to request admin permission when started. This is useful since otherwise the program can't move windows opened by processes that have admin permissions.

- All settings are automatically saved to a settings file next to the executable. The rules specified in the configuration window can also be exported and imported manually.

- The program can also be used in "server" mode if started with the right command line flags, use the "--help" flag to see more information about command line usage.

  - The "server" mode allows re-using the program's code from a scripting language like "JavaScript" for more advanced usage. A default JavaScript/TypeScript client for the Deno JavaScript runtime is included inside the executable and can be emitted with the right command line flags.
