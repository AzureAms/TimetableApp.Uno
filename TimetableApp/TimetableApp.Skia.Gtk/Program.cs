using GLib;
using System;
using Uno.UI.Runtime.Skia;

namespace TimetableApp.Skia.Gtk
{
    class Program
    {
        static void Main(string[] args)
        {
            ExceptionManager.UnhandledException += delegate (UnhandledExceptionArgs expArgs)
            {
                Console.WriteLine("GLIB UNHANDLED EXCEPTION" + expArgs.ExceptionObject.ToString());
                expArgs.ExitApplication = true;
            };

            var host = new GtkHost(() => new App(), args);

            // OpenGL render does not work on some environments,
            // such as WSL. See unoplatform/uno#8643
            host.RenderSurfaceType = RenderSurfaceType.Software;

            host.Run();
        }
    }
}
