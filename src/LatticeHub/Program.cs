using System;
using System.Windows.Forms;

using Lattice.Hub.UI;

namespace Lattice.Hub
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new HubWindow());
        }
    }
}
