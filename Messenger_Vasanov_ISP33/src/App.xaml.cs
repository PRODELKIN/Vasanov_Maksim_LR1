using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Messenger_Vasanov_ISP33
{
    public partial class App : Application
    {
        public static Entities.Messenger_Vasanov_ISP33Entities5 Context { get; } = new Entities.Messenger_Vasanov_ISP33Entities5();
    }
}
